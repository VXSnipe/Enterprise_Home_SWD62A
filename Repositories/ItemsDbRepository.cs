using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Models;
using EnterpriseHomeAssignment.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnterpriseHomeAssignment.Repositories
{
    public class ItemsDbRepository : IItemsRepository
    {
        private readonly ApplicationDbContext _db;

        public ItemsDbRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<IItemValidating>> GetAllAsync()
        {
            var restaurants = await _db.Restaurants
                .Include(r => r.MenuItems)
                .ToListAsync();

            var menuItems = await _db.MenuItems
                .Include(m => m.Restaurant)
                .ToListAsync();

            var combined = new List<IItemValidating>();
            combined.AddRange(restaurants);
            combined.AddRange(menuItems);
            return combined;
        }

        public async Task SaveAsync(IEnumerable<IItemValidating> items)
        {
            foreach (var item in items)
            {
                if (item is Restaurant r)
                {
                    _db.Restaurants.Add(r);
                }
                else if (item is MenuItem m)
                {
                    _db.MenuItems.Add(m);
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
