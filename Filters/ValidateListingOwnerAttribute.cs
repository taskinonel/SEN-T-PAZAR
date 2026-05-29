using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ValidateListingOwnerAttribute : Attribute, IAsyncActionFilter
{
    private readonly bool _allowAdmin;

    public ValidateListingOwnerAttribute(bool allowAdmin = true)
    {
        _allowAdmin = allowAdmin;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        int? id = null;
        if (context.ActionArguments.TryGetValue("id", out var idObj) && idObj is int iid)
        {
            id = iid;
        }
        else
        {
            // try model with Id property
            var modelArg = context.ActionArguments.Values.FirstOrDefault(x => x != null && x.GetType().GetProperty("Id") != null);
            if (modelArg != null)
            {
                var prop = modelArg.GetType().GetProperty("Id");
                if (prop != null && prop.GetValue(modelArg) is int mid)
                {
                    id = mid;
                }
            }
        }

        if (!id.HasValue)
        {
            context.Result = new BadRequestResult();
            return;
        }

        var db = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
        if (db == null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        var listing = await db.Listings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
        if (listing == null)
        {
            context.Result = new NotFoundResult();
            return;
        }

        var user = context.HttpContext.User;
        var userId = context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId) && listing.UserId == userId)
        {
            await next();
            return;
        }

        if (_allowAdmin && user.IsInRole("Admin"))
        {
            await next();
            return;
        }

        context.Result = new ForbidResult();
    }
}
