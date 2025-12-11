using EnterpriseHomeAssignment.Factories;
using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace EnterpriseHomeAssignment.Controllers
{
    public class BulkImportController : Controller
    {
        private readonly ImportItemFactory _factory;

        public BulkImportController(ImportItemFactory factory)
        {
            _factory = factory;
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
            [FromKeyedServices("InMemory")] IItemsRepository tempRepo,
            IWebHostEnvironment env)
        {
            var items = await tempRepo.GetAllAsync();

            var stream = new MemoryStream();

            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                string defaultImage = Path.Combine(env.WebRootPath, "default.jpg");
                byte[] imgBytes = System.IO.File.ReadAllBytes(defaultImage);

                foreach (var item in items)
                {
                    string id = "";

                    if (item is Restaurant r)
                        id = r.ExternalId;
                    if (item is MenuItem m)
                        id = m.ExternalId;

                    var entry = zip.CreateEntry($"item-{id}/default.jpg");

                    using var entryStream = entry.Open();
                    entryStream.Write(imgBytes, 0, imgBytes.Length);
                }
            }

            stream.Position = 0;

            return File(stream, "application/zip", "images-template.zip");
        }

        [HttpPost]
        public async Task<IActionResult> Commit(IFormFile imagesZip,
            IWebHostEnvironment env,
            [FromKeyedServices("InMemory")] IItemsRepository tempRepo,
            [FromKeyedServices("Db")] IItemsRepository dbRepo)
        {
            if (imagesZip == null || imagesZip.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a ZIP file.");
                return RedirectToAction("Preview");
            }

            var items = (await tempRepo.GetAllAsync()).ToList();

            // Extract zip
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            using (var archive = new ZipArchive(imagesZip.OpenReadStream()))
            {
                archive.ExtractToDirectory(tempPath, true);
            }

            // Assign images
            string imagesFolder = Path.Combine(env.WebRootPath, "images");
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

            await dbRepo.SaveAsync(items);

            if (tempRepo is EnterpriseHomeAssignment.Repositories.ItemsInMemoryRepository repo)
            {
                repo.Clear();
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
