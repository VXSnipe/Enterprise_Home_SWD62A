using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnterpriseHomeAssignment.Repositories
{
    public class ItemsInMemoryRepository : IItemsRepository
    {
        private readonly IMemoryCache _cache;
        private const string CacheKey = "ImportedItems";

        public ItemsInMemoryRepository(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<IEnumerable<IItemValidating>> GetAllAsync()
        {
            _cache.TryGetValue(CacheKey, out List<IItemValidating> items);
            return Task.FromResult<IEnumerable<IItemValidating>>(items ?? new List<IItemValidating>());
        }

        public Task SaveAsync(IEnumerable<IItemValidating> items)
        {
            _cache.Set(CacheKey, new List<IItemValidating>(items));
            return Task.CompletedTask;
        }

        public void Clear()
        {
            _cache.Remove(CacheKey);
        }
    }
}
