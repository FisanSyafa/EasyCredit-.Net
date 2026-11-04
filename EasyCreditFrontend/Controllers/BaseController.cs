using EasyCreditFrontend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EasyCreditFrontend.Controllers;

// Base untuk semua halaman yang butuh login.
// Token JWT disimpan di session server-side setelah login.
public abstract class BaseController : Controller
{
    protected string? Token => HttpContext.Session.GetString(SessionKeys.Token);
    protected string Role => HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
    protected string UserName => HttpContext.Session.GetString(SessionKeys.Name) ?? string.Empty;
    protected bool IsAdmin => string.Equals(Role, "ADMIN", StringComparison.OrdinalIgnoreCase);

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Halaman error harus tetap bisa diakses tanpa login.
        if (string.Equals(
                context.ActionDescriptor.RouteValues["action"],
                "Error",
                StringComparison.OrdinalIgnoreCase))
        {
            base.OnActionExecuting(context);
            return;
        }

        if (string.IsNullOrEmpty(Token))
        {
            context.Result = RedirectToAction("Login", "Auth");
            return;
        }

        ViewBag.UserName = UserName;
        ViewBag.Role = Role;
        ViewBag.IsAdmin = IsAdmin;

        base.OnActionExecuting(context);
    }
}