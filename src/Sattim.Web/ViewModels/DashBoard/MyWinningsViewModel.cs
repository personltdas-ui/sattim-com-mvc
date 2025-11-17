namespace Sattim.Web.ViewModels.DashBoard
{
    public class MyWinningsViewModel
    {
        /// <summary>
        /// Kullanıcının kazandığı ürünlerin listesi
        /// </summary>
        public List<WonProductViewModel> WonProducts { get; set; } = new List<WonProductViewModel>();

        /// <summary>
        /// Kazanılan ürünler için harcanan toplam tutar
        /// </summary>
        public decimal TotalSpent { get; set; }

        /// <summary>
        /// Ödemesi yapılmış ürün sayısı
        /// </summary>
        public int ItemsPaid { get; set; }

        /// <summary>
        /// Ödeme bekleyen ürün sayısı
        /// </summary>
        public int ItemsPending { get; set; }

        /// <summary>
        /// Kazanılan toplam ürün sayısı
        /// </summary>
        public int TotalWinnings { get; set; }
    }
}
