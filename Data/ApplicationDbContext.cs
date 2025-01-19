
namespace price_comparator_site.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContexy(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .hasIndex(p => new { p.Name, p.StoreSource })
                .isUnique();
        }
    }
}
