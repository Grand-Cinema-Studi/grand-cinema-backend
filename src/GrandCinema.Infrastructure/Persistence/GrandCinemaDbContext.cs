using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrandCinema.Infrastructure.Persistence;

public class GrandCinemaDbContext : DbContext
{
    public GrandCinemaDbContext(DbContextOptions<GrandCinemaDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Screening> Screenings => Set<Screening>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservedSeat> ReservedSeats => Set<ReservedSeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GrandCinemaDbContext).Assembly);
    }
}
