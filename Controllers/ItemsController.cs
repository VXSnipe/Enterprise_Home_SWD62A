using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Models;
using EnterpriseHomeAssignment.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EnterpriseHomeAssignment.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private bool IsAdmin()
        {
            return User.Identity?.Name == "admin@example.com";
        }

        // SE3.3 — Verification action
        public async Task<IActionResult> Verification(
            [FromKeyedServices("Db")] IItemsRepository repo)
        {
            var allItems = await repo.GetAllAsync();

            if (IsAdmin())
            {
                var pendingRestaurants = allItems
                    .OfType<Restaurant>()
                    .Where(r => r.Status == "Pending")
                    .ToList();

                return View("VerifyRestaurants", pendingRestaurants);
            }

            var userEmail = User.Claims
            .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)
            ?.Value;


            var ownedRestaurants = allItems
                .OfType<Restaurant>()
                .Where(r => r.OwnerEmailAddress == userEmail)
                .ToList();

            return View("VerifyOwner", ownedRestaurants);
        }

        public async Task<IActionResult> VerifyRestaurantItems(
            int id,
            [FromKeyedServices("Db")] IItemsRepository repo)
        {
            var allItems = await repo.GetAllAsync();

            var restaurant = allItems
                .OfType<Restaurant>()
                .FirstOrDefault(r => r.Id == id);

            if (restaurant == null)
                return NotFound();

            var userEmail = User.Claims
            .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)
            ?.Value;


            if (!IsAdmin() && restaurant.OwnerEmailAddress != userEmail)
                return Forbid();

            var pendingMenuItems = allItems
                .OfType<MenuItem>()
                .Where(m =>
                    m.RestaurantId == restaurant.Id &&
                    m.Status == "Pending")
                .ToList();

            return View("VerifyItems", pendingMenuItems);
        }

        // SE3.3 — Approve action
        [HttpPost]
        [ServiceFilter(typeof(ApprovalAuthorizationFilter))]
        public async Task<IActionResult> Approve(
            int[] restaurantIds,
            Guid[] menuItemIds,
            [FromKeyedServices("Db")] IItemsRepository repo)
        {
            await repo.ApproveAsync(restaurantIds, menuItemIds);
            return RedirectToAction(nameof(Catalog));
        }

        // Catalog view
        public async Task<IActionResult> Catalog(
            [FromKeyedServices("Db")] IItemsRepository repo)
        {
            var items = await repo.GetAllAsync();
            return View(items);
        }
    }
}
