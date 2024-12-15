using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<product> products { get; set; }
    public DbSet<user> users { get; set; }
    public DbSet<cart> carts { get; set; }
    public DbSet<order> orders { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
   
        modelBuilder.Entity<cart>()
            .HasKey(c => c.cart_id);

   
        modelBuilder.Entity<order>()
            .HasKey(o => o.order_id);

    
        modelBuilder.Entity<product>()
            .HasKey(p => p.product_id);

        modelBuilder.Entity<user>()
            .HasKey(u => u.user_id);
    }
}


