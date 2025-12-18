namespace MicroServiceReports.Infraestructure.Persistence
{
    using Microsoft.EntityFrameworkCore;
    using MicroServiceReports.Domain.Models;

    public class MicroServiceReportsDbContext : DbContext
    {
        public MicroServiceReportsDbContext(DbContextOptions<MicroServiceReportsDbContext> options)
            : base(options)
        {
        }

        public DbSet<SaleEventRecord> SaleEventRecords { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map entity to table - adjust table name if your DB uses a different one
            modelBuilder.Entity<SaleEventRecord>(b =>
            {
                b.ToTable("sale_event_records");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedOnAdd();
                b.Property(x => x.SaleId).IsRequired();
                b.Property(x => x.Payload).IsRequired();
                b.Property(x => x.Exchange).HasMaxLength(200);
                b.Property(x => x.RoutingKey).HasMaxLength(200);
                b.Property(x => x.ReceivedAt).IsRequired();
                b.HasIndex(x => x.SaleId).IsUnique(false);
            });
        }
    }
}
