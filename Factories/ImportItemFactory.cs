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
                        ExternalId = element.TryGetProperty("id", out var idp) ? idp.GetString() : null,
                        Name = element.TryGetProperty("name", out var np) ? np.GetString() : null,
                        OwnerEmailAddress = element.TryGetProperty("ownerEmailAddress", out var op) ? op.GetString() : null,
                        Description = element.TryGetProperty("description", out var dp) ? dp.GetString() : null,
                        Address = element.TryGetProperty("address", out var ap) ? ap.GetString() : null,
                        Phone = element.TryGetProperty("phone", out var pp) ? pp.GetString() : null,
                        Status = "Pending"
                    };

                    items.Add(restaurant);
                }
                else if (type == "menuitem" || type == "menuItem" || type == "menu-item")
                {
                    var menuItem = new MenuItem
                    {
                        ExternalId = element.TryGetProperty("id", out var idp) ? idp.GetString() : null,
                        Title = element.TryGetProperty("title", out var tp) ? tp.GetString() : null,
                        Price = element.TryGetProperty("price", out var pp) && pp.TryGetDecimal(out var dec) ? dec : 0m,
                        // restaurant in appendix is given as external id like "R-1001"; store in RestaurantId temporarily as 0
                        // We'll resolve to actual Restaurant FK on commit when saving to DB
                        Status = "Pending"
                    };

                    // capture restaurant external id in RestaurantId via negative hash? Instead use ExternalId on menuitems and rely on matching in commit
                    var restExt = element.TryGetProperty("restaurantId", out var rip) ? rip.GetString() : null;
                    // temporarily store restaurant external id in ExternalIdOwner (not present) -> instead embed into Title? better: use ExternalId for item id and store owner ext in ImagePath (hack) -> Since models don't have a field for restaurant external id, we'll set ImagePath to hold restaurant external id temporarily
                    menuItem.ImagePath = restExt; 

                    items.Add(menuItem);
                }
            }

            return items;
        }
    }
}
