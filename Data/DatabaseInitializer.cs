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
                if (userCount > 0 && clubCount > 0 && adminUsers.Count >= 1)
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
                        CreatedDate = new DateTime(2025, 1, 15)
                    },
                    new Club
                    {
                        Name = "Drama Society",
                        Description = "Dedicated to theatrical performances and dramatic arts",
                        CreatedDate = new DateTime(2025, 1, 20)
                    },
                    new Club
                    {
                        Name = "Environmental Club",
                        Description = "Focused on environmental conservation and sustainability",
                        CreatedDate = new DateTime(2025, 2, 1)
                    },
                    new Club
                    {
                        Name = "Photography Club",
                        Description = "For photography enthusiasts to improve skills",
                        CreatedDate = new DateTime(2025, 2, 10)
                    },
                    new Club
                    {
                        Name = "Debate Society",
                        Description = "Enhancing public speaking and critical thinking",
                        CreatedDate = new DateTime(2025, 2, 15)
                    },
                    new Club
                    {
                        Name = "Music Club",
                        Description = "Bringing together musicians of all levels",
                        CreatedDate = new DateTime(2025, 3, 1)
                    },
                    new Club
                    {
                        Name = "Sports Club",
                        Description = "Organizing various sports activities and tournaments",
                        CreatedDate = new DateTime(2025, 3, 5)
                    }
                };

                context.Clubs.AddRange(clubs);
                await context.SaveChangesAsync();
                Console.WriteLine($"[DB_SEED] Created {clubs.Length} clubs successfully.");
            }
            else
            {
                Console.WriteLine($"[DB_SEED] Found {existingClubs.Count} existing clubs, skipping club creation.");
                clubs = existingClubs.Take(7).ToArray();
            }

            // Create users only if they don't exist
            var existingUsers = await context.Users.ToListAsync();
            var existingEmails = existingUsers.Select(u => u.Email).ToHashSet();
            var usersToCreate = new List<User>();

            // Define all users to potentially create (1 Admin, 2 Chairman, 5 Member)
            var potentialUsers = new[]
            {
                // Admin (1 account)
                new User
                {
                    FullName = "System Administrator",
                    Email = "admin@university.edu",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Admin,
                    JoinDate = new DateTime(2025, 1, 1),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Active
                },

                // Chairman (2 accounts)
                new User
                {
                    FullName = "Alice Johnson",
                    Email = "alice.johnson@student.edu",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Chairman,
                    ClubID = clubs[0].ClubID,
                    JoinDate = new DateTime(2025, 1, 15),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Active
                },
                new User
                {
                    FullName = "Bob Smith",
                    Email = "bob.smith@student.edu",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Chairman,
                    ClubID = clubs[1].ClubID,
                    JoinDate = new DateTime(2025, 1, 20),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Active
                },

                // Member (5 accounts)
                new User
                {
                    FullName = "Carol Davis",
                    Email = "carol.davis@student.edu",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Member,
                    ClubID = clubs[2].ClubID,
                    JoinDate = new DateTime(2025, 2, 1),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Active
                },
                new User
                {
                    FullName = "David Wilson",
                    Email = "david.wilson@student.edu",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Member,
                    ClubID = clubs[3].ClubID,
                    JoinDate = new DateTime(2025, 2, 10),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Normal
                },
                new User
                {
                    FullName = "Emma Brown",
                    Email = "emma.brown@student.edu",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Member,
                    ClubID = clubs[4].ClubID,
                    JoinDate = new DateTime(2025, 2, 15),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Active
                },
                new User
                {
                    FullName = "Frank Miller",
                    Email = "frank.miller@student.edu",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Member,
                    ClubID = clubs[5].ClubID,
                    JoinDate = new DateTime(2025, 3, 1),
                    IsActive = true,
                    ActivityLevel = ActivityLevel.Normal
                },
                new User
                {
                    FullName = "Grace Lee",
                    Email = "grace.lee@student.edu",
                    Password = HashPassword("admin123"),
                    Role = UserRole.Member,
                    ClubID = clubs[6].ClubID,
                    JoinDate = new DateTime(2025, 3, 5),
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
                    Console.WriteLine($"[DB_SEED] Will create user: {user.Email} - {user.FullName} ({user.Role})");
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
                        Name = "Python Workshop",
                        Description = "Learn Python programming from basics to advanced concepts",
                        EventDate = new DateTime(2025, 12, 15, 14, 0, 0),
                        Location = "Computer Lab A",
                        ClubID = clubs[0].ClubID,
                        CreatedDate = new DateTime(2025, 11, 1)
                    },
                    new Event
                    {
                        Name = "Nature Photography Walk",
                        Description = "Capture the beauty of campus nature",
                        EventDate = new DateTime(2025, 12, 12, 8, 0, 0),
                        Location = "University Gardens",
                        ClubID = clubs[3].ClubID,
                        CreatedDate = new DateTime(2025, 11, 2)
                    },
                    new Event
                    {
                        Name = "Campus Cleanup Drive",
                        Description = "Help keep our campus clean and green",
                        EventDate = new DateTime(2025, 12, 13, 7, 0, 0),
                        Location = "Campus Grounds",
                        ClubID = clubs[2].ClubID,
                        CreatedDate = new DateTime(2025, 11, 4)
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
                events = await context.Events.Take(3).ToArrayAsync();
            }

            // Create sample event participants only if they don't exist
            var existingParticipants = await context.EventParticipants.CountAsync();
            if (existingParticipants == 0 && events.Length >= 3 && allUsers.Count >= 3)
            {
                var aliceJohnson = allUsers.FirstOrDefault(u => u.Email == "alice.johnson@student.edu");
                var bobSmith = allUsers.FirstOrDefault(u => u.Email == "bob.smith@student.edu");
                var carolDavis = allUsers.FirstOrDefault(u => u.Email == "carol.davis@student.edu");
                var davidWilson = allUsers.FirstOrDefault(u => u.Email == "david.wilson@student.edu");
                var emmaBrown = allUsers.FirstOrDefault(u => u.Email == "emma.brown@student.edu");
                var frankMiller = allUsers.FirstOrDefault(u => u.Email == "frank.miller@student.edu");
                var graceLee = allUsers.FirstOrDefault(u => u.Email == "grace.lee@student.edu");

                if (aliceJohnson != null && bobSmith != null && carolDavis != null && davidWilson != null && emmaBrown != null && frankMiller != null && graceLee != null)
                {
                    var participants = new[]
                    {
                        new EventParticipant
                        {
                            UserID = aliceJohnson.UserID,
                            EventID = events[0].EventID,
                            Status = AttendanceStatus.Registered,
                            RegistrationDate = new DateTime(2025, 12, 10)
                        },
                        new EventParticipant
                        {
                            UserID = bobSmith.UserID,
                            EventID = events[0].EventID,
                            Status = AttendanceStatus.Registered,
                            RegistrationDate = new DateTime(2025, 12, 11)
                        },
                        new EventParticipant
                        {
                            UserID = carolDavis.UserID,
                            EventID = events[1].EventID,
                            Status = AttendanceStatus.Registered,
                            RegistrationDate = new DateTime(2025, 12, 10)
                        },
                        new EventParticipant
                        {
                            UserID = davidWilson.UserID,
                            EventID = events[1].EventID,
                            Status = AttendanceStatus.Registered,
                            RegistrationDate = new DateTime(2025, 12, 11)
                        },
                        new EventParticipant
                        {
                            UserID = emmaBrown.UserID,
                            EventID = events[2].EventID,
                            Status = AttendanceStatus.Registered,
                            RegistrationDate = new DateTime(2025, 12, 10)
                        },
                        new EventParticipant
                        {
                            UserID = frankMiller.UserID,
                            EventID = events[2].EventID,
                            Status = AttendanceStatus.Registered,
                            RegistrationDate = new DateTime(2025, 12, 11)
                        },
                        new EventParticipant
                        {
                            UserID = graceLee.UserID,
                            EventID = events[2].EventID,
                            Status = AttendanceStatus.Registered,
                            RegistrationDate = new DateTime(2025, 12, 10)
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
            if (adminCount < 1)
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
            Console.WriteLine("[DB_ADMIN] Creating admin users...");

            // Check if admin users already exist
            var existingAdmins = await context.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();
            if (existingAdmins.Any())
            {
                Console.WriteLine($"[DB_ADMIN] Found {existingAdmins.Count} existing admin users:");
                foreach (var admin in existingAdmins)
                {
                    Console.WriteLine($"[DB_ADMIN] - {admin.Email}: {admin.FullName}");
                }
                return;
            }

            // Create admin user
            var adminUser = new User
            {
                FullName = "System Administrator",
                Email = "admin@university.edu",
                Password = HashPassword("admin123"),
                Role = UserRole.Admin,
                JoinDate = new DateTime(2025, 1, 1),
                IsActive = true,
                ActivityLevel = ActivityLevel.Active
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            Console.WriteLine($"[DB_ADMIN] Created admin user: {adminUser.Email} - {adminUser.FullName}");
            Console.WriteLine("[DB_ADMIN] Admin user created successfully with password: admin123");
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