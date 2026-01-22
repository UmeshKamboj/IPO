using Microsoft.EntityFrameworkCore;
using IPOClient.Models.Entities;

namespace IPOClient.Data
{
    public class IPOClientDbContext : DbContext
    {
        public IPOClientDbContext(DbContextOptions<IPOClientDbContext> options)
            : base(options)
        {
        }

        public DbSet<IPO_UserMaster> IPO_UserMasters { get; set; }
        public DbSet<IPO_TypeMaster> IPO_TypeMaster { get; set; }  // Added
        public DbSet<IPO_IPOMaster> IPO_IPOMaster { get; set; }
        public DbSet<IPO_ApiLog> IPO_ApiLogs { get; set; }
        public DbSet<IPO_GroupMaster> IPO_GroupMaster { get; set; }
        public DbSet<IPO_BuyerPlaceOrderMaster> BuyerPlaceOrderMasters { get; set; }  //SELL/BUY Master table
        public DbSet<IPO_BuyerOrder> BuyerOrders { get; set; } //SELL/BUY Orders table
        public DbSet<IPO_PlaceOrderChild> ChildPlaceOrder { get; set; } //SELL/BUY Order Child table


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure IPO_UserMaster table
            modelBuilder.Entity<IPO_UserMaster>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FName).HasMaxLength(100);
                entity.Property(e => e.LName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
                entity.Property(e => e.Password).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.IsAdmin).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Email).IsUnique(); // Email should be unique
            });

            // Configure IPOTypeMaster table
            modelBuilder.Entity<IPO_TypeMaster>(entity =>
            {
                entity.HasKey(e => e.IPOTypeID);
                entity.Property(e => e.IPOTypeName).HasMaxLength(50).IsRequired();

                // Seed the enum values
                entity.HasData(
                    new IPO_TypeMaster { IPOTypeID = 1, IPOTypeName = "Main Board IPOs" },
                    new IPO_TypeMaster { IPOTypeID = 2, IPOTypeName = "SME IPOs" }
                );
            });

            // Configure IPO_ApiLog table (optimized for error-only logging)
            modelBuilder.Entity<IPO_ApiLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Method).HasMaxLength(10);
                entity.Property(e => e.Path).HasMaxLength(500);
                entity.Property(e => e.QueryString).HasMaxLength(2000);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.RequestTime).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.RequestTime); // Index for faster cleanup queries
                entity.HasIndex(e => e.StatusCode); // Index for error analysis
            });

            // Configure IPO_GroupMaster table
            modelBuilder.Entity<IPO_GroupMaster>(entity =>
            {
                entity.HasKey(e => e.IPOGroupId);
                entity.Property(e => e.GroupName).HasMaxLength(200);
                entity.Property(e => e.CompanyId).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.ToTable("IPO_GroupMaster");
            });
            // Configure IPO_BuyerPlaceOrderMaster table
            modelBuilder.Entity<IPO_BuyerPlaceOrderMaster>(entity =>
            {
                entity.HasKey(e => e.BuyerMasterId);
                entity.ToTable("IPO_BuyerPlaceOrderMaster");
            });

            modelBuilder.Entity<IPO_BuyerOrder>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.ToTable("IPO_BuyerOrder");

                entity.HasOne(o => o.BuyerMaster)
                      .WithMany(m => m.Orders)
                      .HasForeignKey(o => o.BuyerMasterId);

                // Specify precision and scale for Rate property
                entity.Property(e => e.Rate)
                      .HasPrecision(18, 4); // 18 total digits, 4 decimal places
            });
        }
    }

}
