using FProject.Shared.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        public Sex? Sex { get; set; }
        public DateTime? BirthDate { get; set; }

        public ICollection<Writepad> Writepads { get; set; }

        public static explicit operator UserDTO(ApplicationUser model)
        {
            return new UserDTO
            {
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Sex = model.Sex,
                BirthDate = model.BirthDate
            };
        }
    }
}
