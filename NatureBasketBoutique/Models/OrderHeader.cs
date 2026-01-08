using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NatureBasketBoutique.Models
{
    public class OrderHeader
    {
        public int Id { get; set; }

        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime ShippingDate { get; set; }
        public double OrderTotal { get; set; }

        // Order Status
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }

        // Shipping Details
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string StreetAddress { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        public string PostalCode { get; set; }
        [Required]
        public string Name { get; set; }

        // --- NEW: Smart Display ID Logic ---
        [NotMapped]
        public string DisplayId
        {
            get
            {
                // LOGIC: 
                // If ID is small (e.g. 5), it shows "5"
                // If ID is > 100, it shows "A-101"
                // If ID is > 1000, it shows "B-1001"

                if (Id <= 100)
                {
                    return Id.ToString();
                }
                else if (Id > 100 && Id <= 1000)
                {
                    return $"A-{Id}";
                }
                else
                {
                    // For very large numbers, switch to B series
                    return $"B-{Id}";
                }
            }
        }
    }
}