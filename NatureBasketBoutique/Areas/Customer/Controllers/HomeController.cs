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

        public IActionResult Index(string? searchString)
        {
            IEnumerable<Product> productList;

            if (!string.IsNullOrEmpty(searchString))
            {
                // Filter by Title OR ISBN (Case Insensitive usually handled by DB)
                productList = _unitOfWork.Product.GetAll(u => u.Title.Contains(searchString) ||
                                                              u.ISBN.Contains(searchString),
                                                         includeProperties: "Category");
            }
            else
            {
                // No search? Return everything
                productList = _unitOfWork.Product.GetAll(includeProperties: "Category");
            }

            return View(productList);
        }

        // --- 1. GET: Display Product Details ---
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

        // --- 2. POST: Add to Cart ---
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
                    cartFromDb.Count += shoppingCart.Count;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                }
                else
                {
                    _unitOfWork.ShoppingCart.Add(shoppingCart);
                }
                _unitOfWork.Save();
            }
            else
            {
                // ================= GUEST USER (Session Logic) =================
                List<ShoppingCart> sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart") ?? new List<ShoppingCart>();

                // Check if item already exists in session
                var existingItem = sessionCart.FirstOrDefault(u => u.ProductId == shoppingCart.ProductId);
                if (existingItem != null)
                {
                    existingItem.Count += shoppingCart.Count;
                }
                else
                {
                    // We only store IDs in session, we will load Product details later
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