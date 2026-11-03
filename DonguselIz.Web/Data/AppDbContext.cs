using Microsoft.EntityFrameworkCore;
using DonguselIz.Web.Models.Domain;

namespace DonguselIz.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyEmployee> Employees => Set<CompanyEmployee>();
        public DbSet<CompanyApplication> Applications => Set<CompanyApplication>();
        public DbSet<WasteBatch> Batches => Set<WasteBatch>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.TaxNumber).IsUnique();
                entity.HasMany(c => c.Employees)
                      .WithOne(e => e.Company)
                      .HasForeignKey(e => e.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CompanyEmployee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<CompanyApplication>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<WasteBatch>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.HasOne(b => b.Company)
                      .WithMany()
                      .HasForeignKey(b => b.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.ReservedCompany)
                      .WithMany()
                      .HasForeignKey(b => b.ReservedByCompanyId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
