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
            var items = (await tempRepo.GetAllAsync()).ToList();

            if (!items.Any())
            {
                TempData["Error"] = "No items to export. Please upload JSON first.";
                return RedirectToAction("Upload");
            }

            var stream = new MemoryStream();

            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                // Create a simple 1x1 red pixel JPEG as default image
                byte[] defaultImageBytes = new byte[] {
                    0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
                    0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
                    0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
                    0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x10, 0x01, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00,
                    0x7F, 0xFF, 0xD9
                };

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
                        using (var entryStream = entry.Open())
                        {
                            entryStream.Write(defaultImageBytes, 0, defaultImageBytes.Length);
                        }
                    }
                }
            }

            stream.Position = 0;
            return File(stream, "application/zip", "images.zip");
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

            // Resolve MenuItem → Restaurant relationships
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
                    }
                }
            }

            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            try
            {
                using (var archive = new ZipArchive(imagesZip.OpenReadStream()))
                {
                    archive.ExtractToDirectory(tempPath, true);
                }

                // Find the root folder containing item-* directories
                string searchPath = FindItemFoldersRoot(tempPath);

                // Find all item-* folders recursively
                var extractedFolders = FindAllItemFolders(searchPath);

                // Ensure wwwroot/images folder exists
                string imagesFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                // Process each folder
                foreach (var folder in extractedFolders)
                {
                    string folderName = Path.GetFileName(folder);

                    // Extract externalId - handle "item-" prefix
                    string externalId = folderName;
                    if (folderName.StartsWith("item-"))
                    {
                        externalId = folderName.Substring("item-".Length);
                    }

                    // Find image files
                    var imageFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => {
                            var ext = Path.GetExtension(f).ToLower();
                            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" ||
                                   ext == ".gif" || ext == ".bmp" || ext == ".webp";
                        })
                        .ToList();

                    string file = imageFiles.FirstOrDefault();
                    if (file == null) continue;

                    // Generate new filename
                    string originalExt = Path.GetExtension(file);
                    string newName = Guid.NewGuid().ToString() + originalExt.ToLower();
                    string dest = Path.Combine(imagesFolder, newName);
                    string path = "/images/" + newName;

                    System.IO.File.Copy(file, dest, true);

                    // Link image to items
                    foreach (var item in items)
                    {
                        if (item is Restaurant r && r.ExternalId == externalId)
                            r.ImagePath = path;
                        else if (item is MenuItem m && m.ExternalId == externalId)
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

                TempData["Success"] = $"Successfully imported {restaurants.Count} restaurants and {menuItems.Count} menu items.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing images: {ex.Message}";
                return RedirectToAction("Preview");
            }
            finally
            {
                // Cleanup temp directory
                if (Directory.Exists(tempPath))
                {
                    try { Directory.Delete(tempPath, true); } catch { }
                }
            }

            return RedirectToAction("Catalog", "Items", new { pending = true });
        }

        // Helper method to find the root folder containing item-* directories
        private string FindItemFoldersRoot(string basePath)
        {
            // Check if there are item-* folders directly in basePath
            var itemFolders = Directory.GetDirectories(basePath, "item-*");
            if (itemFolders.Length > 0)
                return basePath;

            // Recursively search through subdirectories
            var allDirectories = Directory.GetDirectories(basePath);

            foreach (var dir in allDirectories)
            {
                // Check this directory
                itemFolders = Directory.GetDirectories(dir, "item-*");
                if (itemFolders.Length > 0)
                    return dir;

                // Also check deeper
                var subDirs = Directory.GetDirectories(dir);
                foreach (var subDir in subDirs)
                {
                    itemFolders = Directory.GetDirectories(subDir, "item-*");
                    if (itemFolders.Length > 0)
                        return subDir;
                }
            }

            return basePath;
        }

        // Helper method to recursively find all item-* folders
        private List<string> FindAllItemFolders(string searchPath)
        {
            var result = new List<string>();

            // First, look for item-* folders in current directory
            var itemFolders = Directory.GetDirectories(searchPath, "item-*");
            result.AddRange(itemFolders);

            // Also look for folders that might be item folders without the prefix
            var allFolders = Directory.GetDirectories(searchPath);
            foreach (var folder in allFolders)
            {
                string folderName = Path.GetFileName(folder);
                // If it's not already an item-* folder, check if it looks like an item folder
                if (!folderName.StartsWith("item-") && (folderName == "r1" || folderName == "m1" ||
                    folderName.StartsWith("r-") || folderName.StartsWith("m-")))
                {
                    result.Add(folder);
                }
            }

            // If we didn't find any in current directory, search recursively
            if (result.Count == 0)
            {
                var allDirectories = Directory.GetDirectories(searchPath);
                foreach (var dir in allDirectories)
                {
                    result.AddRange(FindAllItemFolders(dir));
                }
            }

            return result;
        }
    }
}