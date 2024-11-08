using Microsoft.EntityFrameworkCore;

namespace ErgoFab.Model
{
    public class ErgoFabDbContext : DbContext
    {

        public ErgoFabDbContext() : base(GetDesignTimeDbContextOptions())
        {
        }

        public ErgoFabDbContext(DbContextOptions options) : base(options)
        {
        }

        static DbContextOptions GetDesignTimeDbContextOptions()
        {
            return new DbContextOptionsBuilder<ErgoFabDbContext>()
                .UseSqlServer(DesignTimeSupport.GetDesignTimeConnectionString())
                .Options;
        }



        /*
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(DesignTimeSupport.GetDesignTimeConnectionString(), x =>
            {
                x.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), new[] { 1205 });
                x.UseAzureSqlDefaults();
            });
        }
        */



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Country>().Property(e => e.Flag).IsRequired(false);


            modelBuilder.Entity<RegionalDivision>()
                .HasOne(e => e.ParentOrganization)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.TheOrganization)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Organization>()
                .HasMany(e => e.TheEmployees)
                .WithOne(e => e.TheOrganization)
                .HasForeignKey(e => e.OrganizationId)
                .HasPrincipalKey(e => e.Id);

            modelBuilder.Entity<Organization>()
                .HasMany(e => e.TheSubDivisions)
                .WithOne(e => e.ParentOrganization)
                .HasForeignKey(e => e.IdParent)
                .HasPrincipalKey(e => e.Id);
        }

        public DbSet<Employee> Employee { get; set; }

        public DbSet<Customer> Customer { get; set; }

        public DbSet<Project> Project { get; set; }

        public DbSet<Occupation> Occupation { get; set; }

        public DbSet<Organization> Organization { get; set; }

        public DbSet<Department> Department { get; set; }

        public DbSet<Country> Country { get; set; }

    }
}