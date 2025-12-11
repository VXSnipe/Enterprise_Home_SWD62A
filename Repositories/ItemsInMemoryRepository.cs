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

        public Task<IEnumerable<object>> GetAllAsync()
        {
            _cache.TryGetValue(CacheKey, out List<object> items);
            return Task.FromResult<IEnumerable<object>>(items ?? new List<object>());
        }

        public Task SaveAsync(IEnumerable<object> items)
        {
            _cache.Set(CacheKey, new List<object>(items));
            return Task.CompletedTask;
        }

        public void Clear()
        {
            _cache.Remove(CacheKey);
        }
    }
}
