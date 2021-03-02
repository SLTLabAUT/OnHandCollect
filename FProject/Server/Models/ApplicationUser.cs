using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Writepad> Writepads { get; set; }
    }

    public static class IdentityRoleConstants
    {
        public const string User = "User";
        public const string Admin = "Admin";
    }
}
