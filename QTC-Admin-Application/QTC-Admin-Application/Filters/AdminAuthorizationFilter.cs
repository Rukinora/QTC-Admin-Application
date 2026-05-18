using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QTC_Admin_Application.Filters;

public class AdminAuthorizationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var adminQueryValue = context.HttpContext.Request.Query["admin"].ToString();
        var isLoggedInSession = context.HttpContext.Session.GetString("LoggedIn");

#if DEBUG
        // Path 1: Sponsor-compliant ?admin=true auto-login (ONLY COMPILES IN DEBUG MODE)
        if (string.Equals(adminQueryValue, "true", StringComparison.OrdinalIgnoreCase))
        {
            context.HttpContext.Session.SetString("LoggedIn", "true");
            context.HttpContext.Session.SetString("Role", "Admin");
            context.HttpContext.Session.SetString("Username", "ADMIN");
            return;
        }
#endif

        // Path 2: Team's manual login system (session-based)
        if (isLoggedInSession == "true")
        {
            return;
        }

        // Redirect to login page if not authenticated
        context.Result = new RedirectToActionResult("Login", "Home", null);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
