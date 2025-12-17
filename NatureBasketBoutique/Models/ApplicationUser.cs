using Microsoft.AspNetCore.Identity;

namespace NatureBasketBoutique.Models
{
    public class ApplicationUser : IdentityUser
    {
        // We will add properties like FullName, Address, PostalCode here later.
        public string? FullName { get; set; }
    }
}