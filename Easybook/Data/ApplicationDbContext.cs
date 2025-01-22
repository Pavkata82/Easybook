using Easybook.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Easybook.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<BookingDateRange> BookingDateRanges { get; set; }
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<HotelFacilities> HotelFacilities { get; set; }
        public DbSet<Status> Statuses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Specify the precision and scale for the Price property of Room
            modelBuilder.Entity<Room>()
                .Property(r => r.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<HotelFacilities>()
                .HasKey(hf => new { hf.HotelId, hf.FacilityId });

            modelBuilder.Entity<HotelFacilities>()
                .HasOne(hf => hf.Hotel)
                .WithMany(h => h.HotelFacilities)
                .HasForeignKey(hf => hf.HotelId);

            modelBuilder.Entity<HotelFacilities>()
                .HasOne(hf => hf.Facility)
                .WithMany(f => f.HotelFacilities)
                .HasForeignKey(hf => hf.FacilityId);

            modelBuilder.Entity<Image>()
                .HasOne(i => i.Hotel)
                .WithMany(h => h.Images)
                .HasForeignKey(i => i.HotelId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Hotel)
                .WithMany()
                .HasForeignKey(b => b.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship between Booking and Status
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Status) // Navigation property in Booking
                .WithMany(s => s.bookings) // Navigation property in Status
                .HasForeignKey(b => b.StatusId) // Foreign key in Booking
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship between BookingDateRange and Booking
            modelBuilder.Entity<BookingDateRange>()
                .HasOne(bdr => bdr.Booking)
                .WithMany(b => b.BookingDateRanges)
                .HasForeignKey(bdr => bdr.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingDateRange>()
                .HasOne(bdr => bdr.Room)
                .WithMany(r => r.BookingDateRanges)
                .HasForeignKey(bdr => bdr.RoomId);
        }

    }
}
