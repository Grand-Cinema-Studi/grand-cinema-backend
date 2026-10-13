using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrandCinema.Infrastructure.Persistence.Configurations;

public sealed class ScreeningConfiguration : IEntityTypeConfiguration<Screening>
{
    public void Configure(EntityTypeBuilder<Screening> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StartsAt).HasColumnType("timestamp without time zone");
        builder.Property(s => s.Price).HasPrecision(10, 2);

        builder.HasOne(s => s.Movie).WithMany().HasForeignKey(s => s.MovieId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Room).WithMany().HasForeignKey(s => s.RoomId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.RoomId, s.StartsAt });
    }
}
