using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IGenericRepository<T> where T : class
    {
        IUnitOfWork UnitOfWork { get; }

        Task AddAsync(T entity);

        Task<T?> GetByIdAsync(object id);

        void Update(T entity);

        void Remove(T entity);

        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task<IEnumerable<T>> GetAllAsync();

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    }
}