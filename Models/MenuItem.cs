using System.ComponentModel.DataAnnotations;

namespace EnterpriseHomeAssignment.Models
{
    public class MenuItem : IItemValidating
    {
        public Guid Id { get; set; }

        [Required]
        public string ExternalId { get; set; } = null!;

        [Required]
        public string Title { get; set; } = null!;

        public decimal Price { get; set; }

        [Required]
        public string Status { get; set; } = null!;

        public string? ImagePath { get; set; }

        public int RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; } = null!;

        public string RestaurantExternalId { get; set; } = null!;

        public List<string> GetValidators()
        {
            return new List<string>
        {
            "admin@example.com",
            Restaurant.OwnerEmailAddress
        };
        }


        public string GetCardPartial()
        {
            return "_MenuItemCard";
        }
    }
}
