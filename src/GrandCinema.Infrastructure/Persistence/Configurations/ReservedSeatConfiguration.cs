using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrandCinema.Infrastructure.Persistence.Configurations;

public sealed class ReservedSeatConfiguration : IEntityTypeConfiguration<ReservedSeat>
{
    public void Configure(EntityTypeBuilder<ReservedSeat> builder)
    {
        builder.HasKey(rs => new { rs.ReservationId, rs.SeatId });

        builder.HasOne(rs => rs.Reservation).WithMany(r => r.ReservedSeats).HasForeignKey(rs => rs.ReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rs => rs.Seat).WithMany().HasForeignKey(rs => rs.SeatId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Screening>().WithMany().HasForeignKey(rs => rs.ScreeningId).OnDelete(DeleteBehavior.Restrict);

        // Prevents selling the same seat twice for a screening
        builder.HasIndex(rs => new { rs.ScreeningId, rs.SeatId }).IsUnique();
    }
}
