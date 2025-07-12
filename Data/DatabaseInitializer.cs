using ClubManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ClubManagementApp.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(ClubManagementDbContext context)
        {
            try
            {
                Console.WriteLine("[DB_INIT] Starting database initialization...");

                // Test database connection first
                var canConnect = await context.Database.CanConnectAsync();
                Console.WriteLine($"[DB_INIT] Database connection test: {(canConnect ? "SUCCESS" : "FAILED")}");

                if (!canConnect)
                {
                    Console.WriteLine("[DB_INIT] Cannot connect to database. Please check connection string and SQL Server status.");
                    throw new InvalidOperationException("Database connection failed");
                }

                // Ensure database is created
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("[DB_INIT] Database created/verified successfully.");

                // Clean up any invalid ActivityLevel data
                await CleanupInvalidDataAsync(context);

                // Check if data already exists
                var userCount = await context.Users.CountAsync();
                var clubCount = await context.Clubs.CountAsync();
                var eventCount = await context.Events.CountAsync();
                Console.WriteLine($"[DB_INIT] Found {userCount} users, {clubCount} clubs, {eventCount} events in database.");

                // Check for admin users specifically
                var adminUsers = await context.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();
                Console.WriteLine($"[DB_INIT] Found {adminUsers.Count} admin users in database.");

                // Check if database is already fully seeded
                if (userCount > 0 && clubCount > 0 && adminUsers.Count >= 2)
                {
                    Console.WriteLine("[DB_INIT] Database already seeded. Listing existing admin users:");
                    foreach (var admin in adminUsers)
                    {
                        Console.WriteLine($"[DB_INIT] Admin user: {admin.Email} - {admin.FullName}");
                    }
                    return; // Database has been seeded
                }

                // If we have some data but it's incomplete, handle partial seeding
                if (userCount > 0 || clubCount > 0)
                {
                    Console.WriteLine("[DB_INIT] Database partially seeded. Checking what needs to be added...");
                    await HandlePartialSeedingAsync(context);
                    return;
                }

                // Seed initial data
                Console.WriteLine("[DB_INIT] Seeding initial data...");
                await SeedDataAsync(context);
                Console.WriteLine("[DB_INIT] Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB_INIT] ERROR: Database initialization failed: {ex.Message}");
                Console.WriteLine($"[DB_INIT] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static async Task SeedDataAsync(ClubManagementDbContext context)
        {
            // Create default clubs only if they don't exist
            var existingClubs = await context.Clubs.ToListAsync();
            Club[] clubs;

            if (!existingClubs.Any())
            {
                Console.WriteLine("[DB_SEED] Creating default clubs...");
                clubs = new[]
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
                Console.WriteLine($"[DB_SEED] Created {clubs.Length} clubs successfully.");
            }
            else
            {
                Console.WriteLine($"[DB_SEED] Found {existingClubs.Count} existing clubs, skipping club creation.");
                clubs = existingClubs.Take(2).ToArray();
            }

            // Create users only if they don't exist
            var existingUsers = await context.Users.ToListAsync();
            var existingEmails = existingUsers.Select(u => u.Email).ToHashSet();
            var usersToCreate = new List<User>();

            // Define all users to potentially create
            var potentialUsers = new[]
            {
                new User
                {
                    FullName = "System Administrator",
                    Email = "admin@clubmanagement.com",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Admin,
                    JoinDate = DateTime.Now.AddYears(-1),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Active
                },
                new User
                {
                    FullName = "University Administrator",
                    Email = "admin@university.edu",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Admin,
                    JoinDate = DateTime.Now.AddYears(-1),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Active
                },
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

            // Only add users that don't already exist
            foreach (var user in potentialUsers)
            {
                if (!existingEmails.Contains(user.Email))
                {
                    usersToCreate.Add(user);
                    Console.WriteLine($"[DB_SEED] Will create user: {user.Email} - {user.FullName}");
                }
                else
                {
                    Console.WriteLine($"[DB_SEED] User already exists, skipping: {user.Email}");
                }
            }

            if (usersToCreate.Any())
            {
                context.Users.AddRange(usersToCreate);
                await context.SaveChangesAsync();
                Console.WriteLine($"[DB_SEED] Created {usersToCreate.Count} new users successfully.");
            }
            else
            {
                Console.WriteLine("[DB_SEED] All users already exist, skipping user creation.");
            }

            // Get all users for event participant creation
            var allUsers = await context.Users.ToListAsync();

            // Verify admin users were saved
            var savedAdminUsers = await context.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();
            Console.WriteLine($"[DB_SEED] Verification: Found {savedAdminUsers.Count} admin users in database:");
            foreach (var admin in savedAdminUsers)
            {
                Console.WriteLine($"[DB_SEED] - {admin.Email}: {admin.FullName}");
            }

            // Check if events already exist (to avoid conflicts with seed script)
            var existingEventCount = await context.Events.CountAsync();
            Console.WriteLine($"[DB_SEED] Found {existingEventCount} existing events in database.");

            Event[] events;
            if (existingEventCount == 0)
            {
                Console.WriteLine("[DB_SEED] Creating sample events...");
                // Create sample events only if none exist
                events = new[]
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
                Console.WriteLine($"[DB_SEED] Created {events.Length} sample events successfully.");
            }
            else
            {
                Console.WriteLine("[DB_SEED] Events already exist, skipping event creation to avoid ID conflicts.");
                // Get existing events for participant creation
                events = await context.Events.Take(2).ToArrayAsync();
            }

            // Create sample event participants only if they don't exist
            var existingParticipants = await context.EventParticipants.CountAsync();
            if (existingParticipants == 0 && events.Length >= 2 && allUsers.Count >= 3)
            {
                var janeSmith = allUsers.FirstOrDefault(u => u.Email == "jane.smith@university.edu");
                var mikeJohnson = allUsers.FirstOrDefault(u => u.Email == "mike.johnson@university.edu");

                if (janeSmith != null && mikeJohnson != null)
                {
                    var participants = new[]
                    {
                        new EventParticipant
                        {
                            UserID = janeSmith.UserID,
                            EventID = events[0].EventID,
                            Status = AttendanceStatus.Registered,
                            RegistrationDate = DateTime.Now.AddDays(-8)
                        },
                        new EventParticipant
                        {
                            UserID = mikeJohnson.UserID,
                            EventID = events[1].EventID,
                            Status = AttendanceStatus.Registered,
                            RegistrationDate = DateTime.Now.AddDays(-3)
                        }
                    };

                    context.EventParticipants.AddRange(participants);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[DB_SEED] Created {participants.Length} event participants successfully.");
                }
                else
                {
                    Console.WriteLine("[DB_SEED] Could not find required users for event participants.");
                }
            }
            else
            {
                Console.WriteLine($"[DB_SEED] Event participants already exist ({existingParticipants}) or insufficient data, skipping participant creation.");
            }
        }

        private static async Task HandlePartialSeedingAsync(ClubManagementDbContext context)
        {
            Console.WriteLine("[DB_PARTIAL] Handling partial database seeding...");

            // Check what's missing and create only what's needed
            var clubCount = await context.Clubs.CountAsync();
            var userCount = await context.Users.CountAsync();
            var adminCount = await context.Users.CountAsync(u => u.Role == UserRole.Admin);
            var eventCount = await context.Events.CountAsync();

            Console.WriteLine($"[DB_PARTIAL] Current state: {clubCount} clubs, {userCount} users ({adminCount} admins), {eventCount} events");

            // Ensure we have admin users
            if (adminCount < 2)
            {
                Console.WriteLine("[DB_PARTIAL] Missing admin users, creating them...");
                await CreateAdminUsersAsync(context);
            }

            // If we have no clubs but have users, this might cause issues
            if (clubCount == 0 && userCount > 0)
            {
                Console.WriteLine("[DB_PARTIAL] No clubs found but users exist. Creating default clubs...");
                await SeedDataAsync(context);
            }
            else if (clubCount > 0 && userCount == 0)
            {
                Console.WriteLine("[DB_PARTIAL] Clubs exist but no users. Creating users...");
                await SeedDataAsync(context);
            }
            else
            {
                Console.WriteLine("[DB_PARTIAL] Database appears to be in a valid partial state.");
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hashedPassword = Convert.ToBase64String(hashedBytes);
            Console.WriteLine($"[DB_HASH] Hashing password '{password}' -> '{hashedPassword}'");
            return hashedPassword;
        }

        // Test method to verify password hashing matches UserService
        private static bool VerifyPasswordHash(string password, string hashedPassword)
        {
            var testHash = HashPassword(password);
            var matches = testHash == hashedPassword;
            Console.WriteLine($"[DB_HASH] Password verification: '{password}' -> Expected: {hashedPassword}, Got: {testHash}, Matches: {matches}");
            return matches;
        }

        private static async Task CleanupInvalidDataAsync(ClubManagementDbContext context)
        {
            try
            {
                Console.WriteLine("[DB_CLEANUP] Checking for invalid ActivityLevel data...");

                // Use raw SQL to find and fix invalid ActivityLevel values
                var invalidUsers = await context.Database.SqlQueryRaw<int>(
                    "SELECT UserID FROM Users WHERE ActivityLevel NOT IN ('Active', 'Normal', 'Inactive')"
                ).ToListAsync();

                if (invalidUsers.Any())
                {
                    Console.WriteLine($"[DB_CLEANUP] Found {invalidUsers.Count} users with invalid ActivityLevel values. Fixing...");

                    // Update invalid ActivityLevel values
                    await context.Database.ExecuteSqlRawAsync(
                        "UPDATE Users SET ActivityLevel = CASE " +
                        "WHEN ActivityLevel = 'High' THEN 'Active' " +
                        "WHEN ActivityLevel = 'Medium' THEN 'Normal' " +
                        "WHEN ActivityLevel = 'Low' THEN 'Inactive' " +
                        "ELSE 'Normal' END " +
                        "WHERE ActivityLevel NOT IN ('Active', 'Normal', 'Inactive')"
                    );

                    Console.WriteLine("[DB_CLEANUP] Invalid ActivityLevel values have been fixed.");
                }
                else
                {
                    Console.WriteLine("[DB_CLEANUP] No invalid ActivityLevel data found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB_CLEANUP] Warning: Could not cleanup invalid data: {ex.Message}");
                // Don't throw here as this is just cleanup
            }
        }

        private static async Task CreateAdminUsersAsync(ClubManagementDbContext context)
        {
            Console.WriteLine("[DB_ADMIN] Creating missing admin users...");

            var adminEmails = new[] { "admin@clubmanagement.com", "admin@university.edu" };
            var existingAdmins = await context.Users
                .Where(u => adminEmails.Contains(u.Email))
                .Select(u => u.Email)
                .ToListAsync();

            var usersToCreate = new List<User>();

            if (!existingAdmins.Contains("admin@clubmanagement.com"))
            {
                Console.WriteLine("[DB_ADMIN] Creating admin@clubmanagement.com...");
                usersToCreate.Add(new User
                {
                    FullName = "System Administrator",
                    Email = "admin@clubmanagement.com",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Admin,
                    ActivityLevel = ActivityLevel.Active,
                    JoinDate = DateTime.Now,
                    IsActive = true,
                    TwoFactorEnabled = false
                });
            }
            else
            {
                Console.WriteLine("[DB_ADMIN] admin@clubmanagement.com already exists, skipping...");
            }

            if (!existingAdmins.Contains("admin@university.edu"))
            {
                Console.WriteLine("[DB_ADMIN] Creating admin@university.edu...");
                usersToCreate.Add(new User
                {
                    FullName = "University Admin",
                    Email = "admin@university.edu",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Admin,
                    ActivityLevel = ActivityLevel.Active,
                    JoinDate = DateTime.Now,
                    IsActive = true,
                    TwoFactorEnabled = false
                });
            }
            else
            {
                Console.WriteLine("[DB_ADMIN] admin@university.edu already exists, skipping...");
            }

            if (usersToCreate.Any())
            {
                await context.Users.AddRangeAsync(usersToCreate);
                await context.SaveChangesAsync();
                Console.WriteLine($"[DB_ADMIN] Created {usersToCreate.Count} admin users successfully.");
            }
            else
            {
                Console.WriteLine("[DB_ADMIN] All admin users already exist, no creation needed.");
            }
        }

        public static async Task ResetDatabaseAsync(ClubManagementDbContext context)
        {
            try
            {
                Console.WriteLine("[DB_RESET] Starting database reset...");

                // Delete and recreate database
                await context.Database.EnsureDeletedAsync();
                Console.WriteLine("[DB_RESET] Database deleted successfully.");

                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("[DB_RESET] Database recreated successfully.");

                // Seed fresh data
                await SeedDataAsync(context);
                Console.WriteLine("[DB_RESET] Database reset and seeded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB_RESET] ERROR: Database reset failed: {ex.Message}");
                throw;
            }
        }
    }
}