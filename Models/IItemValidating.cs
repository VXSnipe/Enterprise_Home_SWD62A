using System.Collections.Generic;

namespace EnterpriseHomeAssignment.Models
{
    public interface IItemValidating
    {
        // Used for approval step – who can approve this item
        List<string> GetValidators();

        // Name of the partial view for this card/row
        string GetCardPartial();
    }
}
