using FProject.Server.Models;
using FProject.Shared;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FProject.Server.Data
{
    public class SeedData
    {
        public async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = serviceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

                var adminEmails = config.GetSection("SeedAdmins").Get<List<string>>();
                foreach (var adminEmail in adminEmails)
                {
                    var admin = await userManager.FindByNameAsync(adminEmail);
                    admin = await EnsureAdmin(userManager, admin, adminEmail, config["SeedAdminPw"]);
                    await EnsureAdminRole(serviceProvider, userManager, admin);
                }

                await EnsureText(context);
                await EnsureWordGroup(context);
                await EnsureWordGroupNormalization(context);

                await EnsureUserWordCount(context);
            }
        }

        private async Task EnsureUserWordCount(ApplicationDbContext context)
        {
            var wordsNotCounted = await context.Writepads
                .Where(w => w.Status == WritepadStatus.Accepted && w.Owner.AcceptedWordCount == 0)
                .AnyAsync();

            if (!wordsNotCounted)
            {
                return;
            }

            var text = context.Text
                .Select(t => new { Id = t.Id, WordCount = t.WordCount });
            var writepads = context.Writepads
                .Where(w => w.Status == WritepadStatus.Accepted && w.Owner.AcceptedWordCount == 0)
                .Select(w => new { TextId = w.TextId, OwnerId = w.OwnerId });

            var join = from w in writepads
                       join t in text on w.TextId equals t.Id into gj
                       from wt in gj.DefaultIfEmpty()
                       select new { w.OwnerId, WordCount = wt.WordCount as int? ?? 1 };
            var query = join.GroupBy(r => r.OwnerId, (key, r) => new { UserId = key, AcceptedWordCount = r.Sum(e => e.WordCount) });

            var counts = await query.ToListAsync();

            foreach (var entry in counts)
            {
                var user = new ApplicationUser
                {
                    Id = entry.UserId
                };

                var entity = context.Entry(user);
                if (entity is null)
                {
                    context.Users.Attach(user);
                }
                else
                {
                    user = await context.Users.FindAsync(user.Id);
                }
                user.AcceptedWordCount = entry.AcceptedWordCount;
            }

            await context.SaveChangesAsync();
        }

        private async Task<ApplicationUser> EnsureAdmin(UserManager<ApplicationUser> userManager, ApplicationUser admin, string Email, string UserPw)
        {
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = Email,
                    Email = Email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, UserPw);
            }

            if (admin is null)
            {
                throw new Exception("Admin password is probably not strong enough!");
            }

            return admin;
        }

        private async Task EnsureAdminRole(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager, ApplicationUser admin)
        {
            var adminRole = IdentityRoleConstants.Admin;
            var roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();

            if (roleManager is null)
            {
                throw new Exception("RoleManager is null");
            }

            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(adminRole));

                if (!result.Succeeded)
                {
                    throw new Exception("Couldn't create admin role.");
                }
            }

            if (!await userManager.IsInRoleAsync(admin, adminRole))
            {
                var result = await userManager.AddToRoleAsync(admin, adminRole);

                if (!result.Succeeded)
                {
                    throw new Exception("Couldn't add admin role to admin.");
                }
            }
        }

        private async Task EnsureText(ApplicationDbContext context)
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

        private async Task EnsureWordGroup(ApplicationDbContext context)
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
                            Content = string.Join("\n", batch)
                        }
                    );
                }
            }

            await context.SaveChangesAsync();
        }

        private async Task EnsureWordGroupNormalization(ApplicationDbContext context)
        {
            var first = await context.Text
                .Where(t => t.Type == TextType.WordGroup)
                .FirstOrDefaultAsync();
            if (first is null || !first.Content.Contains(" "))
            {
                return;
            }

            var allWordGroups = await context.Text
                .Where(t => t.Type == TextType.WordGroup)
                .ToListAsync();
            foreach (var wg in allWordGroups)
            {
                wg.Content = wg.Content.Replace(" ", "\n");
            }

            await context.SaveChangesAsync();
        }
    }
}
