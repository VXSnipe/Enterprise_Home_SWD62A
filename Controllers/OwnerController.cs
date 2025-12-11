using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Models;
using System;

namespace EnterpriseHomeAssignment.Controllers
{
    public class OwnerController : Controller
    {
        private readonly IItemsRepository _dbRepo;

        public OwnerController([FromKeyedServices("Db")] IItemsRepository dbRepo)
        {
            _dbRepo = dbRepo;
        }

        public async Task<IActionResult> VerifyRestaurantItems(int id)
        {
            var all = await _dbRepo.GetAllAsync();
            var items = all.OfType<MenuItem>().Where(m => m.RestaurantId == id && m.Status == "Pending");
            return View(items);
        }
    }
}
