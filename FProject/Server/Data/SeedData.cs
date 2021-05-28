using FProject.Server.Models;
using FProject.Shared;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
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
                await EnsureWordGroup(context);
            }
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

        private static async Task EnsureText(ApplicationDbContext context)
        {
            var count = await context.Text
                .Where(t => t.Type == TextType.Text)
                .CountAsync();
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

            await context.SaveChangesAsync();
        }

        private static async Task EnsureWordGroup(ApplicationDbContext context)
        {
            var count = await context.Text
                .Where(t => t.Type == TextType.WordGroup)
                .CountAsync();
            if (count != 0)
            {
                return;
            }

            var allText = await context.Text
                .Where(t => t.Type == TextType.Text)
                .ToListAsync();
            foreach (var text in allText)
            {
                var rnd = new Random();
                var words = text.Content.Split(" ").OrderBy(w => rnd.Next()).ToList();
                var batches = words.Batch(7, e => e.ToList()).ToList();
                var lastBatch = batches.Last();
                if (lastBatch.Count < 7
                    && batches.Count > 1
                    && lastBatch.Count / (batches.Count - 1) <= 3) // Optimum: 7, Minimum: 4, Maximum: 10
                {
                    var i = batches.Count - 1;
                    while (lastBatch.Any())
                    {
                        batches[i].Add(lastBatch.Last());
                        lastBatch.RemoveAt(lastBatch.Count - 1);
                        i--;
                        if (i < 0)
                        {
                            i = batches.Count - 1;
                        }
                    }
                    batches.RemoveAt(batches.Count - 1);
                }
                foreach (var batch in batches)
                {
                    context.Text.Add(
                        new Text
                        {
                            Rarity = text.Rarity,
                            Type = TextType.WordGroup,
                            WordCount = batch.Count,
                            Content = string.Join(" ", batch)
                        }
                    );
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
