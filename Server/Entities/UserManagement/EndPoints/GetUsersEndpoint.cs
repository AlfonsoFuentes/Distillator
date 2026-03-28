using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Entities.UserManagement;
using Server.Services;
using Shared.Identity;
using Shared.Results;

namespace Server.Entities.UserManagement.EndPoints
{
    public class GetUsersEndpoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/GetUsersRequest", async (
                GetUsersRequest request,
                UserManager<ApplicationUser> userManager) =>
            {
                var users = await userManager.Users.ToListAsync();
                var userList = new List<UserDto>();

                foreach (var user in users)
                {
                    var roles = await userManager.GetRolesAsync(user);

                    // 🛡️ REGLA VITAL: Si es administrador, lo ignoramos y pasamos al siguiente
                    if (roles.Contains("Administrator"))
                    {
                        continue;
                    }

                    userList.Add(new UserDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email ?? string.Empty,
                        IsActive = user.IsActive,
                        Roles = roles.ToList()
                    });
                }

                return Result<List<UserDto>>.Success(userList);
            })
            .RequireAuthorization(policy => policy.RequireRole("Developer", "Administrator"))
            .WithTags("Identity");
        }
    }
}
