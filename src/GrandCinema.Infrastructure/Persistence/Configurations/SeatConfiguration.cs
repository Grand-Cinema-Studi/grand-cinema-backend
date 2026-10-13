using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrandCinema.Infrastructure.Persistence.Configurations;

public sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Row).HasMaxLength(5);

        builder.HasOne(s => s.Room).WithMany(r => r.Seats).HasForeignKey(s => s.RoomId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.RoomId, s.Row, s.Number }).IsUnique();
    }
}
