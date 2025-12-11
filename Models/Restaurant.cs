using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseHomeAssignment.Models
{
    public class Restaurant : IItemValidating
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }   // int identity

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string OwnerEmailAddress { get; set; }

        // Pending / Approved
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // Optional extra fields from JSON
        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(300)]
        public string Address { get; set; }

        [MaxLength(50)]
        public string Phone { get; set; }

        // Path to image in wwwroot
        public string ImagePath { get; set; }

        public ICollection<MenuItem> MenuItems { get; set; }

        // Implementation of IItemValidating
        public List<string> GetValidators()
        {
            // Site admin email will be in appsettings.json
            return new List<string> { "admin@site.com" };
        }

        public string GetCardPartial() => "_RestaurantCard";
    }
}
