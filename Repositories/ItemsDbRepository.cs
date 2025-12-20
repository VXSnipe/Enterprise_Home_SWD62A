using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Models;
using EnterpriseHomeAssignment.Data;
using Microsoft.EntityFrameworkCore;
using System;
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
            var restaurants = items.OfType<Restaurant>().ToList();
            var menuItems = items.OfType<MenuItem>().ToList();

            if (restaurants.Any())
            {
                _db.Restaurants.AddRange(restaurants);
                await _db.SaveChangesAsync();
            }

            foreach (var menuItem in menuItems)
            {
                var restaurant = restaurants
                    .FirstOrDefault(r => r.ExternalId == menuItem.RestaurantExternalId);

                if (restaurant == null)
                    throw new Exception("Restaurant not found for menu item");

                menuItem.RestaurantId = restaurant.Id;
            }

            if (menuItems.Any())
            {
                _db.MenuItems.AddRange(menuItems);
                await _db.SaveChangesAsync();
            }
        }

        public async Task ApproveAsync(int[] restaurantIds, Guid[] menuItemIds)
        {
            if (restaurantIds != null && restaurantIds.Length > 0)
            {
                var rests = await _db.Restaurants
                    .Where(r => restaurantIds.Contains(r.Id))
                    .ToListAsync();

                foreach (var r in rests)
                    r.Status = "Approved";
            }

            if (menuItemIds != null && menuItemIds.Length > 0)
            {
                var items = await _db.MenuItems
                    .Where(m => menuItemIds.Contains(m.Id))
                    .ToListAsync();

                foreach (var m in items)
                    m.Status = "Approved";
            }

            await _db.SaveChangesAsync();
        }
    }
}
