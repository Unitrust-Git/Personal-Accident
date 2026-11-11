using GPA.Core.Models;
using GPA.Core.Models.Policy;
using Microsoft.EntityFrameworkCore;

namespace GPA.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = default!;
        public DbSet<IndividualPolicy> IndividualPolicies { get; set; } = default!;
        public DbSet<CorporatePolicy> CorporatePolicies { get; set; } = default!;
        public DbSet<CorporatePolicyBeneficiary> CorporatePolicyBeneficiaries { get; set; } = default!;
        public DbSet<PolicyProvisioningJob> PolicyProvisioningJobs { get; set; } = default!;

        public DbSet<ClaimedJobRow> ClaimedJobRows => Set<ClaimedJobRow>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            modelBuilder.Entity<IndividualPolicy>()
                .HasIndex(x => x.PaymentTransactionId)
                .IsUnique();

            modelBuilder.Entity<CorporatePolicy>()
                .HasIndex(x => x.PaymentTransactionId)
                .IsUnique();

            modelBuilder.Entity<PolicyProvisioningJob>(b =>
            {
                b.Property(x => x.RowVersion)
                    .IsRowVersion()
                    .IsConcurrencyToken();

                b.Property(x => x.LockedBy).HasMaxLength(200);
                b.HasIndex(x => x.NextAttemptAt);
                b.HasIndex(x => x.LockedUntilUtc);
                b.HasIndex(x => x.Status);
            });

            modelBuilder.Entity<ClaimedJobRow>(eb =>
            {
                eb.HasNoKey();
                eb.ToView(null);
            });

            ConfigurePolicyBase(modelBuilder.Entity<IndividualPolicy>());
            ConfigurePolicyBase(modelBuilder.Entity<CorporatePolicy>());
        }

        private static void ConfigurePolicyBase<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> builder)
            where TEntity : PolicyBase
        {
            builder.Property(x => x.RowVersion)
                   .IsRowVersion()
                   .IsConcurrencyToken();

            builder.Property(x => x.LockedBy).HasMaxLength(200);
            builder.Property(x => x.LockToken);

            builder.OwnsOne(x => x.PaymentStep, s =>
            {
                s.Property(p => p.Status).HasConversion<int>();
                s.Property(p => p.LastError).HasMaxLength(2000);
            });

            builder.OwnsOne(x => x.PolicyStep, s =>
            {
                s.Property(p => p.Status).HasConversion<int>();
                s.Property(p => p.LastError).HasMaxLength(2000);
            });

            builder.OwnsOne(x => x.BillingStep, s =>
            {
                s.Property(p => p.Status).HasConversion<int>();
                s.Property(p => p.LastError).HasMaxLength(2000);
            });

            builder.OwnsOne(x => x.ReceiptStep, s =>
            {
                s.Property(p => p.Status).HasConversion<int>();
                s.Property(p => p.LastError).HasMaxLength(2000);
            });

            builder.Navigation(x => x.PaymentStep).IsRequired();
            builder.Navigation(x => x.PolicyStep).IsRequired();
            builder.Navigation(x => x.BillingStep).IsRequired();
            builder.Navigation(x => x.ReceiptStep).IsRequired();
        }
    }
}
