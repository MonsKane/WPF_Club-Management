using ClubManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementApp.Data
{
    public class ClubManagementDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<ScheduledNotification> ScheduledNotifications { get; set; }

        public ClubManagementDbContext(DbContextOptions<ClubManagementDbContext> options) : base(options) { }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    //if (!optionsBuilder.IsConfigured)
        //    //{
        //    //    // Load configuration from appsettings.json
        //    //    var configuration = new ConfigurationBuilder()
        //    //        .SetBasePath(Directory.GetCurrentDirectory())
        //    //        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        //    //        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
        //    //        .Build();

        //    //    var connectionString = configuration.GetConnectionString("DefaultConnection");
        //    //    optionsBuilder.UseSqlServer(connectionString);
        //    //}
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasConversion<string>();
                entity.Property(e => e.ActivityLevel).HasConversion<string>();

                entity.HasOne(e => e.Club)
                      .WithMany(c => c.Members)
                      .HasForeignKey(e => e.ClubID)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Club entity
            modelBuilder.Entity<Club>(entity =>
            {
                entity.HasKey(e => e.ClubID);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Event entity
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.EventID);
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(e => e.Club)
                      .WithMany(c => c.Events)
                      .HasForeignKey(e => e.ClubID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure EventParticipant entity
            modelBuilder.Entity<EventParticipant>(entity =>
            {
                entity.HasKey(e => e.ParticipantID);
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(e => e.User)
                      .WithMany(u => u.EventParticipations)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Event)
                      .WithMany(ev => ev.Participants)
                      .HasForeignKey(e => e.EventID)
                      .OnDelete(DeleteBehavior.Cascade);

                // Ensure a user can only register once per event
                entity.HasIndex(e => new { e.UserID, e.EventID }).IsUnique();
            });

            // Configure Report entity
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.ReportID);
                entity.Property(e => e.Type).HasConversion<string>();

                entity.HasOne(e => e.Club)
                      .WithMany()
                      .HasForeignKey(e => e.ClubID)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.GeneratedByUser)
                      .WithMany(u => u.GeneratedReports)
                      .HasForeignKey(e => e.GeneratedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure AuditLog entity
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LogType).HasConversion<string>();
                entity.Property(e => e.Action).HasMaxLength(255);
                entity.Property(e => e.Details).HasMaxLength(2000);
                entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 max length

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.LogType);
                entity.HasIndex(e => e.UserId);
            });

            // Configure Setting entity
            modelBuilder.Entity<Setting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Scope).HasConversion<string>();
                entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Value).IsRequired();

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Club)
                      .WithMany()
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.Key, e.Scope, e.UserId, e.ClubId }).IsUnique();
                entity.HasIndex(e => e.Scope);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configure Notification entity
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Priority).HasConversion<string>();
                entity.Property(e => e.Category).HasConversion<string>();
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
                entity.Property(e => e.ChannelsJson).HasMaxLength(500);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Club)
                      .WithMany()
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Event)
                      .WithMany()
                      .HasForeignKey(e => e.EventId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Category);
            });

            // Configure NotificationTemplate entity
            modelBuilder.Entity<NotificationTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Priority).HasConversion<string>();
                entity.Property(e => e.Category).HasConversion<string>();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.TitleTemplate).HasMaxLength(200).IsRequired();
                entity.Property(e => e.MessageTemplate).HasMaxLength(2000).IsRequired();
                entity.Property(e => e.ChannelsJson).HasMaxLength(500);
                entity.Property(e => e.ParametersJson).HasMaxLength(1000);

                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.IsActive);
            });

            // Configure ScheduledNotification entity
            modelBuilder.Entity<ScheduledNotification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.NotificationRequest).IsRequired();
                entity.Property(e => e.RecurrencePattern).HasMaxLength(500);

                entity.HasIndex(e => e.ScheduledTime);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.Name);
            });
        }
    }
}