using Microsoft.EntityFrameworkCore;
using POS_MSC.Models;
using System.Collections.Generic;

namespace POS_MSC.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<MSC_RequestModel> MSC_Request { get; set; }
        public DbSet<UploadModel> MSC_Request_Upload { get; set; }
        public DbSet<MSC_AuditLogModel> MSC_AuditLog { get; set; }
        public DbSet<UserSessionModel> UserSession { get; set; }
    }

    public class PersonelDbContext : DbContext
    {
        public PersonelDbContext(DbContextOptions<PersonelDbContext> options) : base(options) { }
        public DbSet<Personel> staff_personnel { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Personel as keyless
            modelBuilder.Entity<Personel>()
                .HasNoKey()
                .ToTable("staff_personnel"); // optional, map to table name if it differs
        }
    }
}
