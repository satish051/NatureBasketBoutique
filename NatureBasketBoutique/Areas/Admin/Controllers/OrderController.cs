using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Repository.IRepository;
using NatureBasketBoutique.Utility;
using NatureBasketBoutique.ViewModels;
using System.Security.Claims;

namespace NatureBasketBoutique.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // 1. LIST ORDERS
        public IActionResult Index()
        {
            IEnumerable<OrderHeader> objOrderHeaders;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }

            return View(objOrderHeaders);
        }

        // 2. VIEW DETAILS
        [HttpGet]
        public IActionResult Details(int orderId)
        {
            OrderVM orderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };

            return View(orderVM);
        }

        // 3. UPDATE DETAILS (With Error Diagnosis)
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderDetail(OrderVM orderVM)
        {
            // 1. Load Original Data
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);

            if (orderHeaderFromDb == null)
            {
                return NotFound();
            }

            // 2. Manual Update (Only specific fields)
            orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = orderVM.OrderHeader.City;
            orderHeaderFromDb.State = orderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;

            // 3. Update Carrier/Tracking if they are not null
            if (!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            }

            try
            {
                // 4. Save Changes (Wrappped in Try-Catch)
                _unitOfWork.Save();

                TempData["Success"] = "Order Details Updated Successfully.";
            }
            catch (Exception ex)
            {
                // This will capture the REAL error and show it on your screen
                string innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["Error"] = $"Save Failed: {innerMessage}";
            }

            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }

        // 4. START PROCESSING
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing(OrderVM orderVM)
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();

            TempData["Success"] = "Order Status: In Process";
            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }

        // 5. SHIP ORDER
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder(OrderVM orderVM)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);

            orderHeader.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = orderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();

            TempData["Success"] = "Order Shipped Successfully";
            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }

        // 6. CANCEL ORDER
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder(OrderVM orderVM)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);

            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                // Process Refund Logic here if using Stripe/Payment Gateway
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.Save();

            TempData["Success"] = "Order Cancelled Successfully";
            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }

        // 7. REOPEN ORDER (Revert Cancel)
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult RevertCancel(int orderId)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);

            if (orderHeader == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index)); // Safety fallback
            }

            // Only allow reverting if currently Cancelled or Refunded
            if (orderHeader.OrderStatus == SD.StatusCancelled || orderHeader.OrderStatus == SD.StatusRefunded)
            {
                // Revert status back to Approved
                _unitOfWork.OrderHeader.UpdateStatus(orderId, SD.StatusApproved);
                _unitOfWork.Save();

                TempData["Success"] = "Order has been reopened successfully.";
            }
            else
            {
                TempData["Error"] = "Only cancelled orders can be reopened.";
            }

            return RedirectToAction(nameof(Details), new { orderId = orderId });
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }

            // PROJECTION: Convert to a new shape to send 'displayId' to JavaScript
            var orderList = objOrderHeaders.Select(u => new {
                id = u.Id,
                displayId = u.DisplayId, // <--- THIS SENDS THE ALPHABET ID
                name = u.Name,
                phoneNumber = u.PhoneNumber,
                applicationUser = u.ApplicationUser,
                orderStatus = u.OrderStatus,
                orderTotal = u.OrderTotal
            });

            return Json(new { data = orderList });
        }
        #endregion

    }
}