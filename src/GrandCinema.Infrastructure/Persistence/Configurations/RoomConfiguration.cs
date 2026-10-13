using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrandCinema.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_Rooms_Capacity", "\"Capacity\" > 0"));

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(50);

        builder.HasIndex(r => r.Name).IsUnique();
    }
}
