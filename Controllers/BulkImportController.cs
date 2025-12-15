using EnterpriseHomeAssignment.Factories;
using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System;

namespace EnterpriseHomeAssignment.Controllers
{
    public class BulkImportController : Controller
    {
        private readonly ImportItemFactory _factory;
        private readonly IWebHostEnvironment _env;

        public BulkImportController(ImportItemFactory factory, IWebHostEnvironment env)
        {
            _factory = factory;
            _env = env;
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile jsonFile,
            [FromKeyedServices("InMemory")] IItemsRepository tempRepo)
        {
            if (jsonFile == null || jsonFile.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a JSON file.");
                return View();
            }

            using var reader = new StreamReader(jsonFile.OpenReadStream());
            string json = await reader.ReadToEndAsync();

            var items = _factory.Create(json);

            await tempRepo.SaveAsync(items);

            return RedirectToAction("Preview");
        }

        public async Task<IActionResult> Preview(
            [FromKeyedServices("InMemory")] IItemsRepository tempRepo)
        {
            var items = await tempRepo.GetAllAsync();
            return View(items);
        }

        public async Task<IActionResult> DownloadZip(
            [FromKeyedServices("InMemory")] IItemsRepository tempRepo)
        {
            var items = await tempRepo.GetAllAsync();

            // Check if default.jpg exists, if not create a placeholder
            string defaultImagePath = Path.Combine(_env.WebRootPath, "default.jpg");
            if (!System.IO.File.Exists(defaultImagePath))
            {
                Directory.CreateDirectory(_env.WebRootPath);
                // Create a minimal 1x1 pixel placeholder image
                byte[] placeholderImage = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
                await System.IO.File.WriteAllBytesAsync(defaultImagePath, placeholderImage);
            }

            var stream = new MemoryStream();

            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                byte[] imgBytes = await System.IO.File.ReadAllBytesAsync(defaultImagePath);

                foreach (var item in items)
                {
                    string id = "";

                    if (item is Restaurant r)
                        id = r.ExternalId;
                    else if (item is MenuItem m)
                        id = m.ExternalId;

                    if (!string.IsNullOrEmpty(id))
                    {
                        var entry = zip.CreateEntry($"item-{id}/default.jpg");
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(imgBytes, 0, imgBytes.Length);
                    }
                }
            }

            stream.Position = 0;
            return File(stream, "application/zip", "images-template.zip");
        }

        [HttpPost]
        public async Task<IActionResult> Commit(IFormFile imagesZip,
            [FromKeyedServices("InMemory")] IItemsRepository tempRepo,
            [FromKeyedServices("Db")] IItemsRepository dbRepo)
        {
            if (imagesZip == null || imagesZip.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a ZIP file.");
                return RedirectToAction("Preview");
            }

            var items = (await tempRepo.GetAllAsync()).ToList();

            // Resolve MenuItem → Restaurant relationships BEFORE saving
            var restaurants = items.OfType<Restaurant>().ToList();
            var menuItems = items.OfType<MenuItem>().ToList();

            foreach (var menuItem in menuItems)
            {
                if (!string.IsNullOrEmpty(menuItem.RestaurantExternalId))
                {
                    var matchedRestaurant = restaurants.FirstOrDefault(r => r.ExternalId == menuItem.RestaurantExternalId);
                    if (matchedRestaurant != null)
                    {
                        menuItem.Restaurant = matchedRestaurant;
                        // RestaurantId will be set by EF when restaurant is saved first
                    }
                }
            }

            // Extract ZIP to a temp directory
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            using (var archive = new ZipArchive(imagesZip.OpenReadStream()))
            {
                archive.ExtractToDirectory(tempPath, true);
            }

            // Save images to /wwwroot/images/
            string imagesFolder = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(imagesFolder);

            foreach (var folder in Directory.GetDirectories(tempPath))
            {
                string externalId = Path.GetFileName(folder).Replace("item-", "");

                string file = Directory.GetFiles(folder).FirstOrDefault();
                if (file == null) continue;

                string newName = Guid.NewGuid().ToString() + Path.GetExtension(file);
                string dest = Path.Combine(imagesFolder, newName);

                System.IO.File.Copy(file, dest, true);

                string path = "/images/" + newName;

                foreach (var item in items)
                {
                    if (item is Restaurant r && r.ExternalId == externalId)
                        r.ImagePath = path;

                    if (item is MenuItem m && m.ExternalId == externalId)
                        m.ImagePath = path;
                }
            }

            // Save to DB with resolved relationships
            await dbRepo.SaveAsync(items);

            // Clear in-memory cache
            if (tempRepo is EnterpriseHomeAssignment.Repositories.ItemsInMemoryRepository repo)
            {
                repo.Clear();
            }

            return RedirectToAction("Catalog", "Items");
        }
    }
}
