using System.Collections.Generic;
using System.Threading.Tasks;
using EnterpriseHomeAssignment.Models;

namespace EnterpriseHomeAssignment.Interfaces
{
    public interface IItemsRepository
    {
        Task<IEnumerable<IItemValidating>> GetAllAsync();
        Task SaveAsync(IEnumerable<IItemValidating> items);
    }
}
