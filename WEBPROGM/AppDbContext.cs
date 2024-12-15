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
        // Настройка первичного ключа для модели cart
        modelBuilder.Entity<cart>()
            .HasKey(c => c.cart_id);

        // Настройка первичного ключа для модели order
        modelBuilder.Entity<order>()
            .HasKey(o => o.order_id);

        // Настройка первичного ключа для модели product
        modelBuilder.Entity<product>()
            .HasKey(p => p.product_id);

        // Настройка первичного ключа для модели user
        modelBuilder.Entity<user>()
            .HasKey(u => u.user_id);
    }
}


