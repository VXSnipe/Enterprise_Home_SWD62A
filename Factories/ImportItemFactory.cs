using EnterpriseHomeAssignment.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace EnterpriseHomeAssignment.Factories
{
    public class ImportItemFactory
    {
        public List<IItemValidating> Create(string json)
        {
            var items = new List<IItemValidating>();
            var doc = JsonDocument.Parse(json);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var type = element.GetProperty("type").GetString()?.ToLowerInvariant();

                if (type == "restaurant")
                {
                    var restaurant = new Restaurant
                    {
                        ExternalId = element.GetProperty("externalId").GetString(),
                        Name = element.GetProperty("name").GetString(),
                        OwnerEmailAddress = element.GetProperty("ownerEmailAddress").GetString(),
                        Description = element.TryGetProperty("description", out var d) ? d.GetString() : null,
                        Address = element.TryGetProperty("address", out var a) ? a.GetString() : null,
                        Phone = element.TryGetProperty("phone", out var p) ? p.GetString() : null,
                        Status = "Pending"
                    };

                    items.Add(restaurant);
                }
                else if (type == "menuitem")
                {
                    var menuItem = new MenuItem
                    {
                        ExternalId = element.GetProperty("externalId").GetString(),
                        Title = element.GetProperty("title").GetString(),
                        Price = element.GetProperty("price").GetDecimal(),
                        RestaurantExternalId = element.GetProperty("restaurantId").GetString(),
                        Status = "Pending"
                    };

                    items.Add(menuItem);
                }
            }

            return items;
        }
    }
}
