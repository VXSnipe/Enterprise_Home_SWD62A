using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EnterpriseHomeAssignment.Models
{
    public class Restaurant : IItemValidating
    {
        public int Id { get; set; }

        [Required]
        public string ExternalId { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        [Required, EmailAddress]
        public string OwnerEmailAddress { get; set; } = null!;

        [Required]
        public string Status { get; set; } = null!;

        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? ImagePath { get; set; }

        public List<MenuItem> MenuItems { get; set; } = new();

        public List<string> GetValidators()
        {
            return new List<string> { "admin@example.com" };
        }

        public string GetCardPartial()
        {
            return "_RestaurantCard";
        }
    }
}
