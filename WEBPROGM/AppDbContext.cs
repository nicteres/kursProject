using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Product> products { get; set; }
    public DbSet<User> users { get; set; }
    public DbSet<Cart> carts { get; set; }
    public DbSet<Order> orders { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
   
        modelBuilder.Entity<Cart>()
            .HasKey(c => c.cart_id);

   
        modelBuilder.Entity<Order>()
            .HasKey(o => o.order_id);

    
        modelBuilder.Entity<Product>()
            .HasKey(p => p.product_id);

        modelBuilder.Entity<User>()
            .HasKey(u => u.user_id);

        modelBuilder.HasPostgresEnum<order_status>("order_status");
    }
}


