namespace Sattim.Web.Services.Repositories
{
    public class BidRepository : GenericRepository<Models.Bid.Bid>, IBidRepository
    {
        public BidRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Models.Bid.Bid?> GetHighestBidForProductAsync(int productId)
        {
            // Bu sorgu, açık artırma mantığının temelidir.
            return await _dbSet
                .Where(b => b.ProductId == productId)
                .OrderByDescending(b => b.Amount) // En yüksek tutar
                .ThenBy(b => b.BidDate) // Eşitlik durumunda ilk teklif veren
                .FirstOrDefaultAsync();

            // Not: Bu sorgu AsNoTracking() içermez, çünkü bir servis
            // bu veriyi alıp hemen ardından 'Product.CurrentPrice'ı
            // güncellemek isteyebilir (izleme gerekebilir).
            // Ancak, sadece okuma yapılacaksa .AsNoTracking() eklenebilir.
        }

        public async Task<IEnumerable<Models.Bid.Bid>> GetBidsForProductAsync(int productId)
        {
            // Bu sorgu, bir ürünün "Teklif Geçmişi" sayfası içindir.
            return await _dbSet
                .Where(b => b.ProductId == productId)
                .Include(b => b.Bidder) // Teklif verenin (User) bilgilerini de al
                .OrderByDescending(b => b.Amount)
                .ThenBy(b => b.BidDate)
                .AsNoTracking() // Sadece okuma (read-only)
                .ToListAsync();
        }

        public async Task<IEnumerable<Models.Bid.Bid>> GetBidsForUserAsync(string userId)
        {
            // Bu sorgu, kullanıcının "Tekliflerim" sayfası içindir.
            return await _dbSet
                .Where(b => b.BidderId == userId)
                .Include(b => b.Product) // Teklif verdiği ürünün bilgilerini de al
                    .ThenInclude(p => p.Images.Where(i => i.IsPrimary)) // Ürünün ana resmini al
                .OrderByDescending(b => b.BidDate)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
