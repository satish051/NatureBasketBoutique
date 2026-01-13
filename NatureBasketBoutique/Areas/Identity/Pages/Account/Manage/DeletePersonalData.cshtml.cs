// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NatureBasketBoutique.Data; // Ensure this matches your DbContext namespace
using NatureBasketBoutique.Models;
using System.Linq;

namespace NatureBasketBoutique.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;
        private readonly ApplicationDbContext _db; // 1. Add DB Context

        public DeletePersonalDataModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<DeletePersonalDataModel> logger,
            ApplicationDbContext db) // 2. Inject DB Context
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Incorrect password.");
                    return Page();
                }
            }

            // =============================================================
            // FIX STARTS HERE: Delete Related Data First
            // =============================================================
            var userId = user.Id;

            // 1. Remove Shopping Cart Items
            var cartItems = _db.ShoppingCarts.Where(u => u.ApplicationUserId == userId).ToList();
            if (cartItems.Any())
            {
                _db.ShoppingCarts.RemoveRange(cartItems);
            }

            // 2. Remove Order Headers (and Order Details via Cascade if configured, otherwise manual)
            // Note: If you want to KEEP order history for business records, you would set ApplicationUserId to null here instead.
            // But for "Delete Personal Data", we usually wipe it.

            var orders = _db.OrderHeaders.Where(u => u.ApplicationUserId == userId).ToList();
            foreach (var order in orders)
            {
                // Manually delete OrderDetails first to be safe
                var details = _db.OrderDetails.Where(u => u.OrderHeaderId == order.Id).ToList();
                _db.OrderDetails.RemoveRange(details);

                // Then delete the OrderHeader
                _db.OrderHeaders.Remove(order);
            }

            // Save these changes BEFORE deleting the user
            await _db.SaveChangesAsync();
            // =============================================================

            // 3. Now it is safe to delete the User
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unexpected error occurred deleting user with ID '{userId}'.");
            }

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

            return Redirect("~/");
        }
    }
}