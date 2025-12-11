using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnterpriseHomeAssignment.Interfaces
{
    public interface IItemsRepository
    {
        Task<IEnumerable<object>> GetAllAsync();
        Task SaveAsync(IEnumerable<object> items);
    }
}
