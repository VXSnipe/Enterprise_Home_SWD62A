using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EnterpriseHomeAssignment.Models
{
    public class Restaurant : IItemValidating
    {
        public int Id { get; set; }

        // 👇 ADD THIS EXACT PROPERTY
        public string ExternalId { get; set; }   // REQUIRED for ZIP + mapping

        public string Name { get; set; }
        public string OwnerEmailAddress { get; set; }

        public string Description { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }

        public string Status { get; set; }
        public string ImagePath { get; set; }

        public List<MenuItem> MenuItems { get; set; }

        // IItemValidating implementation
        public List<string> GetValidators()
        {
            // TODO: read from configuration in real app; use default admin
            return new List<string> { "admin@example.com" };
        }

        public string GetCardPartial()
        {
            return "_RestaurantCard";
        }
    }
}
