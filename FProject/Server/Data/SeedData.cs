using FProject.Server.Models;
using FProject.Shared;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FProject.Server.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = serviceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();

                var adminID = await EnsureUser(serviceProvider, config["SeedAdminPw"], "sssafais@hotmail.com");
                await EnsureRole(serviceProvider, adminID, IdentityRoleConstants.Admin);

                await EnsureText(context);

                //SeedDB(context, adminID);
                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureText(ApplicationDbContext context)
        {
            var count = await context.Text.CountAsync();
            if (count != 0)
            {
                return;
            }
            string line;
            float maxRarity = 1f;
            var splitter = new Regex(@"^(\d+\.\d+)\t(.+)$", RegexOptions.Compiled);
            var wordCounter = new Regex(@"(?: |\\n)+", RegexOptions.Compiled);
            var file = new StreamReader(@"Data/HandWritingPhrases.txt");
            while ((line = await file.ReadLineAsync()) is not null)
            {
                var match = splitter.Match(line);
                var text = new Text
                {
                    Rarity = float.Parse(match.Groups[1].Value) / maxRarity,
                    Content = match.Groups[2].Value
                };
                text.WordCount = wordCounter.Matches(text.Content).Count + 1;

                if (maxRarity == 1f)
                {
                    maxRarity = text.Rarity;
                    text.Rarity = 1f;
                }

                context.Text.Add(text);
            }
            file.Close();
        }

        private static async Task<string> EnsureUser(IServiceProvider serviceProvider, string UserPw, string Email)
        {
            var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByNameAsync(Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = Email,
                    Email = Email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, UserPw);
            }

            if (user is null)
            {
                throw new Exception("The password is probably not strong enough!");
            }

            return user.Id;
        }

        private static async Task<IdentityResult> EnsureRole(IServiceProvider serviceProvider,
                                                                      string uid, string role)
        {
            IdentityResult IR = null;
            var roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();

            if (roleManager is null)
            {
                throw new Exception("roleManager null");
            }

            if (!await roleManager.RoleExistsAsync(role))
            {
                IR = await roleManager.CreateAsync(new IdentityRole(role));
            }

            var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByIdAsync(uid);

            if (user is null)
            {
                throw new Exception("The testUserPw password was probably not strong enough!");
            }

            IR = await userManager.AddToRoleAsync(user, role);

            return IR;
        }
    }
}
