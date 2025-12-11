using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseHomeAssignment.Models
{
    public class MenuItem : IItemValidating
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }  // Guid

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal Price { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public string ImagePath { get; set; }

        // Implementation of IItemValidating
        public List<string> GetValidators()
        {
            // Restaurant owner is the validator
            return Restaurant != null
                ? new List<string> { Restaurant.OwnerEmailAddress }
                : new List<string>();
        }

        public string GetCardPartial() => "_MenuItemRow";
    }
}
