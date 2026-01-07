using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using DSecureApi.Services;
using System.Security.Claims;

namespace DSecureApi.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _permission;

        public RequirePermissionAttribute(string permission)
        {
            _permission = permission;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Check if the user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var roleService = context.HttpContext.RequestServices.GetService<IRoleBasedAuthService>();
            if (roleService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            // Get user email from claims
            var email = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check if it's a subuser (you can implement logic to determine this)
            bool isSubuser = await IsSubuserAsync(email, context.HttpContext);

            // Check permission
            var hasPermission = await roleService.HasPermissionAsync(email, _permission, isSubuser);
            if (!hasPermission)
            {
                context.Result = new ForbidResult();
                return;
            }
        }

        private async Task<bool> IsSubuserAsync(string email, HttpContext httpContext)
        {
            // Add logic to determine if the email belongs to a subuser
            // This could be done by checking the database or token claims
            var dbContext = httpContext.RequestServices.GetService<ApplicationDbContext>();
            if (dbContext != null)
            {
                return await dbContext.subuser.AnyAsync(s => s.subuser_email == email);
            }
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _roles;

        public RequireRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var roleService = context.HttpContext.RequestServices.GetService<IRoleBasedAuthService>();
            if (roleService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var email = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            bool isSubuser = await IsSubuserAsync(email, context.HttpContext);
            var userRoles = await roleService.GetUserRolesAsync(email, isSubuser);

            if (!_roles.Any(role => userRoles.Contains(role)))
            {
                context.Result = new ForbidResult();
                return;
            }
        }

        private async Task<bool> IsSubuserAsync(string email, HttpContext httpContext)
        {
            var dbContext = httpContext.RequestServices.GetService<ApplicationDbContext>();
            if (dbContext != null)
            {
                return await dbContext.subuser.AnyAsync(s => s.subuser_email == email);
            }
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireHierarchyLevelAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly int _maxLevel; // Lower number = higher privilege

        public RequireHierarchyLevelAttribute(int maxLevel)
        {
            _maxLevel = maxLevel;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var roleService = context.HttpContext.RequestServices.GetService<IRoleBasedAuthService>();
            if (roleService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var email = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            bool isSubuser = await IsSubuserAsync(email, context.HttpContext);
            var userLevel = await roleService.GetUserHierarchyLevelAsync(email, isSubuser);

            if (userLevel > _maxLevel)
            {
                context.Result = new ForbidResult();
                return;
            }
        }

        private async Task<bool> IsSubuserAsync(string email, HttpContext httpContext)
        {
            var dbContext = httpContext.RequestServices.GetService<ApplicationDbContext>();
            if (dbContext != null)
            {
                return await dbContext.subuser.AnyAsync(s => s.subuser_email == email);
            }
            return false;
        }
    }
}