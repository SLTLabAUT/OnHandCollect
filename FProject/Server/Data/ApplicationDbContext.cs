using FProject.Server.Models;
using FProject.Shared;
using FProject.Shared.Models;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Identity;
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
        //public DbSet<DeletedDrawing> DeletedDrawings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityRole>().HasData(new IdentityRole(IdentityRoleConstants.User) { Id = "afc9f911-04ae-4bc2-88a6-900ce65eca92", ConcurrencyStamp = "d3198c4c-4dd9-4d8c-8d35-e76757529aac", NormalizedName = IdentityRoleConstants.User.ToUpper() });
            builder.Entity<IdentityRole>().HasData(new IdentityRole(IdentityRoleConstants.Admin) { Id = "1c6b33d2-a1d8-42fa-924b-43449867f115", ConcurrencyStamp = "c0a582f7-49de-43f6-9314-d24b0879ce22", NormalizedName = IdentityRoleConstants.Admin.ToUpper() });

            builder.Entity<DrawingPoint>().HasKey(p => new { p.WritepadId, p.Number });
            builder.Entity<DrawingPoint>().HasQueryFilter(p => !p.IsDeleted);

            builder.Entity<Writepad>().HasIndex(w => new { w.UserSpecifiedNumber, w.OwnerId });
            builder.Entity<Writepad>().HasQueryFilter(w => !w.IsDeleted);

            //builder.Entity<DeletedDrawing>().HasNoKey();
        }
    }
}
