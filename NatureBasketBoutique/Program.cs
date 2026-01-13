using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using NatureBasketBoutique.Data;
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Repository;
using NatureBasketBoutique.Repository.IRepository;
using NatureBasketBoutique.Utility;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders(); // Added this (good practice for emails/resets)

// IMPORTANT: Since you are using a CUSTOM Controller for Account, 
// you might want to change these paths to "/Customer/Account/Login" later.
// For now, I will leave them pointing to Identity defaults.
builder.Services.ConfigureApplicationCookie(options =>
{
    // Update these paths to point to your new Custom Controller
    options.LoginPath = $"/Customer/Account/Login";
    options.LogoutPath = $"/Customer/Account/Logout";
    options.AccessDeniedPath = $"/Customer/Account/AccessDenied";
});

// 3. Register Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 4. Configure Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 5. Auth & Session
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// 6. Routing Maps
app.MapRazorPages(); // Required for Identity partials if you use them

// This handles your Areas (Admin, Customer)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// This handles default fallbacks
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

// 7. Seed Database (MOVED BEFORE app.Run)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        // Helpful to print error to console if seeding fails
        Console.WriteLine("Error seeding DB: " + ex.Message);
    }
}

app.Run(); // <--- ONLY ONE app.Run() AT THE VERY END