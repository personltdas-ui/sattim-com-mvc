namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Bid (Teklif) varlığı için jenerik repository'ye EK OLARAK
    /// bir ürüne ait en yüksek teklifi bulma veya bir kullanıcının
    /// tüm tekliflerini listeleme gibi özel sorgu metotları sağlar.
    /// </summary>
    public interface IBidRepository : IGenericRepository<Models.Bid.Bid>
    {
        /// <summary>
        /// Belirli bir ürüne (ProductId) yapılmış en yüksek tutarlı teklifi getirir.
        /// </summary>
        /// <returns>En yüksek teklif (Bid) veya hiç teklif yoksa null.</returns>
        Task<Models.Bid.Bid?> GetHighestBidForProductAsync(int productId);

        /// <summary>
        /// Belirli bir ürüne (ProductId) yapılmış tüm teklifleri,
        /// teklif vereni (Bidder) de içerecek şekilde, büyükten küçüğe
        /// sıralı olarak listeler.
        /// </summary>
        Task<IEnumerable<Models.Bid.Bid>> GetBidsForProductAsync(int productId);

        /// <summary>
        /// Belirli bir kullanıcının (BidderId) yaptığı tüm teklifleri,
        /// ilgili ürünü (Product) de içerecek şekilde,
        /// tarihe göre listeler.
        /// </summary>
        Task<IEnumerable<Models.Bid.Bid>> GetBidsForUserAsync(string userId);
    }
}
