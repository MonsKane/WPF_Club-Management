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
                var adminUsers = await context.Users.Where(u => u.SystemRole == SystemRole.Admin).ToListAsync();
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
            // Create admin users first
            await CreateAdminUsersIfNeededAsync(context);
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.SystemRole == SystemRole.Admin);

            if (adminUser == null)
            {
                throw new InvalidOperationException("No admin user found for club creation");
            }

            // Create default clubs only if they don't exist
            var existingClubs = await context.Clubs.ToListAsync();
            Club[] clubs;

            if (!existingClubs.Any())
            {
                Console.WriteLine("[DB_SEED] Creating 3 default clubs...");
                clubs = new[]
                {
                    new Club
                    {
                        ClubName = "Computer Science Club",
                        Description = "Technology and programming club",
                        EstablishedDate = DateTime.Now.AddMonths(-6).Date,
                        CreatedUserId = adminUser.UserID
                    },
                    new Club
                    {
                        ClubName = "Photography Club",
                        Description = "Photography and visual arts club",
                        EstablishedDate = DateTime.Now.AddMonths(-4).Date,
                        CreatedUserId = adminUser.UserID
                    },
                    new Club
                    {
                        ClubName = "Music Club",
                        Description = "Music appreciation and performance club",
                        EstablishedDate = DateTime.Now.AddMonths(-3).Date,
                        CreatedUserId = adminUser.UserID
                    }
                };

                context.Clubs.AddRange(clubs);
                await context.SaveChangesAsync();
                Console.WriteLine($"[DB_SEED] Created {clubs.Length} clubs successfully.");
            }
            else
            {
                Console.WriteLine($"[DB_SEED] Found {existingClubs.Count} existing clubs, skipping club creation.");
                clubs = existingClubs.ToArray();
            }

            // Create test users as specified in documentation
            var existingUsers = await context.Users.ToListAsync();
            var existingEmails = existingUsers.Select(u => u.Email).ToHashSet();
            var usersToCreate = new List<User>();

            // Define test users from documentation
            var potentialUsers = new[]
            {
                // Main test accounts from documentation table
                new User
                {
                    FullName = "John Doe",
                    Email = "john.doe@university.edu",
                    Password = HashPassword("password123"),
                    SystemRole = SystemRole.ClubOwner,
                    CreatedAt = DateTime.Now.AddMonths(-5)
                },
                new User
                {
                    FullName = "Jane Smith",
                    Email = "jane.smith@university.edu",
                    Password = HashPassword("password123"),
                    SystemRole = SystemRole.Member,
                    CreatedAt = DateTime.Now.AddMonths(-3)
                },
                new User
                {
                    FullName = "Mike Johnson",
                    Email = "mike.johnson@university.edu",
                    Password = HashPassword("password123"),
                    SystemRole = SystemRole.Member,
                    CreatedAt = DateTime.Now.AddMonths(-4)
                },
                new User
                {
                    FullName = "Sarah Wilson",
                    Email = "sarah.wilson@university.edu",
                    Password = HashPassword("password123"),
                    SystemRole = SystemRole.Member,
                    CreatedAt = DateTime.Now.AddMonths(-2)
                },
                new User
                {
                    FullName = "David Brown",
                    Email = "david.brown@university.edu",
                    Password = HashPassword("password123"),
                    SystemRole = SystemRole.Member,
                    CreatedAt = DateTime.Now.AddMonths(-1)
                },
                // Additional test accounts from step-by-step guide
                new User
                {
                    FullName = "Admin Manager",
                    Email = "admin.manager@university.edu",
                    Password = HashPassword("admin123"),
                    SystemRole = SystemRole.Admin,
                    CreatedAt = DateTime.Now.AddMonths(-6)
                },
                new User
                {
                    FullName = "Alice Johnson",
                    Email = "alice.johnson@student.edu",
                    Password = HashPassword("admin123"),
                    SystemRole = SystemRole.ClubOwner,
                    CreatedAt = DateTime.Now.AddMonths(-5)
                },
                new User
                {
                    FullName = "Michael Chen",
                    Email = "michael.chen@student.edu",
                    Password = HashPassword("admin123"),
                    SystemRole = SystemRole.ClubOwner,
                    CreatedAt = DateTime.Now.AddMonths(-4)
                },
                new User
                {
                    FullName = "Kate Williams",
                    Email = "kate.williams@student.edu",
                    Password = HashPassword("admin123"),
                    SystemRole = SystemRole.Member,
                    CreatedAt = DateTime.Now.AddMonths(-3)
                },
                new User
                {
                    FullName = "Lisa Thompson",
                    Email = "lisa.thompson@student.edu",
                    Password = HashPassword("admin123"),
                    SystemRole = SystemRole.Member,
                    CreatedAt = DateTime.Now.AddMonths(-2)
                }
            };

            // Only add users that don't already exist
            foreach (var user in potentialUsers)
            {
                if (!existingEmails.Contains(user.Email))
                {
                    usersToCreate.Add(user);
                    Console.WriteLine($"[DB_SEED] Will create user: {user.Email} - {user.FullName} ({user.SystemRole})");
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

            // Get all users for club membership creation
            var allUsers = await context.Users.ToListAsync();

            // Create club memberships for the users
            await CreateClubMembershipsAsync(context, clubs);

            // Verify admin users were saved
            var savedAdminUsers = await context.Users.Where(u => u.SystemRole == SystemRole.Admin).ToListAsync();
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
                        Description = "Learn advanced programming techniques and best practices",
                        EventDate = DateTime.Now.AddDays(7),
                        Location = "Computer Lab A",
                        ClubID = clubs[0].ClubID,
                        CreatedDate = DateTime.Now.AddDays(-10)
                    },
                    new Event
                    {
                        Name = "Photography Exhibition",
                        Description = "Showcase of student photography work and techniques",
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

            // Create event participants only if none exist
            var existingParticipants = await context.EventParticipants.CountAsync();
            if (existingParticipants == 0 && events.Any() && allUsers.Any())
            {
                Console.WriteLine("[DB_SEED] Creating sample event participants...");
                var participants = new List<EventParticipant>();

                // Add some users to events
                var nonAdminUsers = allUsers.Where(u => u.SystemRole != SystemRole.Admin).Take(3).ToList();
                foreach (var user in nonAdminUsers)
                {
                    if (events.Length > 0)
                    {
                        participants.Add(new EventParticipant
                        {
                            UserID = user.UserID,
                            EventID = events[0].EventID,
                            Status = AttendanceStatus.Registered,
                            RegistrationDate = DateTime.Now.AddDays(-3)
                        });
                    }
                }

                if (participants.Any())
                {
                    context.EventParticipants.AddRange(participants);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[DB_SEED] Created {participants.Count} event participants successfully.");
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
            var adminCount = await context.Users.CountAsync(u => u.SystemRole == SystemRole.Admin);
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
                Console.WriteLine("[DB_CLEANUP] Checking for invalid data...");
                // No specific cleanup needed for new schema
                Console.WriteLine("[DB_CLEANUP] No cleanup required for current schema.");
                await Task.CompletedTask; // Suppress async warning
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB_CLEANUP] Warning: Could not cleanup invalid data: {ex.Message}");
                // Don't throw here as this is just cleanup
            }
        }

        private static async Task CreateAdminUsersIfNeededAsync(ClubManagementDbContext context)
        {
            var adminCount = await context.Users.CountAsync(u => u.SystemRole == SystemRole.Admin);
            if (adminCount == 0)
            {
                await CreateAdminUsersAsync(context);
            }
        }

        private static async Task CreateClubMembershipsAsync(ClubManagementDbContext context, Club[] clubs)
        {
            Console.WriteLine("[DB_SEED] Creating club memberships...");

            var existingMemberships = await context.ClubMembers.CountAsync();
            if (existingMemberships > 0)
            {
                Console.WriteLine("[DB_SEED] Club memberships already exist, skipping creation.");
                return;
            }

            // Get all the users we created
            var johnDoe = await context.Users.FirstOrDefaultAsync(u => u.Email == "john.doe@university.edu");
            var janeSmith = await context.Users.FirstOrDefaultAsync(u => u.Email == "jane.smith@university.edu");
            var mikeJohnson = await context.Users.FirstOrDefaultAsync(u => u.Email == "mike.johnson@university.edu");
            var sarahWilson = await context.Users.FirstOrDefaultAsync(u => u.Email == "sarah.wilson@university.edu");
            var davidBrown = await context.Users.FirstOrDefaultAsync(u => u.Email == "david.brown@university.edu");
            var aliceJohnson = await context.Users.FirstOrDefaultAsync(u => u.Email == "alice.johnson@student.edu");
            var michaelChen = await context.Users.FirstOrDefaultAsync(u => u.Email == "michael.chen@student.edu");
            var kateWilliams = await context.Users.FirstOrDefaultAsync(u => u.Email == "kate.williams@student.edu");
            var lisaThompson = await context.Users.FirstOrDefaultAsync(u => u.Email == "lisa.thompson@student.edu");

            // Find clubs by name for reliable assignment
            var computerScienceClub = clubs.FirstOrDefault(c => c.ClubName == "Computer Science Club");
            var photographyClub = clubs.FirstOrDefault(c => c.ClubName == "Photography Club");
            var musicClub = clubs.FirstOrDefault(c => c.ClubName == "Music Club");

            var memberships = new List<ClubMember>();

            // Computer Science Club memberships
            // Chairman: John Doe, Members: Jane Smith, Mike Johnson
            if (johnDoe != null && computerScienceClub != null)
            {
                memberships.Add(new ClubMember
                {
                    UserID = johnDoe.UserID,
                    ClubID = computerScienceClub.ClubID,
                    ClubRole = ClubRole.Chairman,
                    JoinDate = DateTime.Now.AddMonths(-5)
                });
                Console.WriteLine($"[DB_SEED] Added John Doe as Chairman of {computerScienceClub.ClubName}");
            }

            if (janeSmith != null && computerScienceClub != null)
            {
                memberships.Add(new ClubMember
                {
                    UserID = janeSmith.UserID,
                    ClubID = computerScienceClub.ClubID,
                    ClubRole = ClubRole.Member,
                    JoinDate = DateTime.Now.AddMonths(-3)
                });
                Console.WriteLine($"[DB_SEED] Added Jane Smith as Member of {computerScienceClub.ClubName}");
            }

            if (mikeJohnson != null && computerScienceClub != null)
            {
                memberships.Add(new ClubMember
                {
                    UserID = mikeJohnson.UserID,
                    ClubID = computerScienceClub.ClubID,
                    ClubRole = ClubRole.Member,
                    JoinDate = DateTime.Now.AddMonths(-4)
                });
                Console.WriteLine($"[DB_SEED] Added Mike Johnson as Member of {computerScienceClub.ClubName}");
            }

            // Add Alice Johnson to Computer Science Club as secondary owner
            if (aliceJohnson != null && computerScienceClub != null)
            {
                memberships.Add(new ClubMember
                {
                    UserID = aliceJohnson.UserID,
                    ClubID = computerScienceClub.ClubID,
                    ClubRole = ClubRole.Admin,
                    JoinDate = DateTime.Now.AddMonths(-5)
                });
                Console.WriteLine($"[DB_SEED] Added Alice Johnson as Admin of {computerScienceClub.ClubName}");
            }

            // Photography Club memberships
            // Admin: Sarah Wilson, Members: David Brown
            if (sarahWilson != null && photographyClub != null)
            {
                memberships.Add(new ClubMember
                {
                    UserID = sarahWilson.UserID,
                    ClubID = photographyClub.ClubID,
                    ClubRole = ClubRole.Admin,
                    JoinDate = DateTime.Now.AddMonths(-2)
                });
                Console.WriteLine($"[DB_SEED] Added Sarah Wilson as Admin of {photographyClub.ClubName}");
            }

            if (davidBrown != null && photographyClub != null)
            {
                memberships.Add(new ClubMember
                {
                    UserID = davidBrown.UserID,
                    ClubID = photographyClub.ClubID,
                    ClubRole = ClubRole.Member,
                    JoinDate = DateTime.Now.AddMonths(-1)
                });
                Console.WriteLine($"[DB_SEED] Added David Brown as Member of {photographyClub.ClubName}");
            }

            // Music Club memberships
            // Chairman: Michael Chen, Members: Kate Williams, Lisa Thompson
            if (michaelChen != null && musicClub != null)
            {
                memberships.Add(new ClubMember
                {
                    UserID = michaelChen.UserID,
                    ClubID = musicClub.ClubID,
                    ClubRole = ClubRole.Chairman,
                    JoinDate = DateTime.Now.AddMonths(-4)
                });
                Console.WriteLine($"[DB_SEED] Added Michael Chen as Chairman of {musicClub.ClubName}");
            }

            if (kateWilliams != null && musicClub != null)
            {
                memberships.Add(new ClubMember
                {
                    UserID = kateWilliams.UserID,
                    ClubID = musicClub.ClubID,
                    ClubRole = ClubRole.Member,
                    JoinDate = DateTime.Now.AddMonths(-3)
                });
                Console.WriteLine($"[DB_SEED] Added Kate Williams as Member of {musicClub.ClubName}");
            }

            if (lisaThompson != null && musicClub != null)
            {
                memberships.Add(new ClubMember
                {
                    UserID = lisaThompson.UserID,
                    ClubID = musicClub.ClubID,
                    ClubRole = ClubRole.Member,
                    JoinDate = DateTime.Now.AddMonths(-2)
                });
                Console.WriteLine($"[DB_SEED] Added Lisa Thompson as Member of {musicClub.ClubName}");
            }

            if (memberships.Any())
            {
                context.ClubMembers.AddRange(memberships);
                await context.SaveChangesAsync();
                Console.WriteLine($"[DB_SEED] Created {memberships.Count} club memberships successfully.");

                var csCount = memberships.Count(m => m.ClubID == computerScienceClub?.ClubID);
                var photoCount = memberships.Count(m => m.ClubID == photographyClub?.ClubID);
                var musicCount = memberships.Count(m => m.ClubID == musicClub?.ClubID);

                Console.WriteLine($"[DB_SEED] Computer Science Club: {csCount} members, Photography Club: {photoCount} members, Music Club: {musicCount} members");
            }
        }

        private static async Task CreateAdminUsersAsync(ClubManagementDbContext context)
        {
            Console.WriteLine("[DB_ADMIN] Creating single admin user...");

            var adminEmail = "admin@university.edu";
            var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (existingAdmin == null)
            {
                Console.WriteLine($"[DB_ADMIN] Creating {adminEmail}...");
                var adminUser = new User
                {
                    FullName = "System Administrator",
                    Email = adminEmail,
                    Password = HashPassword("admin123"),
                    SystemRole = SystemRole.Admin,
                    CreatedAt = DateTime.Now
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
                Console.WriteLine($"[DB_ADMIN] Created admin user successfully: {adminEmail}");
            }
            else
            {
                Console.WriteLine($"[DB_ADMIN] Admin user already exists: {adminEmail}");
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
