using ClubManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClubManagementApp.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(ClubManagementDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await context.Users.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Seed initial data
            await SeedDataAsync(context);
        }

        private static async Task SeedDataAsync(ClubManagementDbContext context)
        {
            // Create default clubs
            var clubs = new[]
            {
                new Club
                {
                    Name = "Computer Science Club",
                    Description = "A club for computer science enthusiasts",
                    CreatedDate = DateTime.Now.AddMonths(-6)
                },
                new Club
                {
                    Name = "Photography Club",
                    Description = "Capturing moments and learning photography",
                    CreatedDate = DateTime.Now.AddMonths(-4)
                }
            };

            context.Clubs.AddRange(clubs);
            await context.SaveChangesAsync();

            // Create default admin users
            var adminUser1 = new User
            {
                FullName = "System Administrator",
                Email = "admin@clubmanagement.com",
                Password = HashPassword("admin123"), // Default password
                Role = UserRole.Admin,
                JoinDate = DateTime.Now.AddYears(-1),
                IsActive = true,
                ActivityLevel = ActivityLevel.Active
            };

            var adminUser2 = new User
            {
                FullName = "University Administrator",
                Email = "admin@university.edu",
                Password = HashPassword("admin123"), // Default password
                Role = UserRole.Admin,
                JoinDate = DateTime.Now.AddYears(-1),
                IsActive = true,
                ActivityLevel = ActivityLevel.Active
            };

            // Create sample users
            var users = new[]
            {
                adminUser1,
                adminUser2,
                new User
                {
                    FullName = "John Doe",
                    Email = "john.doe@university.edu",
                    Password = HashPassword("password123"),
                    Role = UserRole.Chairman,
                    ClubID = clubs[0].ClubID,
                    JoinDate = DateTime.Now.AddMonths(-5),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Active
                },
                new User
                {
                    FullName = "Jane Smith",
                    Email = "jane.smith@university.edu",
                    Password = HashPassword("password123"),
                    Role = UserRole.Member,
                    ClubID = clubs[0].ClubID,
                    JoinDate = DateTime.Now.AddMonths(-3),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Normal
                },
                new User
                {
                    FullName = "Mike Johnson",
                    Email = "mike.johnson@university.edu",
                    Password = HashPassword("password123"),
                    Role = UserRole.ViceChairman,
                    ClubID = clubs[1].ClubID,
                    JoinDate = DateTime.Now.AddMonths(-4),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Active
                }
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            // Create sample events
            var events = new[]
            {
                new Event
                {
                    Name = "Programming Workshop",
                    Description = "Learn advanced programming techniques",
                    EventDate = DateTime.Now.AddDays(7),
                    Location = "Computer Lab A",
                    ClubID = clubs[0].ClubID,
                    CreatedDate = DateTime.Now.AddDays(-10)
                },
                new Event
                {
                    Name = "Photography Exhibition",
                    Description = "Showcase of student photography work",
                    EventDate = DateTime.Now.AddDays(14),
                    Location = "Art Gallery",
                    ClubID = clubs[1].ClubID,
                    CreatedDate = DateTime.Now.AddDays(-5)
                }
            };

            context.Events.AddRange(events);
            await context.SaveChangesAsync();

            // Create sample event participants
            var participants = new[]
            {
                new EventParticipant
                {
                    UserID = users[1].UserID, // Jane Smith
                    EventID = events[0].EventID,
                    Status = AttendanceStatus.Registered,
                    RegistrationDate = DateTime.Now.AddDays(-8)
                },
                new EventParticipant
                {
                    UserID = users[2].UserID, // Mike Johnson
                    EventID = events[1].EventID,
                    Status = AttendanceStatus.Registered,
                    RegistrationDate = DateTime.Now.AddDays(-3)
                }
            };

            context.EventParticipants.AddRange(participants);
            await context.SaveChangesAsync();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}