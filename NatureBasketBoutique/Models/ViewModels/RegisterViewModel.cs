using System.ComponentModel.DataAnnotations;

namespace NatureBasketBoutique.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string Name { get; set; }  // <--- ADD THIS NEW PROPERTY

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}