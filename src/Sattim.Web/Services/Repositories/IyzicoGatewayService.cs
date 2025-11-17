using Sattim.Web.Models.Payment;
using Sattim.Web.Models.User;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Payment
{
    /// <summary>
    /// IGatewayService'in Iyzico için ÖRNEK (sahte/stub) implementasyonu.
    /// Gerçek dünyada burada Iyzico SDK'sı çağrılır.
    /// </summary>
    public class IyzicoGatewayService : IGatewayService
    {
        public async Task<(bool Success, string HtmlContent, string ErrorMessage)> CreateCheckoutFormAsync(
            Models.Payment.Payment payment,
            ApplicationUser user)
        {
            //
            // --- GERÇEK IYZICO SDK KODU BURAYA GELİR ---
            //
            // var options = new Iyzico.Options { ApiKey = "...", SecretKey = "..." };
            // var client = new Iyzico.IyzipayApiClient(options);
            // var request = new Iyzico.Request.CreateCheckoutFormRequest();
            // request.ConversationId = payment.Id.ToString(); // Bizim Payment ID'miz
            // request.Price = payment.Amount;
            // request.Buyer = new Iyzico.Model.Buyer { ... user ... };
            // ... (ve diğer 50 satır Iyzico kodu) ...
            // var response = client.CheckoutForm.Create(request);
            //
            // if (response.Status == "success") {
            //     return (true, response.CheckoutFormContent, null);
            // }
            // return (false, null, response.ErrorMessage);
            //

            // --- SAHTE (STUB) KOD (Test için) ---
            await Task.Delay(250); // Ağ gecikmesini simüle et
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