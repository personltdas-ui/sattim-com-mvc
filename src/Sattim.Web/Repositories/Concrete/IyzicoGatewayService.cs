using Sattim.Web.Models.Payment;
using Sattim.Web.Models.User;
using Sattim.Web.Repositories.Interface;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class IyzicoGatewayService : IGatewayService
    {
        public async Task<(bool Success, string HtmlContent, string ErrorMessage)> CreateCheckoutFormAsync(
            Payment payment,
            ApplicationUser user)
        {
            await Task.Delay(250);
            string fakeIyzicoFormHtml = $@"
                <div style='border: 2px dashed #007bff; padding: 20px; text-align: center;'>
                    <h4>Sahte Iyzico Ödeme Formu</h4>
                    <p>Ödeme yapılıyor: {payment.Amount:C}</p>
                    <p>Kullanıcı: {user.FullName}</p>
                    <p>Ödeme ID: {payment.Id}</p>
                    <p>(Bu form, 1 saniye sonra Webhook'u tetikleyecek...)</p>
                    <script>
                        setTimeout(() => {{
                            console.log('Webhook tetikleniyor...');
                            fetch('/api/Payment/Webhook', {{
                                method: 'POST',
                                headers: {{ 'Content-Type': 'application/json' }},
                                body: JSON.stringify({{
                                    paymentId: {payment.Id},
                                    success: true,
                                    transactionId: 'iyz_tr_12345',
                                    gatewayResponse: '{{""status"":""success""}}'
                                }})
                            }});
                        }}, 1000);
                    </script>
                </div>";

            return (true, fakeIyzicoFormHtml, null);
        }
    }
}