using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Repository.IRepository;
using NatureBasketBoutique.Utility;
using System.Diagnostics;
using System.Security.Claims;

namespace NatureBasketBoutique.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        // --- 1. HOME PAGE (With Search Logic) ---
        public IActionResult Index(string? searchString)
        {
            IEnumerable<Product> productList;

            if (!string.IsNullOrEmpty(searchString))
            {
                // This is the SEARCH LOGIC
                productList = _unitOfWork.Product.GetAll(u => u.Title.Contains(searchString) ||
                                                              u.Description.Contains(searchString), // Expanded search to description
                                                              includeProperties: "Category");
            }
            else
            {
                productList = _unitOfWork.Product.GetAll(includeProperties: "Category");
            }

            return View(productList);
        }

        // --- SHOP PAGE (Multi-Category Filter) ---
        public IActionResult Shop(string? searchString, string? sortOrder, int[]? categoryIds)
        {
            // 1. Pass Data to View
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentSearch"] = searchString;
            ViewData["SelectedCategories"] = categoryIds; // Store array to check boxes in View

            // Load Categories
            ViewBag.Categories = _unitOfWork.Category.GetAll().OrderBy(u => u.DisplayOrder).ToList();

            IEnumerable<Product> productList;

            // 2. FETCH ALL PRODUCTS
            productList = _unitOfWork.Product.GetAll(includeProperties: "Category");

            // 3. APPLY FILTERS

            // Filter by Multiple Categories
            if (categoryIds != null && categoryIds.Length > 0)
            {
                // Keep products where the CategoryId is in the selected list
                productList = productList.Where(u => categoryIds.Contains(u.CategoryId));
            }

            // Filter by Search
            if (!string.IsNullOrEmpty(searchString))
            {
                productList = productList.Where(u => u.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                                                     u.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            // 4. APPLY SORTING
            switch (sortOrder)
            {
                case "price_asc":
                    productList = productList.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    productList = productList.OrderByDescending(p => p.Price);
                    break;
                case "newest":
                    productList = productList.OrderByDescending(p => p.Id);
                    break;
                default:
                    productList = productList.OrderBy(p => p.Title);
                    break;
            }

            return View(productList);
        }

        // --- 2. GET: Display Product Details ---
        public IActionResult Details(int productId)
        {
            ShoppingCart cart = new()
            {
                Product = _unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Category"),
                Count = 1,
                ProductId = productId
            };
            return View(cart);
        }

        // --- 3. POST: Add to Cart (Handles both Guest & Logged-in Users) ---
        [HttpPost]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                // ================= LOGGED IN USER (Database Logic) =================
                shoppingCart.ApplicationUserId = claim.Value;

                ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == claim.Value &&
                                                                        u.ProductId == shoppingCart.ProductId);
                if (cartFromDb != null)
                {
                    // Item exists in cart -> Update count
                    cartFromDb.Count += shoppingCart.Count;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                }
                else
                {
                    // New item -> Add to DB
                    _unitOfWork.ShoppingCart.Add(shoppingCart);
                }
                _unitOfWork.Save();
            }
            else
            {
                // ================= GUEST USER (Session Logic) =================
                // Note: Requires SessionExtensions class in Utility folder
                List<ShoppingCart> sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart") ?? new List<ShoppingCart>();

                // Check if item already exists in session
                var existingItem = sessionCart.FirstOrDefault(u => u.ProductId == shoppingCart.ProductId);
                if (existingItem != null)
                {
                    existingItem.Count += shoppingCart.Count;
                }
                else
                {
                    // We only store IDs in session, we will load Product details later in the Cart Controller
                    shoppingCart.Id = 0; // Temp ID
                    sessionCart.Add(shoppingCart);
                }

                HttpContext.Session.Set("SessionCart", sessionCart);
            }

            TempData["success"] = "Cart updated successfully";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}