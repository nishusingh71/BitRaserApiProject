using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Dynamic Route Discovery Service - Automatically discovers and manages API routes
    /// This service eliminates hardcoded routes by using reflection to discover controller endpoints
    /// </summary>
    public class DynamicRouteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DynamicRouteService> _logger;

        public DynamicRouteService(ApplicationDbContext context, ILogger<DynamicRouteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Discover and register all controller routes dynamically
        /// </summary>
        public async Task<RouteDiscoveryResult> DiscoverAndSeedRoutesAsync()
        {
            var result = new RouteDiscoveryResult();

            try
            {
                // Get all controller types
                var controllerTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract)
                    .ToList();

                result.ControllersProcessed = controllerTypes.Count;

                foreach (var controllerType in controllerTypes)
                {
                    await ProcessControllerAsync(controllerType, result);
                }

                result.RoutesDiscovered = result.DiscoveredRoutes.Count;

                // Save discovered routes to database
                if (result.DiscoveredRoutes.Any())
                {
                    await SaveRoutesToDatabaseAsync(result.DiscoveredRoutes);
                }

                result.Success = true;
                result.Message = $"Successfully discovered {result.RoutesDiscovered} routes from {result.ControllersProcessed} controllers";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error during route discovery: {ex.Message}";
                _logger.LogError(ex, "Error during dynamic route discovery");
            }

            return result;
        }

        /// <summary>
        /// Process a single controller and extract its routes
        /// </summary>
        private async Task ProcessControllerAsync(Type controllerType, RouteDiscoveryResult result)
        {
            try
            {
                var controllerName = controllerType.Name.Replace("Controller", "");
                var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
                var baseRoute = routeAttribute?.Template?.Replace("[controller]", controllerName) ?? $"api/{controllerName}";

                // Get all action methods
                var actionMethods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == controllerType)
                    .ToList();

                foreach (var method in actionMethods)
                {
                    await ProcessActionMethodAsync(method, baseRoute, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing controller {Controller}", controllerType.Name);
            }
        }

        /// <summary>
        /// Process a single action method and extract route information
        /// </summary>
        private async Task ProcessActionMethodAsync(MethodInfo method, string baseRoute, RouteDiscoveryResult result)
        {
            try
            {
                // Get HTTP method attributes
                var httpMethods = GetHttpMethodsFromAttributes(method);
                var routeTemplate = GetRouteTemplate(method, baseRoute);
                var requiredPermissions = GetRequiredPermissions(method);
                var isAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() != null;

                foreach (var httpMethod in httpMethods)
                {
                    var route = new RouteInfo
                    {
                        RoutePath = routeTemplate,
                        HttpMethod = httpMethod,
                        Description = $"{method.Name} in {method.DeclaringType?.Name}",
                        ControllerName = method.DeclaringType?.Name?.Replace("Controller", "") ?? "Unknown",
                        ActionName = method.Name,
                        RequiredPermissions = requiredPermissions,
                        IsAnonymous = isAnonymous,
                        CreatedAt = DateTime.UtcNow
                    };

                    result.DiscoveredRoutes.Add(route);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing action method {Method}", method.Name);
            }
        }

        /// <summary>
        /// Extract HTTP methods from method attributes
        /// </summary>
        private List<string> GetHttpMethodsFromAttributes(MethodInfo method)
        {
            var httpMethods = new List<string>();

            if (method.GetCustomAttribute<HttpGetAttribute>() != null) httpMethods.Add("GET");
            if (method.GetCustomAttribute<HttpPostAttribute>() != null) httpMethods.Add("POST");
            if (method.GetCustomAttribute<HttpPutAttribute>() != null) httpMethods.Add("PUT");
            if (method.GetCustomAttribute<HttpDeleteAttribute>() != null) httpMethods.Add("DELETE");
            if (method.GetCustomAttribute<HttpPatchAttribute>() != null) httpMethods.Add("PATCH");
            if (method.GetCustomAttribute<HttpOptionsAttribute>() != null) httpMethods.Add("OPTIONS");
            if (method.GetCustomAttribute<HttpHeadAttribute>() != null) httpMethods.Add("HEAD");

            // If no specific HTTP method attribute, assume GET
            if (!httpMethods.Any())
            {
                httpMethods.Add("GET");
            }

            return httpMethods;
        }

        /// <summary>
        /// Get route template for the method
        /// </summary>
        private string GetRouteTemplate(MethodInfo method, string baseRoute)
        {
            // Check for Route attribute on method
            var routeAttr = method.GetCustomAttribute<RouteAttribute>();
            if (routeAttr != null && !string.IsNullOrEmpty(routeAttr.Template))
            {
                return routeAttr.Template.StartsWith("/") ? routeAttr.Template : $"/{routeAttr.Template}";
            }

            // Check for HTTP method attributes with templates
            var httpGetAttr = method.GetCustomAttribute<HttpGetAttribute>();
            if (httpGetAttr?.Template != null)
            {
                return $"/{baseRoute}/{httpGetAttr.Template}".Replace("//", "/");
            }

            var httpPostAttr = method.GetCustomAttribute<HttpPostAttribute>();
            if (httpPostAttr?.Template != null)
            {
                return $"/{baseRoute}/{httpPostAttr.Template}".Replace("//", "/");
            }

            var httpPutAttr = method.GetCustomAttribute<HttpPutAttribute>();
            if (httpPutAttr?.Template != null)
            {
                return $"/{baseRoute}/{httpPutAttr.Template}".Replace("//", "/");
            }

            var httpDeleteAttr = method.GetCustomAttribute<HttpDeleteAttribute>();
            if (httpDeleteAttr?.Template != null)
            {
                return $"/{baseRoute}/{httpDeleteAttr.Template}".Replace("//", "/");
            }

            // Default route
            return $"/{baseRoute}/{method.Name}".Replace("//", "/").ToLower();
        }

        /// <summary>
        /// Extract required permissions from method attributes
        /// </summary>
        private List<string> GetRequiredPermissions(MethodInfo method)
        {
            var permissions = new List<string>();

            // Check for custom permission attributes (if you have them)
            var requirePermissionAttrs = method.GetCustomAttributes()
                .Where(attr => attr.GetType().Name.Contains("RequirePermission") || attr.GetType().Name.Contains("Permission"))
                .ToList();

            foreach (var attr in requirePermissionAttrs)
            {
                // Use reflection to get permission name from the attribute
                var permissionProperty = attr.GetType().GetProperty("Permission") ?? 
                                       attr.GetType().GetProperty("PermissionName") ??
                                       attr.GetType().GetProperty("Name");
                
                if (permissionProperty != null)
                {
                    var permissionValue = permissionProperty.GetValue(attr)?.ToString();
                    if (!string.IsNullOrEmpty(permissionValue))
                    {
                        permissions.Add(permissionValue);
                    }
                }
            }

            // Check for Authorize attribute with roles
            var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>() ?? 
                              method.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>();
            
            if (authorizeAttr?.Roles != null)
            {
                // Convert roles to permissions (this is a mapping you can customize)
                var roles = authorizeAttr.Roles.Split(',').Select(r => r.Trim());
                foreach (var role in roles)
                {
                    permissions.Add(MapRoleToPermission(role));
                }
            }

            // If no specific permissions found, check if it requires authentication
            var requiresAuth = method.GetCustomAttribute<AuthorizeAttribute>() != null ||
                             method.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>() != null;
            
            if (requiresAuth && !permissions.Any())
            {
                permissions.Add("ViewOnly"); // Default permission for authenticated endpoints
            }

            return permissions.Distinct().ToList();
        }

        /// <summary>
        /// Map roles to permissions (customize this based on your business logic)
        /// </summary>
        private string MapRoleToPermission(string role)
        {
            return role switch
            {
                "SuperAdmin" => "FullAccess",
                "Admin" => "UserManagement",
                "Manager" => "ReportAccess",
                "Support" => "ViewOnly",
                _ => "ViewOnly"
            };
        }

        /// <summary>
        /// Save discovered routes to database
        /// </summary>
        private async Task SaveRoutesToDatabaseAsync(List<RouteInfo> routes)
        {
            try
            {
                foreach (var routeInfo in routes)
                {
                    // Check if route already exists
                    var existingRoute = await _context.Routes
                        .FirstOrDefaultAsync(r => r.RoutePath == routeInfo.RoutePath && r.HttpMethod == routeInfo.HttpMethod);

                    if (existingRoute == null)
                    {
                        // Create new route
                        var newRoute = new Models.Route
                        {
                            RoutePath = routeInfo.RoutePath,
                            HttpMethod = routeInfo.HttpMethod,
                            Description = routeInfo.Description,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Routes.Add(newRoute);
                        await _context.SaveChangesAsync();

                        // Link to permissions
                        await LinkRouteToPermissionsAsync(newRoute.RouteId, routeInfo.RequiredPermissions);
                    }
                    else
                    {
                        // Update existing route description if needed
                        existingRoute.Description = routeInfo.Description;
                        _context.Routes.Update(existingRoute);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving routes to database");
                throw;
            }
        }

        /// <summary>
        /// Link route to required permissions
        /// </summary>
        private async Task LinkRouteToPermissionsAsync(int routeId, List<string> permissionNames)
        {
            try
            {
                foreach (var permissionName in permissionNames)
                {
                    var permission = await _context.Permissions
                        .FirstOrDefaultAsync(p => p.PermissionName == permissionName);

                    if (permission != null)
                    {
                        // Check if link already exists
                        var existingLink = await _context.PermissionRoutes
                            .FirstOrDefaultAsync(pr => pr.RouteId == routeId && pr.PermissionId == permission.PermissionId);

                        if (existingLink == null)
                        {
                            _context.PermissionRoutes.Add(new PermissionRoute
                            {
                                RouteId = routeId,
                                PermissionId = permission.PermissionId
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking route {RouteId} to permissions", routeId);
            }
        }

        /// <summary>
        /// Get all discovered routes with their permissions
        /// </summary>
        public async Task<List<RouteWithPermissions>> GetAllRoutesWithPermissionsAsync()
        {
            try
            {
                return await _context.Routes
                    .Include(r => r.PermissionRoutes)
                    .ThenInclude(pr => pr.Permission)
                    .Select(r => new RouteWithPermissions
                    {
                        RouteId = r.RouteId,
                        RoutePath = r.RoutePath,
                        HttpMethod = r.HttpMethod,
                        Description = r.Description,
                        Permissions = r.PermissionRoutes.Select(pr => pr.Permission.PermissionName).ToList()
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting routes with permissions");
                return new List<RouteWithPermissions>();
            }
        }

        /// <summary>
        /// Clean up orphaned routes that no longer exist in controllers
        /// </summary>
        public async Task<CleanupResult> CleanupOrphanedRoutesAsync()
        {
            var result = new CleanupResult();

            try
            {
                // Get current routes from database
                var dbRoutes = await _context.Routes.ToListAsync();
                
                // Discover current routes from controllers
                var discoveryResult = await DiscoverAndSeedRoutesAsync();
                var currentRoutes = discoveryResult.DiscoveredRoutes;

                // Find orphaned routes
                var orphanedRoutes = new List<Models.Route>();
                
                foreach (var dbRoute in dbRoutes)
                {
                    var stillExists = currentRoutes.Any(cr => 
                        cr.RoutePath.Equals(dbRoute.RoutePath, StringComparison.OrdinalIgnoreCase) &&
                        cr.HttpMethod.Equals(dbRoute.HttpMethod, StringComparison.OrdinalIgnoreCase));

                    if (!stillExists)
                    {
                        orphanedRoutes.Add(dbRoute);
                    }
                }

                // Remove orphaned routes
                if (orphanedRoutes.Any())
                {
                    _context.Routes.RemoveRange(orphanedRoutes);
                    await _context.SaveChangesAsync();
                }

                result.Success = true;
                result.OrphanedRoutesRemoved = orphanedRoutes.Count;
                result.Message = $"Removed {orphanedRoutes.Count} orphaned routes";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error during cleanup: {ex.Message}";
                _logger.LogError(ex, "Error during orphaned route cleanup");
            }

            return result;
        }
    }

    #region Result Classes

    public class RouteInfo
    {
        public string RoutePath { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ControllerName { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public List<string> RequiredPermissions { get; set; } = new List<string>();
        public bool IsAnonymous { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RouteDiscoveryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<RouteInfo> DiscoveredRoutes { get; set; } = new List<RouteInfo>();
        public int ControllersProcessed { get; set; }
        public int RoutesDiscovered { get; set; }
    }

    public class RouteWithPermissions
    {
        public int RouteId { get; set; }
        public string RoutePath { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class CleanupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int OrphanedRoutesRemoved { get; set; }
    }

    #endregion
}