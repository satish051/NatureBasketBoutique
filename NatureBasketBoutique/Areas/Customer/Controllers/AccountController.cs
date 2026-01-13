using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Models.ViewModels;
using NatureBasketBoutique.Utility; // Assumes you have a SD (Static Details) class for Role Names

namespace NatureBasketBoutique.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // 1. CONSTRUCTOR: Injecting the services we need
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // 2. GET: Show the Registration Form
        [HttpGet]
        public IActionResult Register()
        {
            // Optional: If roles don't exist, create them (quick fix for testing)
            // In production, this should be done in DbInitializer
            /*
            if (!_roleManager.RoleExistsAsync("Customer").GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole("Customer")).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
            }
            */

            return View();
        }

        // 3. POST: Process the Registration Form
        [HttpPost]
        [ValidateAntiForgeryToken] // Security feature
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // A. Create the User Object
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name // Maps the Name from the form to the Database
                };

                // B. Save to Database (Password is hashed automatically here)
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // C. Assign a Role (Default to "Customer")
                    // Ensure the string "Customer" matches exactly what is in your Database/Seeder
                    await _userManager.AddToRoleAsync(user, "Customer");

                    // D. Sign the user in immediately
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // E. Redirect to Home Page
                    return RedirectToAction("Index", "Home", new { area = "Customer" });
                }

                // F. Handle Errors (e.g., Password too weak, Email already exists)
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got here, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            // Redirect to the Home page after logging out
            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }

        // 4. GET: Show the Login Form
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 5. POST: Process the Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Redirect to Home Page upon success
                    return RedirectToAction("Index", "Home", new { area = "Customer" });
                }

                // Handle specific failure scenarios
                if (result.IsLockedOut)
                {
                    // You would create a Lockout view for this, or just return an error
                    ModelState.AddModelError(string.Empty, "This account has been locked out.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }

            // If we got here, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Get the current logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Reuse the RegisterViewModel to display the data (or create a separate ProfileVM)
            var model = new RegisterViewModel
            {
                Name = user.Name,
                Email = user.Email
                // We don't send the password back for security reasons
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(RegisterViewModel model)
        {
            // We fetch the current user from the Database to make sure we are editing the right person
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Since we are reusing RegisterViewModel, we need to ignore Password validation
            // because the user isn't resetting their password here.
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

            if (ModelState.IsValid)
            {
                // 1. Update the fields
                user.Name = model.Name;
                user.Email = model.Email;
                user.UserName = model.Email; // Sync Username with Email

                // 2. Save to Database
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // CRITICAL STEP: Refresh the sign-in cookie
                    // If we don't do this, changing the SecurityStamp (which happens on update) 
                    // will force the user to log out immediately.
                    await _signInManager.RefreshSignInAsync(user);

                    TempData["success"] = "Profile updated successfully!";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

    }
}