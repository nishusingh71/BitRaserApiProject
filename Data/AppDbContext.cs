using BitRaserApiProject.Models;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<machines> machines { get; set; }
        public DbSet<audit_reports> audit_reportss { get; set; }
        public DbSet<users> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Machines Table
            modelBuilder.Entity<machines>()
                .Property(m => m.fingerprint_hash)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.mac_address)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.physical_drive_id)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.cpu_id)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.bios_serial)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.os_version)
                .HasMaxLength(255);

            modelBuilder.Entity<machines>()
                .Property(m => m.user_email)
                .HasMaxLength(255);

            modelBuilder.Entity<machines>()
                .Property(m => m.license_details_json)
                .HasColumnType("json");


            // Audit Reports Table
            modelBuilder.Entity<audit_reports>()
                .HasKey(a => a.report_id);

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.client_email)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.report_name)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.erasure_method)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.report_details_json)
                .HasColumnType("json")
                .IsRequired();

            // Users Table
            modelBuilder.Entity<users>()
                .HasKey(u => u.user_id);

            modelBuilder.Entity<users>()
                .Property(u => u.user_name)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<users>()
                .Property(u => u.user_email)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<users>()
                .HasIndex(u => u.user_email)
                .IsUnique();

            modelBuilder.Entity<users>()
                .Property(u => u.user_password)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<users>()
                .Property(u => u.phone_number)
                .HasMaxLength(20);

            modelBuilder.Entity<users>()
                .Property(u => u.payment_details_json)
                .HasColumnType("json");

            modelBuilder.Entity<users>()
                .Property(u => u.license_details_json)
                .HasColumnType("json");
        }
    }
}
