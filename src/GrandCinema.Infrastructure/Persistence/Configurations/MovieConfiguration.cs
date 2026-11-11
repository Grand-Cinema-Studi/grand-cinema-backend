using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrandCinema.Infrastructure.Persistence.Configurations;

public sealed class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title).HasMaxLength(200);
        builder.Property(m => m.Genre).HasMaxLength(50);
        builder.Property(m => m.PosterUrl).HasMaxLength(500);
    }
}
