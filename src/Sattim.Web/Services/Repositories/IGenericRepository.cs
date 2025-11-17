using System.Linq.Expressions;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Tüm varlıklar (Entities) için temel CRUD (Create, Read, Update, Delete)
    /// işlemlerini sağlayan jenerik (generic) arayüz.
    /// </summary>
    /// <typeparam name="T">Varlık sınıfı (örn: Product, Category)</typeparam>
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// Unit of Work arayüzünü sağlar (SaveChangesAsync metodu için).
        /// </summary>
        IUnitOfWork UnitOfWork { get; }

        /// <summary>
        /// Bir varlığı asenkron olarak ekler.
        /// </summary>
        Task AddAsync(T entity);

        /// <summary>
        /// Bir varlığı birincil anahtarına (PK) göre bulur.
        /// </summary>
        Task<T?> GetByIdAsync(object id);

        /// <summary>
        /// Bir varlığı günceller (EF Core'da 'Modified' olarak işaretler).
        /// </summary>
        void Update(T entity);

        /// <summary>
        /// Bir varlığı siler.
        /// </summary>
        void Remove(T entity);

        /// <summary>
        /// Belirli bir koşula uyan tüm varlıkları listeler.
        /// ÖNEMLİ: Bu metot varsayılan olarak ilişkileri (Include) YÜKLEMEZ.
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Tüm varlıkları listeler.
        /// ÖNEMLİ: Bu metot varsayılan olarak ilişkileri (Include) YÜKLEMEZ.
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Bir koşula uyan herhangi bir varlık olup olmadığını kontrol eder.
        /// </summary>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Koşula uyan ilk varlığı (veya null) getirir.
        /// </summary>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    }
}
