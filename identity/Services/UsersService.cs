using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MicroIdentity.Infrastructure;
using MicroIdentity.Models;
using System.Threading.Tasks;
using System.Linq;

namespace MicroIdentity.Services
{
    [Authorize]
    [Authorize(Roles ="Admin")]
    public class UsersService : Protos.Users.UsersBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _ctx;
        public UsersService(UserManager<AppUser> userManager, AppDbContext ctx)
        {
            _userManager = userManager;
            _ctx = ctx;
        }

        public override async Task<Protos.StatusResponse> AttachUser(Protos.AppUserId request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            var tenantUser = await _userManager.FindByIdAsync(request.Id.ToString());
            if (null == tenantUser)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Unable to find user"));
            }

            if(_ctx.TenantUsers.Any(ut=>ut.UserId== tenantUser.Id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "User already attached"));
            }

            _ctx.TenantUsers.Add(new TenantUser { Tenant = user, User = tenantUser });

            _ctx.SaveChanges();

            return new Protos.StatusResponse
            {
                Status = "Success"
            };
        }

        public override async Task<Protos.StatusResponse> DetachUser(Protos.AppUserId request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            TenantUser? tenantUser = _ctx.TenantUsers.SingleOrDefault(ut=>ut.TenantId==user.Id && ut.UserId==request.Id);

            if (null != tenantUser)
            {
                IQueryable<TenantUserMicroRole> roles = _ctx.TenantUserMicroRoles.Where(r => r.TenantUserId == tenantUser.Id);

                _ctx.TenantUserMicroRoles.RemoveRange(roles);
                _ctx.TenantUsers.Remove(tenantUser);

                _ctx.SaveChanges();
            }

            return new Protos.StatusResponse
            {
                Status = "Success"
            };
        }

        public override async Task<Protos.AppUser> FindUserByName(Protos.FindUserByNameRequest request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            var userTenant = await _userManager.FindByNameAsync(request.Name);
            if (null == userTenant)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "User not found"));
            }

            if (user.Id == userTenant.Id)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Unable to add yourself"));
            }

            return new Protos.AppUser {
                Id = userTenant.Id,
                Name = userTenant.UserName,
                Email = userTenant.Email,
                FirstName = userTenant.FirstName,
                LastName = userTenant.LastName,
                Company = userTenant.Company
            };
        }
    }
}
