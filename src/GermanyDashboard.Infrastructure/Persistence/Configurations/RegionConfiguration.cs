using GermanyDashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GermanyDashboard.Infrastructure.Persistence.Configurations;

public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.HasIndex(r => r.Code).IsUnique();
        builder.HasIndex(r => r.Slug).IsUnique();

        builder.Property(r => r.Code).HasMaxLength(16);
        builder.Property(r => r.Slug).HasMaxLength(64);
        builder.Property(r => r.Name).HasMaxLength(128);
        builder.Property(r => r.NameEnglish).HasMaxLength(128);
        builder.Property(r => r.Capital).HasMaxLength(128);
        builder.Property(r => r.GeoJsonKey).HasMaxLength(64);

        builder.HasOne(r => r.ParentRegion)
            .WithMany(r => r.ChildRegions)
            .HasForeignKey(r => r.ParentRegionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
