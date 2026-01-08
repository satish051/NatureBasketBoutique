using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Repository.IRepository;
using NatureBasketBoutique.ViewModels;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace NatureBasketBoutique.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return View(objProductList);
        }

        // GET: Upsert (Update + Insert)
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()
            };

            // If ID is null/0, it's CREATE
            if (id == null || id == 0)
            {
                return View(productVM);
            }
            else
            {
                // It's UPDATE
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id);
                return View(productVM);
            }
        }

        // POST: Upsert (Update + Insert)
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, IFormFile? file)
        {
            // Prevent crash if no image is uploaded for a new product
            if (file == null && productVM.Product.Id == 0)
            {
                ModelState.AddModelError("file", "Please upload an image for new products.");
            }

            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\products");

                    if (!Directory.Exists(productPath))
                        Directory.CreateDirectory(productPath);

                    // Delete old image if exists (Update scenario)
                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // --- IMAGE RESIZING LOGIC START ---
                    // Instead of simple CopyTo, we load, resize, and save using ImageSharp
                    using (var image = Image.Load(file.OpenReadStream()))
                    {
                        // Resize to 800x800 (Fit inside box, maintain aspect ratio)
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(800, 800),
                            Mode = ResizeMode.Max
                        }));

                        // Save the resized image to the server
                        string finalPath = Path.Combine(productPath, fileName);
                        image.Save(finalPath);
                    }
                    // --- IMAGE RESIZING LOGIC END ---

                    productVM.Product.ImageUrl = @"\images\products\" + fileName;
                }

                // Save Data
                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);
                    TempData["Success"] = "Product created successfully";
                }
                else
                {
                    _unitOfWork.Product.Update(productVM.Product);
                    TempData["Success"] = "Product updated successfully";
                }

                _unitOfWork.Save();
                return RedirectToAction("Index");
            }

            // Reload dropdown if validation fails
            productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
            return View(productVM);
        }

        // GET: Show Delete Confirmation Page
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Product? productFromDb = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "Category");

            if (productFromDb == null)
            {
                return NotFound();
            }

            return View(productFromDb);
        }

        // POST: Actually Delete the Product
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {
            Product? obj = _unitOfWork.Product.Get(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }

            // 1. Delete the old image from wwwroot
            if (!string.IsNullOrEmpty(obj.ImageUrl))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            // 2. Delete from Database
            _unitOfWork.Product.Remove(obj);
            _unitOfWork.Save();

            TempData["Success"] = "Product deleted successfully";
            return RedirectToAction("Index");
        }

        // API Call (Kept for future use)
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }
        #endregion
    }
}