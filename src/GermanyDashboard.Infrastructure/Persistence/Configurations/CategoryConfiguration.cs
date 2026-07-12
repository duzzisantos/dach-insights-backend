using GermanyDashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GermanyDashboard.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.Property(c => c.Slug).HasMaxLength(64);
        builder.Property(c => c.Name).HasMaxLength(128);
        builder.Property(c => c.Icon).HasMaxLength(64);
        builder.Property(c => c.ColorSlot).HasMaxLength(32);
    }
}
