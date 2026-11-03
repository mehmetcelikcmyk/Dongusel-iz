using DonguselIz.Web.Services;

namespace DonguselIz.Web.Infrastructure.Middleware
{
    public class RememberMeMiddleware
    {
        private readonly RequestDelegate _next;

        public RememberMeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService, IAuthService authService)
        {
            var sessionEmail = context.Session.GetString("CurrentUserEmail");

            if (string.IsNullOrEmpty(sessionEmail))
            {
                if (context.Request.Cookies.TryGetValue("DonguselIz_AuthToken", out var token) && !string.IsNullOrWhiteSpace(token))
                {
                    var validatedEmail = tokenService.ValidateRememberToken(token);
                    if (!string.IsNullOrEmpty(validatedEmail))
                    {
                        var employee = await authService.GetEmployeeByEmailAsync(validatedEmail);
                        if (employee != null && employee.IsActive)
                        {
                            context.Session.SetString("CurrentUserEmail", employee.Email);
                            context.Session.SetString("CurrentUserRole", employee.Role.ToString());
                            context.Session.SetString("CurrentCompany", employee.Company?.Name ?? "");
                            context.Session.SetString("CurrentUserName", employee.FullName);
                            context.Items["AuthenticatedEmployee"] = employee;
                        }
                    }
                }
            }

            await _next(context);
        }
    }

    public static class RememberMeMiddlewareExtensions
    {
        public static IApplicationBuilder UseRememberMe(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RememberMeMiddleware>();
        }
    }
}
