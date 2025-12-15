using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

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

        public Task ApproveAsync(int[] restaurantIds, Guid[] menuItemIds)
        {
            _cache.TryGetValue(CacheKey, out List<IItemValidating> items);
            if (items == null)
                return Task.CompletedTask;

            if (restaurantIds != null && restaurantIds.Length > 0)
            {
                var rests = items.OfType<Restaurant>().Where(r => restaurantIds.Contains(r.Id));
                foreach (var r in rests)
                    r.Status = "Approved";
            }

            if (menuItemIds != null && menuItemIds.Length > 0)
            {
                var mis = items.OfType<MenuItem>().Where(m => menuItemIds.Contains(m.Id));
                foreach (var m in mis)
                    m.Status = "Approved";
            }

            // Update cache
            _cache.Set(CacheKey, items);

            return Task.CompletedTask;
        }
    }
}
