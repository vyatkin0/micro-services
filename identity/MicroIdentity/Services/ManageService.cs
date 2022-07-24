using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MicroIdentity.Infrastructure;
using MicroIdentity.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace MicroIdentity.Services
{
    [Authorize]
    [Authorize(Roles ="User, Admin")]
    public class ManageService : Protos.Manage.ManageBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _ctx;
        public ManageService(AppDbContext ctx, UserManager<AppUser> userManager)
        {
            _ctx = ctx;
            _userManager = userManager;
        }

        public override async Task<Protos.AppUser> Info(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            return new Protos.AppUser {
                Id = user.Id,
                Name = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Company = user.Company
            };
        }

        public override async Task<Protos.TenantsResponse> GetTenants(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            var tenants = _ctx.TenantUsers.Where(ut => ut.UserId == user.Id)
                .Distinct()
                .Select(ut => new Protos.Tenant
                {
                    Id = ut.TenantId,
                    Name = _ctx.Set<AppUser>().First(u => u.Id == ut.TenantId).UserName
                });

            Protos.TenantsResponse response = new Protos.TenantsResponse();

            response.Tenants.AddRange(tenants);
            return response;
        }

        public override async Task<Protos.AppUser> Update(Protos.AppUser request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            if (user.Id != request.Id)
            {
                IList<string> userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("Admin"))
                {
                    throw new RpcException(new Status(StatusCode.PermissionDenied, $"Unable to update user with Id {request.Id}"));
                }
            }

            bool updated = false;
            if (null != request.Email)
            {
                string? value = request.Email.Trim();
                if (value.Length < 1)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Email must not be empty"));
                }

                if (user.Email != value)
                {
                    var setEmailResult = await _userManager.SetEmailAsync(user, value);
                    if (!setEmailResult.Succeeded)
                    {
                        throw new RpcException(new Status(StatusCode.InvalidArgument, Program.GetErrorsFromResult(setEmailResult)));
                    }

                    user.Email = value;
                }
            }

            if (null != request.Name)
            {
                string? value = request.Name.Trim();
                if (value.Length < 1)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Name must not be empty"));
                }

                if (user.UserName != value)
                {
                    user.UserName = value;
                    updated = true;
                }
            }

            if (null != request.FirstName)
            {
                string? value = request.FirstName.Trim();
                if (value.Length < 1)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "First name must not be empty"));
                }

                if (user.FirstName != value)
                {
                    user.FirstName = value;
                    updated = true;
                }
            }

            if (null != request.LastName)
            {
                string? value = request.LastName.Trim();
                if (value.Length < 1)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Last name must not be empty"));
                }

                if (user.LastName != value)
                {
                    user.LastName = value;
                    updated = true;
                }
            }

            if (null != request.Company)
            {
                string? value = request.Company.Trim();
                if (value.Length < 1)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Company must not be empty"));
                }
                
                if(user.Company != value)
                {
                    user.Company = value;
                    updated = true;
                }
            }

            if (updated)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new RpcException(new Status(StatusCode.Internal, $"Unexpected error occurred while updating user with Id '{user.Id}'."));
                }
            }

            return await Info(new Google.Protobuf.WellKnownTypes.Empty(), context);
        }

        public override async Task<Protos.StatusResponse> ChangePassword(Protos.ChangePasswordRequest request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                string error = Program.GetErrorsFromResult(changePasswordResult);

                throw new RpcException(new Status(StatusCode.Internal, $"Unable to change password. {error}"));
            }

            return new Protos.StatusResponse
            {
                Status = "Success",
            };
        }
    }
}
