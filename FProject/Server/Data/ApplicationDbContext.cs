using FProject.Server.Models;
using FProject.Shared;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Data
{
    public class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions options,
            IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions)
        {
        }

        public DbSet<Text> Text { get; set; }
        public DbSet<Writepad> Writepads { get; set; }
        public DbSet<DrawingPoint> Points { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<DrawingPoint>().HasKey(p => new { p.WritepadId, p.Number });
            builder.Entity<DrawingPoint>().HasQueryFilter(p => !p.IsDeleted);

            builder.Entity<Writepad>().HasQueryFilter(w => !w.IsDeleted);
        }
    }
}
