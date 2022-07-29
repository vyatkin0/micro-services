using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroIdentity.Models
{
    public class AppUser : IdentityUser<int> 
    {
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName ="nvarchar(300)")]
        public string? FirstName { get; set; }
        [Column(TypeName = "nvarchar(300)")]
        public string? LastName { get; set; }
        [Column(TypeName = "nvarchar(300)")]
        public string? Company { get; set; }

        public ICollection<TenantUser>? TenantUserTenants { get; set; }
        public ICollection<TenantUser>? TenantUserUsers { get; set; }
    }

    public class AppRole : IdentityRole<int>
    {
       public ICollection<MicroRoleRole>? MicroRoleRoles { get; set; }
    }

    /// <summary>
    /// Refresh token
    /// </summary>
    public class UserRefreshToken
    {
        [ForeignKey("User")]
        public int UserId { get; set; }
        public AppUser? User { get; set; }

        [Column(TypeName = "char(36)")]
        public string TokenId { get; set; } = "";
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
    }

    /// <summary>
    /// Data class for tenat users
    /// </summary>
    public class TenantUser
    {
        public int Id { get; set; }

        [ForeignKey("Tenant")]
        public int TenantId { get; set; }
        public AppUser Tenant { get; set; } = null!;

        [ForeignKey("User")]
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public ICollection<TenantUserMicroRole>? TenantUserMicroRoles { get; set; }
    }

    /// <summary>
    /// Data class for service specific roles - Micro roles
    /// </summary>
    public class MicroRole
    {
        [Column(TypeName ="datetime")]
        public DateTime CreatedAt { get; set; }
        public int Id { get; set; }
        public int UserId { get; set; }

        [Column(TypeName = "nvarchar(300)")]
        public string Name { get; set; } = "";

        public ICollection<TenantUserMicroRole> TenantUserMicroRoles { get; set; } = null!;
        public ICollection<MicroRoleRole> MicroRoleRoles { get; set; } = null!;
    }

    /// <summary>
    /// Data class for relations between Micro roles and Identity roles
    /// </summary>
    public class MicroRoleRole
    {
        public int MicroRoleId { get; set; }
        public MicroRole MicroRole { get; set; } = null!;

        public int RoleId { get; set; }
        public AppRole Role{ get; set; } = null!;
    }


    /// <summary>
    /// Data class for Micro roles assigned to tenant users
    /// </summary>
    public class TenantUserMicroRole
    {
        public int TenantUserId { get; set; }
        public TenantUser TenantUser { get; set; } = null!;

        public int MicroRoleId { get; set; }
        public MicroRole MicroRole { get; set; } = null!;
    }
}
