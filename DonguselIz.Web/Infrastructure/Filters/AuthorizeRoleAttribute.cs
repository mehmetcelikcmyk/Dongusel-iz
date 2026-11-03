using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DonguselIz.Web.Models.Enums;
using DonguselIz.Web.Services;

namespace DonguselIz.Web.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRoleAttribute : TypeFilterAttribute
    {
        public AuthorizeRoleAttribute(params UserRole[] allowedRoles) : base(typeof(AuthorizeRoleFilter))
        {
            Arguments = new object[] { allowedRoles };
        }
    }

    public class AuthorizeRoleFilter : IAsyncActionFilter
    {
        private readonly UserRole[] _allowedRoles;
        private readonly IAuthService _authService;

        public AuthorizeRoleFilter(UserRole[] allowedRoles, IAuthService authService)
        {
            _allowedRoles = allowedRoles;
            _authService = authService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            
            var sessionUserEmail = httpContext.Session.GetString("CurrentUserEmail");

            if (string.IsNullOrEmpty(sessionUserEmail))
            {
                if (httpContext.Request.Cookies.TryGetValue("DonguselIz_AuthToken", out var rememberEmail) && !string.IsNullOrEmpty(rememberEmail))
                {
                    sessionUserEmail = rememberEmail;
                    var emp = await _authService.GetEmployeeByEmailAsync(rememberEmail);
                    if (emp != null)
                    {
                        httpContext.Session.SetString("CurrentUserEmail", emp.Email);
                        httpContext.Session.SetString("CurrentUserRole", emp.Role.ToString());
                        httpContext.Session.SetString("CurrentCompany", emp.Company?.Name ?? "");
                        httpContext.Session.SetString("CurrentUserName", emp.FullName);
                    }
                }
            }

            if (string.IsNullOrEmpty(sessionUserEmail))
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = httpContext.Request.Path });
                return;
            }

            var employee = await _authService.GetEmployeeByEmailAsync(sessionUserEmail);
            if (employee == null || !_allowedRoles.Contains(employee.Role))
            {
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml"
                };
                return;
            }

            httpContext.Items["AuthenticatedEmployee"] = employee;
            await next();
        }
    }
}
