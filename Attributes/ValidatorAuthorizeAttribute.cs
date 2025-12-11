using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using EnterpriseHomeAssignment.Models;
using Microsoft.AspNetCore.Http;
using System;

namespace EnterpriseHomeAssignment.Attributes
{
    public class ValidatorAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Expect action arguments "restaurantIds" and/or "menuItemIds"
            var user = context.HttpContext.User;
            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new ForbidResult();
                return;
            }

            string email = user.Identity.Name ?? user.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            if (string.IsNullOrEmpty(email))
            {
                context.Result = new ForbidResult();
                return;
            }

            // We can't fully check validators here because we need the actual items to see validators.
            // Instead allow the action to run; the action should perform the actual validation using repository Get and IItemValidating.GetValidators.

            base.OnActionExecuting(context);
        }
    }
}
