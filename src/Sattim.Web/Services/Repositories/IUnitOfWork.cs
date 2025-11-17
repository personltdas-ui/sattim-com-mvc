namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Değişikliklerin toplu olarak kaydedilmesini (SaveChangesAsync)
    /// garanti eden merkezi arayüz.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Yapılan tüm değişiklikleri tek bir işlem (transaction) olarak
        /// veritabanına kaydeder.
        /// </summary>
        /// <returns>Etkilenen satır sayısı.</returns>
        Task<int> SaveChangesAsync();
    }
}
