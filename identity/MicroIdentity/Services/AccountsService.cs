using MicroIdentity.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MicroIdentity.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace MicroIdentity.Services
{
    public static class MicroIdentityClaimTypes
    {
        public static readonly string TokenType = "https://github.com/vyatkin0/micro-services/identity/claims/token-type";

        public static readonly string TokenRefresh = "https://github.com/vyatkin0/micro-services/identity/claims/token-refresh";
    }

    public class TokenInfo
    {
        public JwtSecurityToken? token { get; set; }
        public AppUser? tenant { get; set; }
        public bool isAdmin { get; set; }
    }

    public class AccountsService : Protos.Accounts.AccountsBase
    {
        private readonly ILogger<AccountsService> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _ctx;
        public AccountsService(AppDbContext ctx, ILogger<AccountsService> logger, UserManager<AppUser> userManager)
        {
            _ctx = ctx;
            _logger = logger;
            _userManager = userManager;
        }

        public override async Task<Protos.LoginInfo> Login(Protos.LoginRequest request, ServerCallContext context)
        {
            var user = await _userManager.FindByNameAsync(request.Name);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, $"Unable to load user with name {request.Name}"));
            }

            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await CheckPasswordSignInAsync(user, request.Password, true);
            if (result.Succeeded)
            {
                if (_userManager.SupportsUserLockout)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                }

                IList<string> userRoles = await _userManager.GetRolesAsync(user);
                if(!userRoles.Contains("User") && !userRoles.Contains("Admin"))
                {
                    throw new RpcException(new Status(StatusCode.PermissionDenied, "Login is not permitted"));
                }

                _logger.LogInformation($"User {request.Name} logged in");

                TokenInfo? refreshTokenInfo = await GenerateToken(user, false, null);

                Protos.LoginInfo li = new Protos.LoginInfo
                {
                    RefreshToken = new JwtSecurityTokenHandler().WriteToken(refreshTokenInfo?.token)
                };

                TokenInfo? accessTokenInfo = null;

                if (null != refreshTokenInfo?.token)
                {
                    accessTokenInfo =await GenerateToken(user, true, refreshTokenInfo?.token.Id);
                }

                if (null != accessTokenInfo)
                {
                    li.AccessToken = new JwtSecurityTokenHandler().WriteToken(accessTokenInfo.token);
                    li.IsAdmin = accessTokenInfo.isAdmin;

                    if (null != accessTokenInfo.tenant)
                    {
                        li.Tenant = new Protos.TenantInfo
                        {
                            Id = accessTokenInfo.tenant.Id,
                            Name = accessTokenInfo.tenant.UserName,
                            Company = accessTokenInfo.tenant.Company,
                        };
                    }
                }

                return li;
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning($"User with Id {user.Id} account locked out.", user.Id);
            }

            throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
        }

        [Authorize]
        [Authorize(Roles = "User, Admin")]
        public override async Task<Protos.StatusResponse> Logout(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            string? RefreshTokenId = null;

            Claim? tokenTypeClaim = Context.User.Claims.FirstOrDefault(c => c.Type == MicroIdentityClaimTypes.TokenType);
            switch (tokenTypeClaim?.Value)
            {
                case "access":
                    Claim? tokenRefreshIdClaim = Context.User.Claims.FirstOrDefault(c => c.Type == MicroIdentityClaimTypes.TokenRefresh);
                    RefreshTokenId = tokenRefreshIdClaim?.Value;
                    break;
                case "refresh":
                    Claim? tokenIdClaim = Context.User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
                    RefreshTokenId = tokenIdClaim?.Value; 
                    break;
                default:
                    throw new RpcException(new Status(StatusCode.PermissionDenied, "Wrong token"));
            }

            if (null == RefreshTokenId)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Wrong token"));
            }

            _ctx.RemoveRange(_ctx.UserRefreshTokens.Where(t => t.UserId == user.Id && t.TokenId == RefreshTokenId));
            _ctx.SaveChanges();

            return new Protos.StatusResponse { Status = "Success" };
        }

        public override async Task<Protos.LoginInfo> Register(Protos.RegisterRequest request, ServerCallContext context)
        {
            var user = new AppUser {
                UserName = request.Name,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Company = request.Company,
                CreatedAt = DateTime.UtcNow
            };

            IdentityResult result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password");

                var companyUserRoles = _ctx.UserRoles
                    .Join(_ctx.Users, ur => ur.UserId, u => u.Id, (ur, u) => new { ur, u })
                    .Where(ur => ur.u.Company == user.Company)
                    .OrderBy(ur => ur.u.Id);

                //Seeds company admin if it is not exists yet
                //RoleId Admin=1
                string role = !companyUserRoles.Any(ur => ur.ur.RoleId == 1) ? "Admin" : "User";
                result = await _userManager.AddToRoleAsync(user, role);
                if (!result.Succeeded)
                {
                    _logger.LogInformation(Program.GetErrorsFromResult(result));
                }

                return await Login(new Protos.LoginRequest {
                    Name = request.Name,
                    Password = request.Password
                }, context);
            }

            throw new RpcException(new Status(StatusCode.InvalidArgument, Program.GetErrorsFromResult(result)));
        }

        [Authorize]
        [Authorize(Roles="Admin")]
        public override async Task<Protos.ListResponse> List(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            HttpContext Context = context.GetHttpContext();

            var user = await _userManager.GetUserAsync(Context.User);
            if (null == user)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unknown user"));
            }

            IList<string> userRoles = await _userManager.GetRolesAsync(user);

            IQueryable<AppUser> users;
            //if (userRoles.Contains("Admin"))
            //{
            //    users = _userManager.Users.Where(u => _userManager.IsInRoleAsync(u, "Admin").Result
            //        || _userManager.IsInRoleAsync(u, "User").Result);
            //}
            //else
            {
                users = _ctx.TenantUsers.Where(ut => ut.TenantId == user.Id).Select(ut => ut.User);
            }

            Protos.ListResponse response = new Protos.ListResponse();
            response.AppUsers.AddRange(users.ToArray().Select(u => new Protos.AppUser
            {
                Id = u.Id,
                Name = u.UserName,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Company = u.Company
            }));

            return response;
        }

        /// <summary>
        /// Метод генерирует JWT токен для пользователя
        /// </summary>
        /// <param name="user">Пользователь для которого генерируется токен</param>
        /// <param name="isAccessToken">Если true, то генерируется токен доступа, иначе токен обновления</param>
        /// <param name="isLocal">Если true, то генерируется токен доступа используемый сами сервисом Identity</param>
        /// <returns>обект JwtSecurityToken или null в случае ошибки</returns>
        private async Task<TokenInfo?> GenerateToken(AppUser user, bool isAccessToken, string? TokenRefreshId)
        {
            TokenInfo? tokenInfo = await GenerateToken(_userManager, user, isAccessToken, TokenRefreshId);

            if (!isAccessToken && tokenInfo?.token != null)
            {
                _ctx.UserRefreshTokens.Add(new UserRefreshToken
                {
                    User = user,
                    TokenId = tokenInfo.token.Id,
                    ValidFrom = tokenInfo.token.ValidFrom,
                    ValidTo = tokenInfo.token.ValidTo
                });

                _ctx.SaveChanges();

                _logger.LogInformation($"Issued token:\r\n{tokenInfo.token}");
            }

            return tokenInfo;
        }

        /// <summary>
        /// Метод генерирует JWT токен для пользователя
        /// </summary>
        /// <param name="userManager">Объект UserManager</param>
        /// <param name="user">Пользователь для которого генерируется токен</param>
        /// <param name="isAccessToken">Если true, то генерируется токен доступа, иначе токен обновления</param>
        /// <returns>обект JwtSecurityToken или null в случае ошибки</returns>
        public async Task<TokenInfo?> GenerateToken(UserManager<AppUser> userManager,AppUser user, bool isAccessToken, string? TokenRefreshId)
        {
            if (user == null) return null;

            string tokenId = Guid.NewGuid().ToString();
            List<Claim> claims = new List<Claim>
            {
                //Sub claim will be converted to ClaimTypes.NameIdentifier by
                //JwtSecurityTokenHandler.ValidateToken
                //new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, tokenId)
            };

            bool isAdmin = false;

            if (isAccessToken)
            {
                //Adding IdentityApi roles
                HashSet<string> roles = new HashSet<string>();
                IList<string> userRoles = await userManager.GetRolesAsync(user);
                foreach (string role in userRoles)
                {
                    if(!isAdmin)
                    {
                        isAdmin = role == "Admin";
                    }

                    roles.Add(role);
                }

                //Adding IdentityApi roles related to Micro roles
                IQueryable<TenantUserMicroRole> tenantRoles = _ctx.TenantUserMicroRoles
                    .Include("MicroRole.MicroRoleRoles.Role")
                    .Include(tuwr => tuwr.TenantUser)
                    .Where(tuwr => tuwr.TenantUser.UserId == user.Id);

                foreach (TenantUserMicroRole utmr in tenantRoles)
                {
                    foreach (MicroRoleRole mrr in utmr.MicroRole.MicroRoleRoles)
                    {
                        roles.Add($"{utmr.TenantUser.TenantId}/{mrr.Role.Name}");
                    }
                }

                foreach (string role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                if (null != TokenRefreshId)
                {
                    claims.Add(new Claim(MicroIdentityClaimTypes.TokenRefresh, TokenRefreshId));
                }

                claims.Add(new Claim(MicroIdentityClaimTypes.TokenType, "access"));
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

                IList<Claim> userClaims = await userManager.GetClaimsAsync(user);
                foreach (Claim claim in userClaims)
                    claims.Add(claim);
            }
            else
            {
                claims.Add(new Claim(MicroIdentityClaimTypes.TokenType, "refresh"));
            }

            string? tokenKey = Environment.GetEnvironmentVariable("TOKEN_KEY");

            if(string.IsNullOrEmpty(tokenKey))
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Token key is not specified"));
            }

            byte[] keyData = Encoding.UTF8.GetBytes(tokenKey);

            SymmetricSecurityKey key = new SymmetricSecurityKey(keyData);
            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            int validMinutes = isAccessToken ? 30 : 30 * 24 * 60;
            JwtSecurityToken token = new JwtSecurityToken(Environment.GetEnvironmentVariable("TOKEN_ISSUER"),
                Environment.GetEnvironmentVariable("TOKEN_AUDIENCE"),
                claims,
                expires: DateTime.UtcNow.AddMinutes(validMinutes),
                signingCredentials: creds);

            return new TokenInfo { token = token, isAdmin = isAdmin };
        }

        // These method is taken from https://github.com/aspnet/Identity/blob/fcc02103aa10dcdd8759e0463cac2717114f3c1e/src/Identity/SignInManager.cs#L320
        // and modified to avoid dependency from Identity SignInManager
        /// <summary>
        /// Attempts a password sign in for a user.
        /// </summary>
        /// <param name="user">The user to sign in.</param>
        /// <param name="password">The password to attempt to sign in with.</param>
        /// <param name="lockoutOnFailure">Flag indicating if the user account should be locked if the sign in fails.</param>
        /// <returns>The task object representing the asynchronous operation containing the <see name="SignInResult"/>
        /// for the sign-in attempt.</returns>
        /// <returns></returns>
        private async Task<SignInResult> CheckPasswordSignInAsync(AppUser user, string password, bool lockoutOnFailure)
        {
            if (_userManager.Options.SignIn.RequireConfirmedEmail && !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                _logger.LogWarning($"User with Id {user.Id} cannot sign in without a confirmed email", await _userManager.GetUserIdAsync(user));
                return SignInResult.NotAllowed;
            }

            if (_userManager.Options.SignIn.RequireConfirmedPhoneNumber && !(await _userManager.IsPhoneNumberConfirmedAsync(user)))
            {
                _logger.LogWarning($"User with Id {user.Id} cannot sign in without a confirmed phone number", await _userManager.GetUserIdAsync(user));
                return SignInResult.NotAllowed;
            }

            if (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning($"User with Id {user.Id} is currently locked out", await _userManager.GetUserIdAsync(user));
                return SignInResult.LockedOut;
            }

            if (await _userManager.CheckPasswordAsync(user, password))
            {
                return SignInResult.Success;
            }

            _logger.LogWarning($"User with Id {user.Id} failed to provide the correct password", await _userManager.GetUserIdAsync(user));

            if (_userManager.SupportsUserLockout && lockoutOnFailure)
            {
                // If lockout is requested, increment access failed count which might lock out the user
                await _userManager.AccessFailedAsync(user);

                if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning($"User with Id {user.Id} is currently locked out", await _userManager.GetUserIdAsync(user));
                    return SignInResult.LockedOut;
                }
            }

            return SignInResult.Failed;
        }
    }
}
