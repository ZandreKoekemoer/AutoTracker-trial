using AutoTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies { get; set; }
    public DbSet<CompanySetting> CompanySettings { get; set; }
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<RepairJob> RepairJobs => Set<RepairJob>();
    public DbSet<JobDocument> JobDocuments => Set<JobDocument>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<RfidTag> RfidTags { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<RfidTagHistory> RfidTagHistories { get; set; }
    public DbSet<JobAuditLog> JobAuditLogs { get; set; }
    public DbSet<JobPhoto> JobPhotos { get; set; }
    public DbSet<VehicleDamageItem> VehicleDamageItems { get; set; }
    public DbSet<JobTimelineEntry> JobTimelineEntries { get; set; }
    public DbSet<CustomerSignature> CustomerSignatures { get; set; }
    public DbSet<ChecklistItem> ChecklistItems { get; set; }
    public DbSet<QuoteLineItem> QuoteLineItems { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<ClientTrackingAccess> ClientTrackingAccesses { get; set; }
    public DbSet<UserSecurityAuditLog> UserSecurityAuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>()
            .HasMany(c => c.Vehicles)
            .WithOne(v => v.Client)
            .HasForeignKey(v => v.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RepairJob>()
            .HasOne(r => r.Client)
            .WithMany(c => c.RepairJobs)
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RepairJob>()
            .HasOne(r => r.Vehicle)
            .WithMany(v => v.RepairJobs)
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RepairJob>()
            .HasIndex(r => r.RfidTagCode);

        modelBuilder.Entity<RepairJob>()
            .HasIndex(r => r.AssignedTechnicianUserId);

        modelBuilder.Entity<RfidTag>()
            .HasIndex(t => t.TagCode)
            .IsUnique();

        modelBuilder.Entity<RfidTag>()
            .HasIndex(t => t.CurrentRepairJobId)
            .IsUnique()
            .HasFilter("CurrentRepairJobId IS NOT NULL");

        modelBuilder.Entity<ClientTrackingAccess>()
            .HasIndex(x => x.TokenHash)
            .IsUnique();

        modelBuilder.Entity<ClientTrackingAccess>()
            .HasOne(x => x.RepairJob)
            .WithMany(j => j.TrackingAccesses)
            .HasForeignKey(x => x.RepairJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
