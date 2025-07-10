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
                Console.WriteLine($"[DB_INIT] Found {userCount} users in database.");

                // Check for admin users specifically
                var adminUsers = await context.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();
                Console.WriteLine($"[DB_INIT] Found {adminUsers.Count} admin users in database.");

                if (userCount > 0 && adminUsers.Count >= 2)
                {
                    Console.WriteLine("[DB_INIT] Database already seeded. Listing existing admin users:");
                    foreach (var admin in adminUsers)
                    {
                        Console.WriteLine($"[DB_INIT] Admin user: {admin.Email} - {admin.FullName}");
                    }
                    return; // Database has been seeded
                }

                // If we have users but missing admin users, create them
                if (userCount > 0 && adminUsers.Count < 2)
                {
                    Console.WriteLine("[DB_INIT] Database has users but missing admin accounts. Creating admin users...");
                    await CreateAdminUsersAsync(context);
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
            Console.WriteLine("[DB_SEED] Creating default clubs...");
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
            Console.WriteLine($"[DB_SEED] Created {clubs.Length} clubs successfully.");

            Console.WriteLine("[DB_SEED] Creating default admin users...");
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
            Console.WriteLine($"[DB_SEED] Created admin user 1: {adminUser1.Email} with hashed password: {adminUser1.Password.Substring(0, 10)}...");

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
            Console.WriteLine($"[DB_SEED] Created admin user 2: {adminUser2.Email} with hashed password: {adminUser2.Password.Substring(0, 10)}...");

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
            Console.WriteLine($"[DB_SEED] Saved {users.Length} users to database successfully.");

            // Verify admin users were saved
            var savedAdminUsers = await context.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();
            Console.WriteLine($"[DB_SEED] Verification: Found {savedAdminUsers.Count} admin users in database:");
            foreach (var admin in savedAdminUsers)
            {
                Console.WriteLine($"[DB_SEED] - {admin.Email}: {admin.FullName}");
            }

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