using GPA.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.Persistence.Configurations
{
    public class PaymentTransactionConfiguration
        : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.ToTable("PaymentTransactions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Reference)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Reference)
                .IsUnique();

            builder.Property(x => x.GatewayReference)
                .HasMaxLength(100);

            builder.Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(x => x.Gateway)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Metadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();
        }
    }

}
