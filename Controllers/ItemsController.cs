using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using System;
using EnterpriseHomeAssignment.Attributes;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseHomeAssignment.Controllers
{
    public class ItemsController : Controller
    {
        private readonly IItemsRepository _dbRepo;

        public ItemsController([FromKeyedServices("Db")] IItemsRepository dbRepo)
        {
            _dbRepo = dbRepo;
        }

        public async Task<IActionResult> Catalog(string view = "card", bool pending = false)
        {
            var allItems = await _dbRepo.GetAllAsync();
            
            var items = allItems.Where(i =>
            {
                if (i is Restaurant r) 
                    return pending ? r.Status == "Pending" : r.Status == "Approved";
                if (i is MenuItem m) 
                    return pending ? m.Status == "Pending" : m.Status == "Approved";
                return false;
            });

            return View(items);
        }

        [Authorize]
        public async Task<IActionResult> Verification()
        {
            var user = User;
            var email = user.Identity.Name ?? user.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            bool isAdmin = string.Equals(email, "admin@example.com", StringComparison.OrdinalIgnoreCase);

            if (isAdmin)
            {
                // Show pending restaurants
                var all = await _dbRepo.GetAllAsync();
                var pendingRestaurants = all.OfType<Restaurant>().Where(r => r.Status == "Pending");
                return View("VerifyRestaurants", pendingRestaurants);
            }
            else
            {
                // Show restaurants owned by this owner
                var all = await _dbRepo.GetAllAsync();
                var myRestaurants = all.OfType<Restaurant>().Where(r => string.Equals(r.OwnerEmailAddress, email, StringComparison.OrdinalIgnoreCase));
                return View("VerifyOwner", myRestaurants);
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Approve(int[] restaurantIds, Guid[] menuItemIds)
        {
            // perform validation: ensure current user is allowed to approve each item
            var all = await _dbRepo.GetAllAsync();

            var userEmail = User.Identity.Name ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            // Check restaurants
            if (restaurantIds != null && restaurantIds.Length > 0)
            {
                foreach (var id in restaurantIds)
                {
                    var r = all.OfType<Restaurant>().FirstOrDefault(x => x.Id == id);
                    if (r == null) return Forbid();
                    var validators = r.GetValidators();
                    if (!validators.Contains(userEmail, StringComparer.OrdinalIgnoreCase))
                        return Forbid();
                }
            }

            // Check menu items
            if (menuItemIds != null && menuItemIds.Length > 0)
            {
                foreach (var id in menuItemIds)
                {
                    var m = all.OfType<MenuItem>().FirstOrDefault(x => x.Id == id);
                    if (m == null) return Forbid();
                    var validators = m.GetValidators();
                    if (!validators.Contains(userEmail, StringComparer.OrdinalIgnoreCase))
                        return Forbid();
                }
            }

            await _dbRepo.ApproveAsync(restaurantIds ?? Array.Empty<int>(), menuItemIds ?? Array.Empty<Guid>());

            return RedirectToAction("Verification");
        }
    }
}
