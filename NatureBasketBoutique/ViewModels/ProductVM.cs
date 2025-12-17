using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using NatureBasketBoutique.Models;

namespace NatureBasketBoutique.ViewModels
{
    public class ProductVM
    {
        public Product Product { get; set; } = new Product(); // Initialize it

        [ValidateNever]
        public IEnumerable<SelectListItem> CategoryList { get; set; } = Enumerable.Empty<SelectListItem>(); // Initialize it
    }
}