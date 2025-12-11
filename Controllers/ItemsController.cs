using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace EnterpriseHomeAssignment.Controllers
{
    public class ItemsController : Controller
    {
        private readonly IItemsRepository _dbRepo;

        public ItemsController([FromKeyedServices("Db")] IItemsRepository dbRepo)
        {
            _dbRepo = dbRepo;
        }

        public async Task<IActionResult> Catalog(string view = "card")
        {
            var items = (await _dbRepo.GetAllAsync()).Where(i =>
            {
                if (i is Restaurant r) return r.Status == "Approved";
                if (i is MenuItem m) return m.Status == "Approved";
                return false;
            });

            // pass through view querystring
            return View(items);
        }
    }
}
