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
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // 1. ORDER HISTORY (Index)
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            IEnumerable<OrderHeader> orderHeaders = _unitOfWork.OrderHeader
                .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser")
                .OrderByDescending(u => u.Id); // Newest first

            return View(orderHeaders);
        }

        // 2. ORDER DETAILS & TRACKING
        public IActionResult Details(int orderId)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser");

            // Security: Ensure user can only see their OWN orders
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            if (orderHeader == null || orderHeader.ApplicationUserId != userId)
            {
                return NotFound();
            }

            OrderVM orderVM = new()
            {
                OrderHeader = orderHeader,
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };

            return View(orderVM);
        }

        // 3. DOWNLOAD INVOICE
        public IActionResult Invoice(int orderId)
        {
            // 1. Fetch Header
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser");

            // 2. Security Check
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            if (orderHeader == null || orderHeader.ApplicationUserId != userId)
            {
                return NotFound();
            }

            // 3. Fetch Products (CRITICAL STEP)
            IEnumerable<OrderDetail> orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product");

            // 4. Create Model
            OrderVM orderVM = new()
            {
                OrderHeader = orderHeader,
                OrderDetail = orderDetails
            };

            // 5. FORCE USE OF ADMIN VIEW
            // The "~" symbol tells it to look from the root of the project
            return View("~/Areas/Admin/Views/Order/Invoice.cshtml", orderVM);
        }


        // 4. CANCEL ORDER (Customer Policy)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int orderId)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);

            // Security Check
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            if (orderHeader == null || orderHeader.ApplicationUserId != userId) return NotFound();

            // Policy: Can only cancel if NOT shipped yet
            if (orderHeader.OrderStatus != SD.StatusShipped && orderHeader.OrderStatus != SD.StatusCancelled)
            {
                // Simple Cancellation
                _unitOfWork.OrderHeader.UpdateStatus(orderId, SD.StatusCancelled);
                _unitOfWork.Save();
                TempData["Success"] = "Order Cancelled Successfully.";
            }
            else
            {
                TempData["Error"] = "Order cannot be cancelled as it has already been shipped.";
            }

            return RedirectToAction(nameof(Details), new { orderId = orderId });
        }

        // 5. REORDER (Add items back to cart)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reorder(int orderId)
        {
            var orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId);

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            foreach (var item in orderDetails)
            {
                ShoppingCart cart = new()
                {
                    ProductId = item.ProductId,
                    ApplicationUserId = userId,
                    Count = item.Count
                };
                _unitOfWork.ShoppingCart.Add(cart);
            }

            _unitOfWork.Save();
            TempData["Success"] = "Items added to cart successfully!";

            return RedirectToAction("Index", "Cart");
        }
    }
}