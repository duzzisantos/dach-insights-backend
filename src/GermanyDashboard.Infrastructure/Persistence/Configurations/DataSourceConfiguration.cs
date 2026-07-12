using GermanyDashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GermanyDashboard.Infrastructure.Persistence.Configurations;

public class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(256);
        builder.Property(s => s.GenesisTableCode).HasMaxLength(32);
        builder.Property(s => s.Url).HasMaxLength(512);
        builder.Property(s => s.License).HasMaxLength(256);
    }
}
