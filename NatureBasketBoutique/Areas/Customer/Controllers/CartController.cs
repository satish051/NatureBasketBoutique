using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Repository.IRepository;
using NatureBasketBoutique.Utility;
using NatureBasketBoutique.ViewModels;
using System.Security.Claims;

namespace NatureBasketBoutique.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = new List<ShoppingCart>(),
                OrderHeader = new OrderHeader()
            };

            if (claim != null)
            {
                ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,
                    includeProperties: "Product");
            }
            else
            {
                var sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart") ?? new List<ShoppingCart>();
                foreach (var item in sessionCart)
                {
                    item.Product = _unitOfWork.Product.Get(u => u.Id == item.ProductId);
                    if (item.Product != null) item.Price = item.Product.Price;
                }
                ShoppingCartVM.ShoppingCartList = sessionCart;
            }

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = cart.Product.Price;
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Plus(int cartId, int productId)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
                cartFromDb.Count += 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
            }
            else
            {
                var sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart");
                var item = sessionCart.FirstOrDefault(u => u.ProductId == productId);
                if (item != null) item.Count += 1;
                HttpContext.Session.Set("SessionCart", sessionCart);
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId, int productId)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
                if (cartFromDb.Count <= 1)
                {
                    _unitOfWork.ShoppingCart.Remove(cartFromDb);
                }
                else
                {
                    cartFromDb.Count -= 1;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                }
                _unitOfWork.Save();
            }
            else
            {
                var sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart");
                var item = sessionCart.FirstOrDefault(u => u.ProductId == productId);
                if (item != null)
                {
                    if (item.Count <= 1) sessionCart.Remove(item);
                    else item.Count -= 1;
                }
                HttpContext.Session.Set("SessionCart", sessionCart);
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId, int productId)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                _unitOfWork.Save();
            }
            else
            {
                var sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart");
                var item = sessionCart.FirstOrDefault(u => u.ProductId == productId);
                if (item != null) sessionCart.Remove(item);
                HttpContext.Session.Set("SessionCart", sessionCart);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Summary (Checkout Page)
        [Authorize]
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };

            // --- SECURITY CHECK: PREVENT EMPTY CART CHECKOUT ---
            if (ShoppingCartVM.ShoppingCartList.Count() == 0)
            {
                // Optional: Add a notification message here if you have Toastr set up
                // TempData["error"] = "Your cart is empty!";
                return RedirectToAction(nameof(Index));
            }
            // ---------------------------------------------------

            var applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            ShoppingCartVM.OrderHeader.ApplicationUser = applicationUser;
            ShoppingCartVM.OrderHeader.Name = applicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = applicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = applicationUser.City;
            ShoppingCartVM.OrderHeader.State = applicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = applicationUser.PostalCode;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = cart.Product.Price;
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        // --- NEW: PLACE ORDER LOGIC ---
        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // 1. Load Cart Items (Because 'BindProperty' only binds the Header fields)
            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product");

            // 2. Set Order Data
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

            // 3. Calculate Total
            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = cart.Product.Price;
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            // 4. Set Status 
            // Since we don't have Stripe/PayPal yet, we default to "Pending"
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;

            // 5. Save Order Header
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            // 6. Save Order Details (The Items)
            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            // 7. Clear Shopping Cart
            _unitOfWork.ShoppingCart.RemoveRange(ShoppingCartVM.ShoppingCartList);
            _unitOfWork.Save();

            // 8. Redirect to Confirmation
            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
        }

        // --- NEW: CONFIRMATION PAGE ---
        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }
    }
}