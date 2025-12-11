using System.Collections.Generic;

namespace EnterpriseHomeAssignment.Models
{
    public interface IItemValidating
    {
        List<string> GetValidators();
        string GetCardPartial();
    }

}
