using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EnterpriseHomeAssignment.Filters
{
    public class ApprovalAuthorizationFilter : IAsyncActionFilter
    {
        private readonly IItemsRepository _repo;

        public ApprovalAuthorizationFilter(
            [FromKeyedServices("Db")] IItemsRepository repo)
        {
            _repo = repo;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            if (!user.Identity!.IsAuthenticated)
            {
                context.Result = new ForbidResult();
                return;
            }

            var email = user.FindFirstValue(ClaimTypes.Email)
                        ?? user.Identity!.Name;

            var items = await _repo.GetAllAsync();

            var allowed = items.Any(i => i.GetValidators().Contains(email));

            if (!allowed)
            {
                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}
