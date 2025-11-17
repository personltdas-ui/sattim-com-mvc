using AutoMapper; 
using Microsoft.EntityFrameworkCore; 
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; 
using Sattim.Web.Models.Bid;
using Sattim.Web.Models.Product;
using Sattim.Web.Services.Repositories;
using Sattim.Web.ViewModels.Bid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Bid
{
    public class BidService : IBidService
    {
        
        private readonly IBidRepository _bidRepo;
        private readonly IProductRepository _productRepo;
        private readonly IGenericRepository<AutoBid> _autoBidRepo;

        
        private readonly ApplicationDbContext _context; 
        private readonly IMapper _mapper; 
        private readonly ILogger<BidService> _logger;
       

        public BidService(
            IBidRepository bidRepo,
            IProductRepository productRepo,
            IGenericRepository<AutoBid> autoBidRepo,
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<BidService> logger
            
            )
        {
            _bidRepo = bidRepo;
            _productRepo = productRepo;
            _autoBidRepo = autoBidRepo;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            
        }

        // ====================================================================
        //  COMMANDS (Veri Yazma/Değiştirme İşlemleri)
        // ====================================================================

        /// <summary>
        /// Bir ürüne manuel (normal) bir teklif verir.
        /// (Arayüzdeki hata yönetimine uyar: (bool, string) döndürür)
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> PlaceBidAsync(PlaceBidViewModel model, string userId)
        {
            // 1. Transaction'ı Başlat (Bid ve Product aynı anda güncellenmeli)
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 2. Varlıkları Al (Takip et - 'AsNoTracking' KULLANMA)
                var product = await _productRepo.GetByIdAsync(model.ProductId);

                // 3. İş Kurallarını (Service Layer) Kontrol Et
                if (product == null)
                    return (false, "Teklif verilmek istenen ürün bulunamadı.");
                if (product.Status != ProductStatus.Active)
                    return (false, "Bu ihale şu anda aktif değil.");
                if (product.EndDate <= DateTime.UtcNow)
                    return (false, "Bu ihalenin süresi dolmuş.");
                if (product.SellerId == userId)
                    return (false, "Kendi ürününüze teklif veremezsiniz.");

                // Gerekli minimum teklifi hesapla
                // (Mevcut fiyat + artış) VEYA (eğer ilk teklifse) Başlangıç fiyatı
                decimal minBid;
                if (product.CurrentPrice == product.StartingPrice && (await _bidRepo.AnyAsync(b => b.ProductId == product.Id) == false))
                {
                    // Bu, ilk teklif
                    minBid = product.StartingPrice;
                }
                else
                {
                    // Bu, sonraki teklif
                    minBid = product.CurrentPrice + product.BidIncrement;
                }

                if (model.Amount < minBid)
                    return (false, $"Teklifiniz minimum ({minBid:C}) tutarı karşılamıyor.");

                // 4. İş Mantığını Modele (Varlığa) Devret
                product.UpdateCurrentPrice(model.Amount); // (Model kendi durumunu (Active) tekrar kontrol eder)

                // 5. Yeni Teklifi (Dekont) Oluştur
                var newBid = new Models.Bid.Bid(product.Id, userId, model.Amount);

                // 6. Değişiklikleri Repolara Bildir
                _productRepo.Update(product); // Ürünün 'CurrentPrice'ını güncelle
                await _bidRepo.AddAsync(newBid); // Yeni 'Bid' ekle

                // 7. Değişiklikleri Veritabanına Kaydet (Atomik)
                await _context.SaveChangesAsync(); // Repository'lerin UoW'unu DEĞİL, context'i kullanıyoruz

                // 8. Transaction'ı Onayla
                await transaction.CommitAsync();

                _logger.LogInformation($"Manuel Teklif BAŞARILI (Commit). Kullanıcı: {userId}, Ürün: {product.Id}, Yeni Fiyat: {model.Amount}");

                // 9. (AYRI İŞLEM) Otomatik Teklifleri Tetikle
                // Bu, ana transaction commit edildikten SONRA çalışır.
                // (Bu metot kendi transaction'ını yönetmeli veya
                // bir arka plan servisine (Background Job) atılmalıdır)
                await ProcessAutoBidsAsync(product, newBid);

                // (Burada INotificationService çağrılıp satıcıya ve geçilen kullanıcıya bildirim gönderilir)
                // await _notificationService.NotifyBidPlaced(product, newBid);
                // await _notificationService.NotifyOutbid(product, newBid);

                return (true, null); // Başarılı
            }
            catch (Exception ex)
            {
                // (Modelin 'UpdateCurrentPrice' metodundan fırlatılan
                // 'InvalidOperationException' veya 'ArgumentException' dahil)
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Teklif verme sırasında KRİTİK HATA (Rollback). Kullanıcı: {userId}, Ürün: {model.ProductId}");
                return (false, "Teklifiniz işlenirken beklenmedik bir sistem hatası oluştu.");
            }
        }

        /// <summary>
        /// Bir ürün için 'AutoBid' (Otomatik Teklif) ayarı oluşturur veya günceller.
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> PlaceAutoBidAsync(AutoBidViewModel model, string userId)
        {
            try
            {
                // 1. İlgili Varlıkları Al
                var product = await _productRepo.GetByIdAsync(model.ProductId);
                if (product == null || product.Status != ProductStatus.Active)
                    return (false, "Bu ürüne şu anda otomatik teklif ayarlanamaz.");

                // 2. İş Kuralı: Maksimum teklif, mevcut fiyattan yüksek olmalı
                if (model.MaxAmount <= product.CurrentPrice)
                    return (false, "Maksimum teklif, mevcut fiyattan yüksek olmalıdır.");

                // 3. Mevcut ayarı bul (Bileşik Anahtar (Composite Key) ile)
                var existingAutoBid = await _autoBidRepo.GetByIdAsync(new object[] { userId, model.ProductId });

                if (existingAutoBid != null)
                {
                    // 4a. Varsa: Güncelle (Model metodu kuralları zorunlu kılar)
                    existingAutoBid.UpdateSettings(model.MaxAmount, model.IncrementAmount);
                    _autoBidRepo.Update(existingAutoBid);
                    _logger.LogInformation($"Otomatik teklif güncellendi. Kullanıcı: {userId}, Ürün: {model.ProductId}");
                }
                else
                {
                    // 4b. Yoksa: Yeni oluştur (Model metodu kuralları zorunlu kılar)
                    existingAutoBid = new AutoBid(userId, model.ProductId, model.MaxAmount, model.IncrementAmount);
                    await _autoBidRepo.AddAsync(existingAutoBid);
                    _logger.LogInformation($"Yeni otomatik teklif ayarlandı. Kullanıcı: {userId}, Ürün: {model.ProductId}");
                }

                // 5. Kaydet
                await _autoBidRepo.UnitOfWork.SaveChangesAsync();

                // 6. (ÖNEMLİ) Otomatik Teklif, mevcut fiyatı GEÇİYORSA,
                //    Otomatik Teklif sistemini HEMEN tetikle.
                var highestBid = await _bidRepo.GetHighestBidForProductAsync(product.Id);
                if (highestBid == null || (highestBid.BidderId != userId && existingAutoBid.MaxAmount > product.CurrentPrice))
                {
                    // Mevcut en yüksek teklif bizim değil (veya hiç teklif yok)
                    // ve bizim limitimiz mevcut fiyattan yüksek.
                    // 'Boş' bir teklif (dummy bid) ile süreci tetikle.
                    var dummyBid = new Models.Bid.Bid(product.Id, "SYSTEM", product.CurrentPrice);
                    await ProcessAutoBidsAsync(product, dummyBid);
                }

                return (true, null); // Başarılı
            }
            catch (Exception ex) // Modelin 'UpdateSettings' veya 'Constructor'ından gelen hatalar dahil
            {
                _logger.LogError(ex, $"Otomatik teklif ayarlanırken hata. Kullanıcı: {userId}, Ürün: {model.ProductId}");
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Bir kullanıcının ürün için ayarlanmış Otomatik Teklif ayarını pasif hale getirir.
        /// </summary>
        public async Task<bool> CancelAutoBidAsync(int productId, string userId)
        {
            try
            {
                var autoBid = await _autoBidRepo.GetByIdAsync(new object[] { userId, productId });
                if (autoBid == null)
                {
                    _logger.LogWarning($"İptal edilecek otomatik teklif bulunamadı. Kullanıcı: {userId}, Ürün: {productId}");
                    return false;
                }

                autoBid.Deactivate(); // Model metodu
                _autoBidRepo.Update(autoBid);
                await _autoBidRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Otomatik teklif iptal edildi. Kullanıcı: {userId}, Ürün: {productId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Otomatik teklif iptal edilirken hata. Kullanıcı: {userId}, Ürün: {productId}");
                return false;
            }
        }


        // ====================================================================
        //  QUERIES (Veri Okuma İşlemleri)
        // ====================================================================

        /// <summary>
        /// Bir ürünün tüm teklif geçmişini (kim, ne zaman, ne kadar) getirir.
        /// (Entity'leri ViewModel'lara dönüştürür)
        /// </summary>
        public async Task<ProductBidHistoryViewModel> GetProductBidHistoryAsync(int productId)
        {
            // 1. Ürün bilgilerini al (Sadece 1'e 1 ilişkileri al, teklifleri değil)
            // (IProductRepository'deki GetProductWithDetailsAsync'i KULLANMIYORUZ,
            // çünkü o 'Bids' koleksiyonunu da getirir, biz zaten 'IBidRepository'
            // ile teklifleri ayrıca ve daha verimli çekeceğiz.)
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null)
                throw new KeyNotFoundException($"Ürün bulunamadı (ID: {productId})");

            // 2. Teklif geçmişini al (Özel repo metodu ile)
            var bids = await _bidRepo.GetBidsForProductAsync(productId);

            // 3. AutoMapper ile Dönüştür
            var viewModel = _mapper.Map<ProductBidHistoryViewModel>(product);
            viewModel.Bids = _mapper.Map<List<BidHistoryItemViewModel>>(bids);

            

            return viewModel;
        }

        /// <summary>
        /// Mevcut kullanıcının yaptığı tüm teklifleri, ihalelere göre gruplanmış
        /// ve filtrelenmiş olarak getirir. ("Tekliflerim" sayfası)
        /// </summary>
        public async Task<List<UserBidItemViewModel>> GetUserBidsAsync(string userId, BidFilterType filter)
        {
            // 1. Özel Repo: Kullanıcının tüm tekliflerini (ve ilgili Ürünleri) al
            var allUserBids = await _bidRepo.GetBidsForUserAsync(userId);

            // 2. Grupla: Teklifleri Ürün bazında grupla
            var groupedByProduct = allUserBids.GroupBy(bid => bid.Product);

            var results = new List<UserBidItemViewModel>();

            // 3. Projeksiyon (ViewModel'a Dönüştürme) ve Filtreleme
            foreach (var group in groupedByProduct)
            {
                var product = group.Key;
                var myHighestBidInGroup = group.Max(b => b.Amount);

                // 3a. Durumu Hesapla
                UserBidStatus status;
                if (product.Status == ProductStatus.Active)
                {
                    status = (product.CurrentPrice == myHighestBidInGroup)
                        ? UserBidStatus.Active_Winning
                        : UserBidStatus.Active_Losing;
                }
                else if (product.Status == ProductStatus.Sold)
                {
                    status = (product.WinnerId == userId)
                        ? UserBidStatus.Won
                        : UserBidStatus.Lost;
                }
                else // Closed, Cancelled vb.
                {
                    status = UserBidStatus.Lost;
                }

                // 3b. Filtreyi Uygula
                bool matchesFilter = filter == BidFilterType.All ||
                    (filter == BidFilterType.Active && (status == UserBidStatus.Active_Losing || status == UserBidStatus.Active_Winning)) ||
                    (filter == BidFilterType.Won && status == UserBidStatus.Won) ||
                    (filter == BidFilterType.Lost && status == UserBidStatus.Lost);

                if (!matchesFilter)
                    continue; // Bu ürünü listeye ekleme

                // 3c. ViewModel'a Dönüştür (AutoMapper + Manuel)
                var viewModelItem = _mapper.Map<UserBidItemViewModel>(product);
                viewModelItem.MyHighestBid = myHighestBidInGroup;
                viewModelItem.Status = status;

                results.Add(viewModelItem);
            }

            return results;
        }

        /// <summary>
        /// Bir kullanıcının, belirli bir ürün için mevcut Otomatik Teklif ayarını getirir.
        /// </summary>
        public async Task<AutoBidSettingViewModel> GetUserAutoBidSettingAsync(int productId, string userId)
        {
            var autoBid = await _autoBidRepo.GetByIdAsync(new object[] { userId, productId });
            if (autoBid == null)
                return null; // Ayar yok

            // AutoMapper ile Entity -> ViewModel dönüşümü
            return _mapper.Map<AutoBidSettingViewModel>(autoBid);
        }

        // ====================================================================
        //  PRIVATE HELPERS (Yardımcı Metotlar)
        // ====================================================================

        /// <summary>
        /// (Bu, BidService'e ait özel bir yardımcı metottur)
        /// Yeni bir tekliften (manuel veya otomatik) sonra, diğer otomatik
        /// teklifleri kontrol eder ve gerekirse karşı teklif(ler) oluşturur.
        /// </summary>
        private async Task ProcessAutoBidsAsync(Models.Product.Product product, Models.Bid.Bid lastBid)
        {
            _logger.LogInformation($"Otomatik teklif süreci kontrol ediliyor... (Ürün: {product.Id})");

            // 1. Bu ürüne ayarlı, AKTİF ve teklifi geçilen
            //    (son teklifi veren KENDİSİ OLMAYAN) tüm otomatik teklifleri al.
            var autoBids = await _autoBidRepo.FindAsync(ab =>
                ab.ProductId == product.Id &&
                ab.IsActive &&
                ab.UserId != lastBid.BidderId &&
                ab.MaxAmount > product.CurrentPrice // Sadece mevcut fiyattan yüksek limiti olanlar
            );

            if (!autoBids.Any())
            {
                _logger.LogInformation("Tetiklenecek aktif otomatik teklif bulunamadı.");
                return; // Karşı teklif verecek kimse yok.
            }

            // 2. En yüksek MaxAmount'a sahip olanı bul (veya eşitse en eski tarihli)
            var highestAutoBidder = autoBids
                .OrderByDescending(ab => ab.MaxAmount)
                .ThenBy(ab => ab.CreatedDate)
                .First();

            // 3. Yeni otomatik teklif tutarını hesapla
            decimal newAutoBidAmount = product.CurrentPrice + product.BidIncrement;

            // 4. Eğer yeni tutar, otomatik teklif verenin limitini aşıyorsa,
            //    sadece limitini basar.
            if (newAutoBidAmount > highestAutoBidder.MaxAmount)
            {
                newAutoBidAmount = highestAutoBidder.MaxAmount;
                // Bu kullanıcı artık limitine ulaştı, pasife al.
                highestAutoBidder.Deactivate();
                _autoBidRepo.Update(highestAutoBidder);
                // (SaveChangesAsync aşağıda çağrılacak)
            }

            // 5. (Çok nadir durum) Eğer son teklif, otomatik teklifin limitine
            //    eşit veya yüksekse, otomatik teklif tetiklenemez.
            if (newAutoBidAmount <= product.CurrentPrice)
            {
                _logger.LogWarning($"Otomatik teklif (Kullanıcı: {highestAutoBidder.UserId}) tetiklenemedi, limit aşıldı.");
                highestAutoBidder.Deactivate();
                _autoBidRepo.Update(highestAutoBidder);
                await _autoBidRepo.UnitOfWork.SaveChangesAsync();
                return;
            }

            // 6. Otomatik Teklifi Ver (Transaction)
            // Bu, 'PlaceBidAsync'ten ayrı bir transaction olmalıdır.
            await using var autoBidTx = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Otomatik teklif tetiklendi: Kullanıcı: {highestAutoBidder.UserId}, Tutar: {newAutoBidAmount}");

                // Ürün fiyatını güncelle (Model metoduyla)
                product.UpdateCurrentPrice(newAutoBidAmount);
                _productRepo.Update(product);

                // Yeni otomatik teklifi (Bid) oluştur
                var autoBidEntry = new Models.Bid.Bid(product.Id, highestAutoBidder.UserId, newAutoBidAmount);
                await _bidRepo.AddAsync(autoBidEntry);

                // Değişiklikleri kaydet (AutoBid'in Deactivate() durumu dahil)
                await _context.SaveChangesAsync();
                await autoBidTx.CommitAsync();

                // (Burada eski teklif sahibine 'BidOutbid' bildirimi gönderilir)
                // await _notificationService.NotifyOutbid(product, autoBidEntry);
            }
            catch (Exception ex)
            {
                await autoBidTx.RollbackAsync();
                _logger.LogError(ex, "Otomatik teklif verilirken kritik hata (Rollback).");
            }
        }
    }
}