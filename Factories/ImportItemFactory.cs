using EnterpriseHomeAssignment.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace EnterpriseHomeAssignment.Factories
{
    public class ImportItemFactory
    {
        public List<object> Create(string json)
        {
            var items = new List<object>();
            var doc = JsonDocument.Parse(json);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var type = element.GetProperty("type").GetString();

                if (type == "restaurant")
                {
                    var restaurant = new Restaurant
                    {
                        Name = element.GetProperty("name").GetString(),
                        OwnerEmailAddress = element.GetProperty("ownerEmailAddress").GetString(),
                        Description = element.TryGetProperty("description", out var d) ? d.GetString() : null,
                        Address = element.TryGetProperty("address", out var a) ? a.GetString() : null,
                        Phone = element.TryGetProperty("phone", out var p) ? p.GetString() : null,
                        Status = "Pending"
                    };

                    items.Add(restaurant);
                }
                else if (type == "menuItem")
                {
                    var menuItem = new MenuItem
                    {
                        Title = element.GetProperty("title").GetString(),
                        Price = element.GetProperty("price").GetDecimal(),
                        RestaurantId = element.GetProperty("restaurantId").GetInt32(),
                        Status = "Pending"
                    };

                    items.Add(menuItem);
                }
            }

            return items;
        }
    }
}
