using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Repository.IRepository;
using NatureBasketBoutique.Utility; // Needed for SessionExtensions
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
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = new List<ShoppingCart>(),
                OrderHeader = new OrderHeader()
            };

            if (userId != null)
            {
                // LOGGED IN: Check for session items to merge
                MergeSessionCart(userId);

                // Load from DB
                ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                    includeProperties: "Product");
            }
            else
            {
                // GUEST: Load from Session
                List<ShoppingCart> sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart");
                if (sessionCart != null)
                {
                    foreach (var item in sessionCart)
                    {
                        item.Product = _unitOfWork.Product.Get(u => u.Id == item.ProductId);
                    }
                    ShoppingCartVM.ShoppingCartList = sessionCart;
                }
            }

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = cart.Product.Price;
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        // GET: Checkout Page
        [Authorize] // Forces Login
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // CRITICAL: Merge items if they just logged in and came straight here
            MergeSessionCart(userId);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                                                                 includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };

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

        // POST: Place Order
        [HttpPost]
        [ActionName("Summary")]
        [Authorize]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                                                                             includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = cart.Product.Price;
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

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
            }
            _unitOfWork.Save();

            _unitOfWork.ShoppingCart.RemoveRange(ShoppingCartVM.ShoppingCartList);
            _unitOfWork.Save();

            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }

        // --- HELPER METHOD TO MERGE CART ---
        private void MergeSessionCart(string userId)
        {
            List<ShoppingCart> sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart");
            if (sessionCart != null && sessionCart.Count > 0)
            {
                foreach (var item in sessionCart)
                {
                    item.ApplicationUserId = userId;
                    item.Id = 0; // Reset ID so DB creates a new entry

                    var existingDbItem = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId && u.ProductId == item.ProductId);
                    if (existingDbItem != null)
                    {
                        // Update existing item count
                        existingDbItem.Count += item.Count;
                        _unitOfWork.ShoppingCart.Update(existingDbItem);
                    }
                    else
                    {
                        // Add new item
                        _unitOfWork.ShoppingCart.Add(item);
                    }
                }
                _unitOfWork.Save();
                HttpContext.Session.Remove("SessionCart"); // Clear session
            }
        }

        // --- CART ACTIONS (Plus/Minus/Remove) ---
        // Note: For full guest support, these need session logic too.
        // For now, we assume users manage cart mostly after login or we can add that next.
        public IActionResult Plus(int cartId, int productId)
        {
            if (cartId == 0)
            {
                // ============ GUEST LOGIC (Session) ============
                List<ShoppingCart> sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart");
                var cartItem = sessionCart.FirstOrDefault(u => u.ProductId == productId);
                if (cartItem != null)
                {
                    cartItem.Count += 1;
                    HttpContext.Session.Set("SessionCart", sessionCart);
                }
            }
            else
            {
                // ============ LOGGED IN LOGIC (Database) ============
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
                cartFromDb.Count += 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId, int productId)
        {
            if (cartId == 0)
            {
                // ============ GUEST LOGIC (Session) ============
                List<ShoppingCart> sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart");
                var cartItem = sessionCart.FirstOrDefault(u => u.ProductId == productId);
                if (cartItem != null)
                {
                    if (cartItem.Count <= 1)
                    {
                        sessionCart.Remove(cartItem);
                    }
                    else
                    {
                        cartItem.Count -= 1;
                    }
                    HttpContext.Session.Set("SessionCart", sessionCart);
                }
            }
            else
            {
                // ============ LOGGED IN LOGIC (Database) ============
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
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId, int productId)
        {
            if (cartId == 0)
            {
                // ============ GUEST LOGIC (Session) ============
                List<ShoppingCart> sessionCart = HttpContext.Session.Get<List<ShoppingCart>>("SessionCart");
                var cartItem = sessionCart.FirstOrDefault(u => u.ProductId == productId);
                if (cartItem != null)
                {
                    sessionCart.Remove(cartItem);
                    HttpContext.Session.Set("SessionCart", sessionCart);
                }
            }
            else
            {
                // ============ LOGGED IN LOGIC (Database) ============
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}