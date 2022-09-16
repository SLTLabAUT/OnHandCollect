using FProject.Server.Models;
using FProject.Shared;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        protected ILogger<SeedData> Logger { get; set; }

        public async Task Initialize(IServiceProvider serviceProvider)
        {
            Logger = serviceProvider.GetRequiredService<ILogger<SeedData>>();
            Logger.LogInformation("SeedData started...");

            using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

            var adminEmails = config.GetSection("SeedAdmins").Get<List<string>>();
            foreach (var adminEmail in adminEmails)
            {
                Logger.LogInformation("Ensuring admins' roles...");
                var admin = await userManager.FindByNameAsync(adminEmail);
                admin = await EnsureAdmin(userManager, admin, adminEmail, config["SeedAdminPw"]);
                await EnsureAdminRole(serviceProvider, userManager, admin);
                Logger.LogInformation("Seeding done.");
            }

            await EnsureText(context);
            await EnsureWordGroup(context);
            await EnsureWordGroupNormalization(context);
            await EnsureWordGroupX(context, TextType.WordGroup2, "Data/len2_40k_scored.txt", 2);
            await EnsureWordGroupX(context, TextType.WordGroup3, "Data/len3_40k_scored.txt", 3);
            await EnsureNumbers(context);

            await EnsureUserWordCount(context);

            Logger.LogInformation("SeedData ended.");
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
            Logger.LogInformation("Seeding UserWordCount data...");

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

            Logger.LogInformation("Seeding done.");
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
            if (count == 0)
            {
                Logger.LogInformation("Seeding Text data...");
                string line;
                float maxRarity = 1f;
                var splitter = new Regex(@"^(\d+\.\d+)\t(.+)$", RegexOptions.Compiled);
                var wordCounter = new Regex(@" +", RegexOptions.Compiled);
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

                Logger.LogInformation("Seeding done.");
            }

            // w/number text
            count = await context.Text
                .Where(t => t.Type == TextType.Text && t.Content == "رنگ پوستش کرم بود و موها و لب‌هایش مثل یک کوروت مدل ۱۹۶۷ که همین الان از خط تولید در آمده باشد سرخ سرخ بود.")
                .CountAsync();
            if (count == 0)
            {
                Logger.LogInformation("Seeding Text/w/Number data...");
                string line;
                float maxRarity = 1f;
                var splitter = new Regex(@"^(\d+\.\d+)\t(.+)$", RegexOptions.Compiled);
                var wordCounter = new Regex(@" +", RegexOptions.Compiled);
                var file = new StreamReader(@"Data/has_num_40k_scored.txt");
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

                Logger.LogInformation("Seeding done.");
            }
        }

        private async Task EnsureNumbers(ApplicationDbContext context)
        {
            var count = await context.Text
                .Where(t => t.Type == TextType.NumberGroup)
                .CountAsync();
            if (count != 0)
            {
                return;
            }
            Logger.LogInformation("Seeding NumberGroup data...");

            string line;
            int batchSize = 14;
            int numberCount = 0;
            int wordCount = 0;
            string content = string.Empty;
            var file = new StreamReader("Data/random_numbers.txt");
            while ((line = await file.ReadLineAsync()) is not null)
            {
                numberCount++;
                wordCount += line.Contains(" ") ? 2 : 1;
                content += line + '\n';

                if (numberCount == batchSize)
                {
                    var text = new Text
                    {
                        Type = TextType.NumberGroup,
                        Rarity = 0.5f,
                        Content = content.TrimEnd(),
                        WordCount = wordCount
                    };

                    context.Text.Add(text);

                    numberCount = 0;
                    wordCount = 0;
                    content = string.Empty;
                }
            }
            file.Close();

            await context.SaveChangesAsync();
            Logger.LogInformation("Seeding done.");
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
            Logger.LogInformation("Seeding WordGroup data...");

            var allText = await context.Text
                .Where(t => t.Type == TextType.Text)
                .ToListAsync();
            foreach (var text in allText)
            {
                var words = text.Content.Split(" ").ToList();
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

            Logger.LogInformation("Seeding done.");
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
            Logger.LogInformation("Ensuring WordGroup data normalization...");

            var allWordGroups = await context.Text
                .Where(t => t.Type == TextType.WordGroup)
                .ToListAsync();
            foreach (var wg in allWordGroups)
            {
                wg.Content = wg.Content.Replace(" ", "\n");
            }

            await context.SaveChangesAsync();

            Logger.LogInformation("Seeding done.");
        }

        private async Task EnsureWordGroupX(ApplicationDbContext context, TextType type, string fileName, int wordCountPerGroup)
        {
            var count = await context.Text
                .Where(t => t.Type == type)
                .CountAsync();
            if (count != 0)
            {
                return;
            }
            Logger.LogInformation($"Seeding {type} data...");

            string line;
            float maxRarity = 1f;
            var splitter = new Regex(@"^(\d+\.\d+)\t(.+)$", RegexOptions.Compiled);
            var wordCounter = new Regex(@"(?: |\n)+", RegexOptions.Compiled);
            // create groupExtractor
            // language=regex
            var groupExtractorPattern = @"([^ ]+)(?: +)?";
            for (int i = 0; i < wordCountPerGroup - 1; i++)
            {
                // language=regex
                groupExtractorPattern = @"([^ ]+) +" + groupExtractorPattern;
            }
            var groupExtractor = new Regex(groupExtractorPattern, RegexOptions.Compiled);
            // ...
            var file = new StreamReader(fileName);
            while ((line = await file.ReadLineAsync()) is not null)
            {
                var match = splitter.Match(line);

                var rarity = float.Parse(match.Groups[1].Value) / maxRarity;
                if (maxRarity == 1f)
                {
                    maxRarity = rarity;
                    rarity = 1f;
                }

                var content = string.Empty;
                var groups = groupExtractor.Matches(match.Groups[2].Value).OfType<Match>();
                foreach (var group in groups)
                {
                    for (int i = 1; i <= wordCountPerGroup; i++)
                    {
                        content += $"{group.Groups[i]}";
                        if (i == wordCountPerGroup)
                        {
                            content += "\n";
                        }
                        else
                        {
                            content += " ";
                        }
                    }
                }
                content = content.TrimEnd();

                var text = new Text
                {
                    Type = type,
                    Rarity = rarity,
                    Content = content,
                    WordCount = wordCounter.Matches(content).Count + 1
                };

                context.Text.Add(text);
            }
            file.Close();

            await context.SaveChangesAsync();

            Logger.LogInformation("Seeding done.");
        }
    }
}
