using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}