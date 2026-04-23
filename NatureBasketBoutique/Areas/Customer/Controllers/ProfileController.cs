using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Repository.IRepository;
using System.Security.Claims;

namespace NatureBasketBoutique.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize] // Must be logged in to see profile
    public class ProfileController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProfileController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Fetch the user from the database
            var applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            if (applicationUser == null) return NotFound();

            return View(applicationUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(ApplicationUser userObj)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var userFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            if (userFromDb == null) return NotFound();

            // Update the details
            userFromDb.Name = userObj.Name;
            userFromDb.PhoneNumber = userObj.PhoneNumber;
            userFromDb.StreetAddress = userObj.StreetAddress;
            userFromDb.City = userObj.City;
            userFromDb.State = userObj.State;
            userFromDb.PostalCode = userObj.PostalCode;

          //  _unitOfWork.ApplicationUser.Update(userFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}