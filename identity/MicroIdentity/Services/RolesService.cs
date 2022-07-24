using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MicroIdentity.Infrastructure;
using MicroIdentity.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MicroIdentity.Services
{
    [Authorize]
    [Authorize(Roles ="Admin")]
    public class RolesService : Protos.Roles.RolesBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly AppDbContext _ctx;
        public RolesService(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, AppDbContext ctx)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _ctx = ctx;
        }

        public override async Task<Protos.UserRolesResponse> RolesList(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            IEnumerable<MicroRole> roles = _ctx.MicroRoles.Where(wr => wr.UserId == user.Id)
                .Include("MicroRoleRoles.Role");

            Protos.UserRolesResponse response = new Protos.UserRolesResponse();
            response.UserId = user.Id;
            response.Roles.AddRange(roles.Select( wr => {
                Protos.Role role = new Protos.Role
                {
                    Id = wr.Id,
                    UserId = wr.UserId,
                    Name = wr.Name
                };

                role.Privileges.AddRange(wr.MicroRoleRoles.Select(p => new Protos.Privilege
                {
                    Id = p.RoleId,
                    Name = p.Role.Name,
                }));

                return role;
            }));
            
            return response;
        }

        public override Task<Protos.PrivilegesResponse> PrivilegesList(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            Protos.PrivilegesResponse response = new Protos.PrivilegesResponse();

            response.Privileges.AddRange(_roleManager.Roles.Select(r=>new Protos.Privilege { Id = r.Id, Name = r.Name }));

            return Task.FromResult(response);
        }

        public override async Task<Protos.UserRolesResponse> AssignUserRoles(Protos.UserRoleRequest request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            TenantUser? userTenant = _ctx.TenantUsers.SingleOrDefault(ut => ut.TenantId == user.Id && ut.UserId == request.UserId);
            if (null == userTenant)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Wrong tenant"));
            }

            IEnumerable<MicroRole> roles = _ctx.MicroRoles.Where(r => r.UserId == user.Id && request.RoleIds.Contains(r.Id));

            if (roles.Count() != request.RoleIds.Count)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Wrong roles"));
            }

            _ctx.TenantUserMicroRoles.RemoveRange(_ctx.TenantUserMicroRoles.Where(utmr => utmr.TenantUserId == userTenant.Id));

            _ctx.TenantUserMicroRoles.AddRange(roles.Select(r=>new TenantUserMicroRole { TenantUser = userTenant, MicroRole = r } ));
            _ctx.SaveChanges();

            return GetUserTenantRoles(user.Id, request.UserId);
        }

        public override async Task<Protos.UserRolesResponse> RemoveUserRoles(Protos.UserRoleRequest request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            TenantUser? tenant = _ctx.TenantUsers.SingleOrDefault(ut => ut.TenantId == user.Id && ut.UserId == request.UserId);
            if (null == tenant)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Wrong tenant"));
            }

            IEnumerable<MicroRole> roles = _ctx.MicroRoles.Where(r => r.UserId == user.Id && request.RoleIds.Contains(r.Id));
            if (roles.Count() != request.RoleIds.Count)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Wrong roles"));
            }

            var userTenantMicroRoles = _ctx.TenantUserMicroRoles.Where(utmr => utmr.TenantUserId == tenant.Id && roles.Any(r=> utmr.MicroRoleId == r.Id));

            _ctx.TenantUserMicroRoles.RemoveRange(userTenantMicroRoles);
            _ctx.SaveChanges();

            return GetUserTenantRoles(user.Id, request.UserId);
        }

        private Protos.UserRolesResponse GetUserTenantRoles(int userId, int tenantId)
        {
            IQueryable<TenantUserMicroRole> tenantRoles = _ctx.TenantUserMicroRoles
                .Where(utmr => utmr.TenantUser.TenantId == userId && utmr.TenantUser.UserId == tenantId); ;

            Protos.UserRolesResponse response = new Protos.UserRolesResponse
            {
                UserId = tenantId
            };

            response.Roles.AddRange(tenantRoles.Select(tr => new Protos.Role
            {
                Id = tr.MicroRole.Id,
                UserId = tr.MicroRole.UserId,
                Name = tr.MicroRole.Name,
            }));

            return response;
        }

        public override async Task<Protos.UsersRolesResponse> GetUsersRoles(Protos.UsersRolesRequest request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            IEnumerable<IGrouping<int, TenantUserMicroRole>> tenantRoles = _ctx.TenantUserMicroRoles
                .Where(utmr => utmr.TenantUser.TenantId == user.Id && request.UserIds.Contains(utmr.TenantUser.UserId))
                .Include(utmr => utmr.MicroRole)
                .Include(utmr => utmr.TenantUser)
                .AsNoTracking()
                .AsEnumerable()
                .GroupBy(utmr => utmr.TenantUser.UserId);

            Protos.UsersRolesResponse response = new Protos.UsersRolesResponse();

            response.UserRoles.AddRange(tenantRoles.Select(tg => {

                Protos.UserRolesResponse r = new Protos.UserRolesResponse
                {
                    UserId = tg.Key
                };

                r.Roles.AddRange(tg.Select(utmr => new Protos.Role
                {
                    Id = utmr.MicroRoleId,
                    UserId = utmr.MicroRole.UserId,
                    Name = utmr.MicroRole.Name
                }));

                return r;
            }));

            return response;
        }

        public override async Task<Protos.UserRolesResponse> GetUserRoles(Protos.UserRolesRequest request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            IList<string> userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin") && request.UserId!=user.Id)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied"));
            }

            IQueryable<TenantUserMicroRole> tenantRoles = _ctx.TenantUserMicroRoles.Where(utmr => utmr.TenantUser.UserId == request.UserId);

            Protos.UserRolesResponse response = new Protos.UserRolesResponse
            {
                UserId = request.UserId
            };

            response.Roles.AddRange(tenantRoles.Select(tr=>new Protos.Role {
                Id = tr.MicroRole.Id,
                UserId = tr.MicroRole.UserId,
                Name = tr.MicroRole.Name,
            }));

            return response;
        }

        public override async Task<Protos.RoleUsersResponse> GetRoleUsers(Protos.RoleRequest request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            IQueryable<int> tenantIds = _ctx.TenantUserMicroRoles
                .Where(utmr => utmr.MicroRoleId == request.RoleId && utmr.TenantUser.TenantId==user.Id)
                .Select(utwr=> utwr.TenantUserId);

            Protos.RoleUsersResponse response = new Protos.RoleUsersResponse();
            response.Users.AddRange(tenantIds);
            return response;
        }


        public override async Task<Protos.Role> RoleCreate(Protos.Role request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            MicroRole microRole = new MicroRole { UserId = user.Id, Name = request.Name, CreatedAt = DateTime.UtcNow };

            var privileges = request.Privileges.Select(p => new MicroRoleRole { MicroRole = microRole, RoleId = p.Id });

            _ctx.MicroRoleRoles.AddRange(privileges);
            _ctx.SaveChanges();

            Protos.Role role = new Protos.Role { 
                Id = microRole.Id,
                UserId = microRole.UserId,
                Name = microRole.Name,
            };

            role.Privileges.AddRange(privileges.Select(p => new Protos.Privilege
            {
                Id = p.RoleId,
                //Name = p.Role.Name,
            }));

            return role;
        }

        public override async Task<Protos.Role> RoleRemove(Protos.RoleRequest request, ServerCallContext context)
        {
            (Protos.Role role, MicroRole microRole) = await GetRole(request, context);

            _ctx.MicroRoles.Remove(microRole);
            _ctx.SaveChanges();

            return role;
        }

        public override async Task<Protos.Role> RoleInfo(Protos.RoleRequest request, ServerCallContext context)
        {
            (Protos.Role role, _ ) = await GetRole(request, context);
            return role;
        }

        public override async Task<Protos.Role> RoleEdit(Protos.Role request, ServerCallContext context)
        {
            (_, MicroRole microRole) = await GetRole(new Protos.RoleRequest { RoleId= request.Id}, context);

            microRole.Name = request.Name;

            var privileges = request.Privileges.Select(p => new MicroRoleRole { MicroRole = microRole, RoleId = p.Id });

            microRole.MicroRoleRoles = privileges.ToList();

            _ctx.SaveChanges();

            return request;
        }

        private async Task<(Protos.Role, MicroRole)> GetRole(Protos.RoleRequest request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            MicroRole? microRole = _ctx.MicroRoles.Include("MicroRoleRoles.Role").SingleOrDefault(wr => wr.UserId == user.Id && wr.Id == request.RoleId);

            if (null == microRole)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Role not found"));
            }

            Protos.Role role = new Protos.Role
            {
                Id = microRole.Id,
                UserId = microRole.UserId,
                Name = microRole.Name,
            };

            role.Privileges.AddRange(microRole.MicroRoleRoles.Select(mrr => new Protos.Privilege
            {
                Id = mrr.RoleId,
                Name = mrr.Role.Name,
            }));

            return (role, microRole);
        }
    }
}
