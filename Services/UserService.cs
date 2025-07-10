using ClubManagementApp.Data;
using ClubManagementApp.Models;
using ClubManagementApp.Configuration;
using ClubManagementApp.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ClubManagementApp.Services
{
    /// <summary>
    /// UserService handles all user-related business logic and data operations.
    /// This service acts as the primary interface between the presentation layer and user data.
    ///
    /// Key Responsibilities:
    /// 1. User CRUD operations (Create, Read, Update, Delete)
    /// 2. Authentication and credential validation
    /// 3. User activity level tracking and calculation
    /// 4. Club membership management
    /// 5. Role-based user management
    /// 6. Participation history and statistics
    ///
    /// Data Flow:
    /// ViewModels -> UserService -> DbContext -> Database
    ///
    /// Security Features:
    /// - Password hashing using SHA256
    /// - Current user session management
    /// - Role-based access control support
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ClubManagementDbContext _context;
        private readonly ILoggingService _loggingService;
        private User? _currentUser; // Maintains the currently authenticated user session

        /// <summary>
        /// Initializes the UserService with database context and logging dependencies.
        /// The DbContext is injected via dependency injection for database operations.
        /// The ILoggingService is injected for comprehensive logging and monitoring.
        /// </summary>
        /// <param name="context">Entity Framework database context for user data access</param>
        /// <param name="loggingService">Logging service for error handling and monitoring</param>
        public UserService(ClubManagementDbContext context, ILoggingService loggingService)
        {
            _context = context;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Retrieves all users from the database with their associated club information.
        ///
        /// Data Flow:
        /// 1. Query Users table with Club navigation property
        /// 2. Order results alphabetically by full name
        /// 3. Return complete user list for administrative views
        ///
        /// Used by: Admin panels, user management screens, reporting
        /// </summary>
        /// <returns>Collection of all users with club details, ordered by name</returns>
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                _loggingService.LogInfo("Getting all users from database");
                var users = await _context.Users
                    .Include(u => u.Club) // Load related club data for display
                    .OrderBy(u => u.FullName) // Alphabetical sorting for user-friendly display
                    .ToListAsync();
                _loggingService.LogInfo($"Retrieved {users.Count} users from database");
                return users;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error retrieving all users: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a specific user by ID with complete profile information.
        ///
        /// Data Flow:
        /// 1. Query Users table by primary key
        /// 2. Include Club navigation for membership details
        /// 3. Include EventParticipations with nested Event data for activity history
        /// 4. Return full user profile or null if not found
        ///
        /// Used by: User profile views, detailed user information displays
        /// </summary>
        /// <param name="userId">Unique identifier of the user</param>
        /// <returns>Complete user profile with club and event participation data, or null</returns>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    _loggingService.LogWarning($"Invalid user ID provided: {userId}");
                    return null;
                }

                _loggingService.LogInfo($"Getting user by ID: {userId}");
                var user = await _context.Users
                    .Include(u => u.Club) // Club membership information
                    .Include(u => u.EventParticipations) // User's event participation history
                        .ThenInclude(ep => ep.Event) // Event details for each participation
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user != null)
                {
                    _loggingService.LogInfo($"Found user: {user.FullName} ({user.Email})");
                }
                else
                {
                    _loggingService.LogWarning($"User not found with ID: {userId}");
                }
                return user;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error retrieving user by ID {userId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a user by their email address for authentication and lookup purposes.
        ///
        /// Data Flow:
        /// 1. Query Users table by email (unique identifier)
        /// 2. Include Club navigation for membership context
        /// 3. Return user profile or null if email not found
        ///
        /// Used by: Login authentication, password reset, user lookup
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>User profile with club information, or null if not found</returns>
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _loggingService.LogWarning("Invalid email provided: null or empty");
                    return null;
                }

                _loggingService.LogInfo($"Getting user by email: {email}");
                var user = await _context.Users
                    .Include(u => u.Club) // Club context for authenticated user
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    _loggingService.LogInfo($"Found user: {user.FullName} (ID: {user.UserID}, Role: {user.Role})");
                }
                else
                {
                    _loggingService.LogWarning($"User not found with email: {email}");
                }
                return user;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error retrieving user by email {email}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all members of a specific club for club management operations.
        ///
        /// Data Flow:
        /// 1. Filter Users by ClubID foreign key
        /// 2. Include Club navigation for context
        /// 3. Order by name for organized display
        /// 4. Return club membership list
        ///
        /// Used by: Club management screens, member lists, club statistics
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Collection of club members with club details, ordered by name</returns>
        public async Task<IEnumerable<User>> GetUsersByClubAsync(int clubId)
        {
            try
            {
                if (clubId <= 0)
                {
                    _loggingService.LogWarning($"Invalid club ID provided: {clubId}");
                    return new List<User>();
                }

                return await _context.Users
                    .Include(u => u.Club) // Club information for context
                    .Where(u => u.ClubID == clubId) // Filter by club membership
                    .OrderBy(u => u.FullName) // Alphabetical organization
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error retrieving users by club {clubId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Searches users by name or email using partial string matching.
        ///
        /// Data Flow:
        /// 1. Apply LIKE query on FullName and Email fields
        /// 2. Include Club navigation for search result context
        /// 3. Order results alphabetically
        /// 4. Return matching users for search functionality
        ///
        /// Used by: User search features, admin lookup, member finding
        /// </summary>
        /// <param name="searchTerm">Partial name or email to search for</param>
        /// <returns>Collection of matching users with club details, ordered by name</returns>
        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    _loggingService.LogWarning("Invalid search term provided: null or empty");
                    return new List<User>();
                }

                return await _context.Users
                    .Include(u => u.Club) // Club context for search results
                    .Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm)) // Partial matching
                    .OrderBy(u => u.FullName) // Consistent ordering
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error searching users with term '{searchTerm}': {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a new user account with secure password hashing.
        ///
        /// Data Flow:
        /// 1. Receive user data from registration form
        /// 2. Hash the plain text password using SHA256
        /// 3. Add user entity to database context
        /// 4. Save changes to persist the new user
        /// 5. Return the created user with generated ID
        ///
        /// Security: Password is hashed before database storage
        /// Used by: User registration, admin user creation
        /// </summary>
        /// <param name="user">User entity with plain text password</param>
        /// <returns>Created user entity with hashed password and generated ID</returns>
        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                _loggingService.LogInfo($"Creating new user: {user.FullName} ({user.Email})");
                _loggingService.LogInfo($"User role: {user.Role}, Club ID: {user.ClubID}");

                // Input validation
                if (user == null)
                    throw new ArgumentNullException(nameof(user), "User cannot be null");

                if (string.IsNullOrWhiteSpace(user.FullName))
                    throw new ArgumentException("Full name is required", nameof(user));

                if (string.IsNullOrWhiteSpace(user.Email))
                    throw new ArgumentException("Email is required", nameof(user));

                if (string.IsNullOrWhiteSpace(user.Password))
                    throw new ArgumentException("Password is required", nameof(user));

                // Check for duplicate email
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                if (existingUser != null)
                    throw new InvalidOperationException($"User with email {user.Email} already exists");

                // SECURITY: Hash password before database storage
                // This ensures plain text passwords are never stored in the database
                user.Password = HashPassword(user.Password);
                _loggingService.LogInfo("Password hashed successfully");

                // Add user to database context and persist changes
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _loggingService.LogInfo($"User created successfully with ID: {user.UserID}");
                return user;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error creating user: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing user's information in the database.
        ///
        /// Data Flow:
        /// 1. Receive updated user entity from UI
        /// 2. Find existing entity in database
        /// 3. Update properties manually to work with tracking
        /// 4. Hash password if it's being updated
        /// 5. Save changes to update database record
        /// 6. Return updated user entity
        ///
        /// Security: Password is hashed before database storage if updated
        /// Used by: User profile editing, admin user management
        /// </summary>
        /// <param name="user">User entity with updated information</param>
        /// <returns>Updated user entity</returns>
        public async Task<User> UpdateUserAsync(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user), "User cannot be null");

                if (user.UserID <= 0)
                    throw new ArgumentException("Invalid user ID", nameof(user));

                if (string.IsNullOrWhiteSpace(user.FullName))
                    throw new ArgumentException("Full name is required", nameof(user));

                if (string.IsNullOrWhiteSpace(user.Email))
                    throw new ArgumentException("Email is required", nameof(user));

                _loggingService.LogInfo($"Updating user: {user.FullName} (ID: {user.UserID})");

                // Find the existing entity to avoid tracking conflicts
                var existingUser = await _context.Users.FindAsync(user.UserID);
                if (existingUser == null)
                {
                    throw new InvalidOperationException($"User with ID {user.UserID} not found");
                }

                // Update properties manually to avoid entity tracking issues
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.Role = user.Role;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.IsActive = user.IsActive;
                existingUser.ClubID = user.ClubID;
                existingUser.ActivityLevel = user.ActivityLevel;
                existingUser.StudentID = user.StudentID;
                existingUser.TwoFactorEnabled = user.TwoFactorEnabled;

                // Update password if it's different from the existing one (indicating a new password)
                // Only hash if it's a new password (not already hashed)
                if (!string.IsNullOrEmpty(user.Password) && user.Password != existingUser.Password)
                {
                    Console.WriteLine("[USER_SERVICE] Password update detected, hashing new password");
                    existingUser.Password = HashPassword(user.Password);
                }

                await _context.SaveChangesAsync();
                _loggingService.LogInfo($"User updated successfully: {existingUser.FullName}");
                return existingUser;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error updating user: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes a user account from the database.
        ///
        /// Data Flow:
        /// 1. Find user by ID in database
        /// 2. If user exists, remove from context
        /// 3. Save changes to delete from database
        /// 4. Return success/failure status
        ///
        /// Considerations: This is a hard delete - consider soft delete for audit trails
        /// Used by: Admin user management, account deactivation
        /// </summary>
        /// <param name="userId">Unique identifier of user to delete</param>
        /// <returns>True if user was deleted, false if user not found</returns>
        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                _loggingService.LogInfo($"Attempting to delete user with ID: {userId}");

                if (userId <= 0)
                    throw new ArgumentException("Invalid user ID", nameof(userId));

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _loggingService.LogWarning($"User not found for deletion: {userId}");
                    return false; // User not found
                }

                _loggingService.LogInfo($"Deleting user: {user.FullName} ({user.Email})");
                // Remove user entity and persist changes
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _loggingService.LogInfo($"User deleted successfully: {user.FullName}");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error deleting user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Validates user login credentials for authentication.
        ///
        /// Data Flow:
        /// 1. Look up user by email address
        /// 2. If user found, verify provided password against stored hash
        /// 3. Return authentication result
        ///
        /// Security: Uses secure password hashing verification
        /// Used by: Login process, password verification
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">Plain text password to verify</param>
        /// <returns>True if credentials are valid, false otherwise</returns>
        public async Task<bool> ValidateUserCredentialsAsync(string email, string password)
        {
            try
            {
                _loggingService.LogInfo($"Validating credentials for email: {email}");

                // Input validation
                if (string.IsNullOrWhiteSpace(email))
                    throw new ArgumentException("Email is required", nameof(email));

                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Password is required", nameof(password));

                // Look up user by email
                var user = await GetUserByEmailAsync(email);
                if (user == null)
                {
                    _loggingService.LogWarning($"User not found in database: {email}");
                    return false; // User not found
                }

                _loggingService.LogInfo($"User found: {user.FullName} (ID: {user.UserID}, Active: {user.IsActive})");

                // Check if user is active
                if (!user.IsActive)
                {
                    _loggingService.LogWarning($"User account is inactive: {email}");
                    return false;
                }

                // Verify password against stored hash
                var passwordValid = VerifyPassword(password, user.Password);
                _loggingService.LogInfo($"Password verification result: {passwordValid}");

                return passwordValid;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error validating credentials for {email}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Manually updates a user's activity level for administrative purposes.
        ///
        /// Data Flow:
        /// 1. Find user by ID in database
        /// 2. Update ActivityLevel property
        /// 3. Save changes to persist the update
        ///
        /// Used by: Admin overrides, manual activity level adjustments
        /// </summary>
        /// <param name="userId">Unique identifier of the user</param>
        /// <param name="activityLevel">New activity level to assign</param>
        public async Task UpdateActivityLevelAsync(int userId, ActivityLevel activityLevel)
        {
            try
            {
                if (userId <= 0)
                {
                    _loggingService.LogWarning($"Invalid user ID provided: {userId}");
                    return;
                }

                if (!Enum.IsDefined(typeof(ActivityLevel), activityLevel))
                {
                    _loggingService.LogWarning($"Invalid activity level provided: {activityLevel}");
                    return;
                }

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.ActivityLevel = activityLevel;
                    await _context.SaveChangesAsync();
                    _loggingService.LogInfo($"Activity level updated for user {userId}: {activityLevel}");
                }
                else
                {
                    _loggingService.LogWarning($"User not found for activity level update: {userId}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error updating activity level for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Automatically calculates and updates activity levels for users based on event attendance.
        /// This method implements the business logic for member engagement tracking.
        ///
        /// Data Flow:
        /// 1. Query active users with their event participation history
        /// 2. For each user, calculate attendance rate from participation data
        /// 3. Apply business rules to determine activity level:
        ///    - Active: >80% attendance rate
        ///    - Normal: 50-80% attendance rate
        ///    - Inactive: <50% attendance rate
        /// 4. Update all user activity levels in batch
        /// 5. Save changes to persist updates
        ///
        /// Business Logic: Activity levels drive engagement strategies and reporting
        /// Used by: Scheduled jobs, club management, engagement analysis
        /// </summary>
        /// <param name="clubId">Optional club ID to limit updates to specific club members</param>
        public async Task UpdateMemberActivityLevelsAsync(int? clubId = null)
        {
            try
            {
                if (clubId.HasValue && clubId.Value <= 0)
                {
                    _loggingService.LogWarning($"Invalid club ID provided: {clubId.Value}");
                    return;
                }

                // STEP 1: Build query for active users with participation history
                var query = _context.Users
                    .Include(u => u.EventParticipations) // Load participation records
                        .ThenInclude(ep => ep.Event) // Load event details for each participation
                    .Where(u => u.IsActive); // Only process active users

                // Filter by club if specified
                if (clubId.HasValue)
                    query = query.Where(u => u.ClubID == clubId.Value);

                var users = await query.ToListAsync();
                _loggingService.LogInfo($"Updating activity levels for {users.Count} users");

                // STEP 2: Calculate activity levels for each user
                foreach (var user in users)
                {
                    var totalEvents = user.EventParticipations.Count;
                    var attendedEvents = user.EventParticipations.Count(ep => ep.Status == AttendanceStatus.Attended);

                    // STEP 3: Apply business rules for activity level determination
                    if (totalEvents == 0)
                    {
                        // New users with no event history default to Normal
                        user.ActivityLevel = ActivityLevel.Normal;
                    }
                    else
                    {
                        // Calculate attendance percentage
                        var attendanceRate = (double)attendedEvents / totalEvents * 100;

                        // Apply tiered activity level logic
                        user.ActivityLevel = attendanceRate switch
                        {
                            > 80 => ActivityLevel.Active,    // High engagement
                            >= 50 => ActivityLevel.Normal,   // Moderate engagement
                            _ => ActivityLevel.Inactive       // Low engagement
                        };
                    }
                }

                // STEP 4: Persist all activity level updates
                await _context.SaveChangesAsync();
                _loggingService.LogInfo($"Activity levels updated successfully for {users.Count} users");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error updating member activity levels: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates activity level statistics for reporting and dashboard displays.
        ///
        /// Data Flow:
        /// 1. Query users table with optional club filtering
        /// 2. Group users by their ActivityLevel enum values
        /// 3. Count members in each activity category
        /// 4. Return dictionary mapping activity levels to member counts
        ///
        /// Used by: Dashboard widgets, club statistics, engagement reports
        /// </summary>
        /// <param name="clubId">Optional club ID to limit statistics to specific club</param>
        /// <returns>Dictionary mapping ActivityLevel to member count</returns>
        public async Task<Dictionary<ActivityLevel, int>> GetActivityStatisticsAsync(int? clubId = null)
        {
            try
            {
                if (clubId.HasValue && clubId.Value <= 0)
                {
                    _loggingService.LogWarning($"Invalid club ID provided: {clubId.Value}");
                    return new Dictionary<ActivityLevel, int>();
                }

                var query = _context.Users.AsQueryable();

                // Filter by club if specified for club-specific statistics
                if (clubId.HasValue)
                    query = query.Where(u => u.ClubID == clubId.Value);

                // Group by activity level and count members in each category
                return await query
                    .GroupBy(u => u.ActivityLevel)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error retrieving activity statistics: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves users filtered by their role for role-based management operations.
        ///
        /// Data Flow:
        /// 1. Query users with specified role and active status
        /// 2. Include club navigation for context
        /// 3. Apply optional club filtering
        /// 4. Order results alphabetically
        /// 5. Return role-specific user list
        ///
        /// Used by: Leadership management, role assignment, organizational charts
        /// </summary>
        /// <param name="role">User role to filter by (Chairman, ViceChairman, etc.)</param>
        /// <param name="clubId">Optional club ID to limit results to specific club</param>
        /// <returns>Collection of active users with specified role, ordered by name</returns>
        public async Task<IEnumerable<User>> GetMembersByRoleAsync(UserRole role, int? clubId = null)
        {
            try
            {
                if (!Enum.IsDefined(typeof(UserRole), role))
                {
                    _loggingService.LogWarning($"Invalid role provided: {role}");
                    return new List<User>();
                }

                if (clubId.HasValue && clubId.Value <= 0)
                {
                    _loggingService.LogWarning($"Invalid club ID provided: {clubId.Value}");
                    return new List<User>();
                }

                var query = _context.Users
                    .Include(u => u.Club) // Club context for role management
                    .Where(u => u.Role == role && u.IsActive); // Filter by role and active status

                // Apply club filtering if specified
                if (clubId.HasValue)
                    query = query.Where(u => u.ClubID == clubId.Value);

                return await query.OrderBy(u => u.FullName).ToListAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error retrieving members by role {role}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates comprehensive participation history and statistics for a specific user.
        /// This method provides detailed analytics for user engagement tracking.
        ///
        /// Data Flow:
        /// 1. Query user with complete event participation history
        /// 2. Calculate participation statistics (totals, rates, trends)
        /// 3. Generate recent events summary for quick overview
        /// 4. Return structured data for UI display and reporting
        ///
        /// Analytics Provided:
        /// - Total events participated in
        /// - Attendance vs registration rates
        /// - Recent event activity summary
        /// - Engagement trends over time
        ///
        /// Used by: User profile pages, performance reviews, engagement analysis
        /// </summary>
        /// <param name="userId">Unique identifier of the user</param>
        /// <returns>Dictionary containing participation statistics and recent events data</returns>
        public async Task<Dictionary<string, object>> GetMemberParticipationHistoryAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    _loggingService.LogWarning($"Invalid user ID provided: {userId}");
                    return new Dictionary<string, object>();
                }

                // Load user with complete participation history
                var user = await _context.Users
                    .Include(u => u.EventParticipations) // All participation records
                        .ThenInclude(ep => ep.Event) // Event details for each participation
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null)
                {
                    _loggingService.LogWarning($"User not found for participation history: {userId}");
                    return new Dictionary<string, object>(); // Return empty if user not found
                }

                // Process participation data for analytics
                var participations = user.EventParticipations.ToList();
                var statistics = CalculateParticipationStatistics(participations);
                var recentEvents = GetRecentEventsSummary(participations);

                // Return structured analytics data for UI consumption
                return new Dictionary<string, object>
                {
                    ["TotalEvents"] = statistics.TotalEvents,
                    ["AttendedEvents"] = statistics.AttendedEvents,
                    ["RegisteredEvents"] = statistics.RegisteredEvents,
                    ["AbsentEvents"] = statistics.AbsentEvents,
                    ["AttendanceRate"] = statistics.AttendanceRate,
                    ["RecentEvents"] = recentEvents
                };
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error retrieving participation history for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Assigns a user to a specific club, establishing club membership.
        ///
        /// Data Flow:
        /// 1. Find user by ID in database
        /// 2. Update user's ClubID foreign key
        /// 3. Save changes to establish membership
        /// 4. Return success/failure status
        ///
        /// Used by: Club enrollment, membership management, user transfers
        /// </summary>
        /// <param name="userId">Unique identifier of the user</param>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>True if assignment successful, false if user not found</returns>
        public async Task<bool> AssignUserToClubAsync(int userId, int clubId)
        {
            try
            {
                if (userId <= 0)
                {
                    _loggingService.LogWarning($"Invalid user ID provided: {userId}");
                    return false;
                }

                if (clubId <= 0)
                {
                    _loggingService.LogWarning($"Invalid club ID provided: {clubId}");
                    return false;
                }

                _loggingService.LogInfo($"Assigning user {userId} to club {clubId}");
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _loggingService.LogWarning($"User not found for club assignment: {userId}");
                    return false; // User not found
                }

                _loggingService.LogInfo($"Assigning {user.FullName} to club {clubId}");
                // Establish club membership by setting foreign key
                user.ClubID = clubId;
                await _context.SaveChangesAsync();
                _loggingService.LogInfo($"User {user.FullName} successfully assigned to club {clubId}");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error assigning user {userId} to club {clubId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes a user from their current club, ending club membership.
        ///
        /// Data Flow:
        /// 1. Find user by ID in database
        /// 2. Clear user's ClubID foreign key (set to null)
        /// 3. Save changes to remove membership
        /// 4. Return success/failure status
        ///
        /// Used by: Club departures, membership termination, user transfers
        /// </summary>
        /// <param name="userId">Unique identifier of the user</param>
        /// <returns>True if removal successful, false if user not found</returns>
        public async Task<bool> RemoveUserFromClubAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    await _loggingService.LogWarningAsync("Invalid userId provided to RemoveUserFromClubAsync");
                    return false;
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    await _loggingService.LogWarningAsync($"User not found for club removal: {userId}");
                    return false; // User not found
                }

                // Remove club membership by clearing foreign key
                user.ClubID = null;
                await _context.SaveChangesAsync();
                await _loggingService.LogInformationAsync($"User {userId} removed from club successfully");
                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error removing user {userId} from club: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates a user's role within the organization or club.
        ///
        /// Data Flow:
        /// 1. Find user by ID in database
        /// 2. Update user's Role property
        /// 3. Save changes to apply new role
        /// 4. Return success/failure status
        ///
        /// Used by: Role promotions, leadership changes, organizational restructuring
        /// </summary>
        /// <param name="userId">Unique identifier of the user</param>
        /// <param name="newRole">New role to assign to the user</param>
        /// <returns>True if role update successful, false if user not found</returns>
        public async Task<bool> UpdateUserRoleAsync(int userId, UserRole newRole)
        {
            try
            {
                if (userId <= 0)
                {
                    await _loggingService.LogWarningAsync("Invalid userId provided to UpdateUserRoleAsync");
                    return false;
                }

                if (!Enum.IsDefined(typeof(UserRole), newRole))
                {
                    await _loggingService.LogWarningAsync($"Invalid role provided to UpdateUserRoleAsync: {newRole}");
                    return false;
                }

                Console.WriteLine($"[USER_SERVICE] Updating role for user {userId} to {newRole}");
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    Console.WriteLine($"[USER_SERVICE] User not found for role update: {userId}");
                    await _loggingService.LogWarningAsync($"User not found for role update: {userId}");
                    return false; // User not found
                }

                var oldRole = user.Role;
                Console.WriteLine($"[USER_SERVICE] Changing {user.FullName} role from {oldRole} to {newRole}");
                // Update user's organizational role
                user.Role = newRole;
                await _context.SaveChangesAsync();
                Console.WriteLine($"[USER_SERVICE] Role updated successfully for {user.FullName}: {oldRole} -> {newRole}");
                await _loggingService.LogInformationAsync($"User {userId} role updated from {oldRole} to {newRole}");
                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error updating role for user {userId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves the leadership team for a specific club.
        ///
        /// Data Flow:
        /// 1. Query users belonging to specified club
        /// 2. Filter by leadership roles (Chairman, ViceChairman, TeamLeader)
        /// 3. Include club navigation for context
        /// 4. Order by role hierarchy, then by name
        /// 5. Return organized leadership list
        ///
        /// Used by: Organizational charts, leadership directories, club management
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Collection of club leaders ordered by role hierarchy and name</returns>
        public async Task<IEnumerable<User>> GetClubLeadershipAsync(int clubId)
        {
            try
            {
                if (clubId <= 0)
                {
                    await _loggingService.LogWarningAsync("Invalid clubId provided to GetClubLeadershipAsync");
                    return new List<User>();
                }

                return await _context.Users
                    .Include(u => u.Club) // Club context for leadership display
                    .Where(u => u.ClubID == clubId &&
                               (u.Role == UserRole.Chairman ||
                                u.Role == UserRole.ViceChairman ||
                                u.Role == UserRole.TeamLeader)) // Filter leadership roles
                    .OrderBy(u => u.Role) // Order by role hierarchy
                    .ThenBy(u => u.FullName) // Then alphabetically by name
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error retrieving club leadership for club {clubId}: {ex.Message}");
                return new List<User>();
            }
        }

        /// <summary>
        /// Securely hashes a plain text password using SHA256 algorithm.
        ///
        /// Security Implementation:
        /// 1. Convert password string to UTF-8 bytes
        /// 2. Apply SHA256 cryptographic hash function
        /// 3. Convert hash bytes to Base64 string for storage
        ///
        /// Note: In production, consider using BCrypt or Argon2 for better security
        /// </summary>
        /// <param name="password">Plain text password to hash</param>
        /// <returns>Base64-encoded SHA256 hash of the password</returns>
        private string HashPassword(string password)
        {
            try
            {
                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));
                }

                using var sha256 = SHA256.Create();
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
            catch (Exception ex)
            {
                _loggingService.LogErrorAsync($"Error hashing password: {ex.Message}").Wait();
                throw;
            }
        }

        /// <summary>
        /// Verifies a plain text password against a stored hash.
        ///
        /// Verification Process:
        /// 1. Hash the provided password using same algorithm
        /// 2. Compare the new hash with the stored hash
        /// 3. Return true if hashes match (password is correct)
        ///
        /// Used by: Authentication, password validation
        /// </summary>
        /// <param name="password">Plain text password to verify</param>
        /// <param name="hashedPassword">Stored password hash to compare against</param>
        /// <returns>True if password matches the hash, false otherwise</returns>
        private bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                {
                    return false;
                }

                var hashedInput = HashPassword(password);
                Console.WriteLine($"[HASH] Input password hash: {hashedInput}");
                Console.WriteLine($"[HASH] Stored password hash: {hashedPassword}");
                Console.WriteLine($"[HASH] Hash comparison result: {hashedInput == hashedPassword}");
                return hashedInput == hashedPassword;
            }
            catch (Exception ex)
            {
                _loggingService.LogErrorAsync($"Error verifying password: {ex.Message}").Wait();
                return false;
            }
        }

        /// <summary>
        /// Calculates comprehensive participation statistics from event participation data.
        ///
        /// Analytics Calculation:
        /// 1. Count total events user participated in
        /// 2. Count events by attendance status (Attended, Registered, Absent)
        /// 3. Calculate attendance rate percentage
        /// 4. Return structured statistics object
        ///
        /// Used by: User analytics, engagement reporting, performance tracking
        /// </summary>
        /// <param name="participations">List of user's event participation records</param>
        /// <returns>Structured statistics object with participation metrics</returns>
        private ParticipationStatistics CalculateParticipationStatistics(List<EventParticipant> participations)
        {
            try
            {
                if (participations == null)
                {
                    return new ParticipationStatistics
                    {
                        TotalEvents = 0,
                        AttendedEvents = 0,
                        RegisteredEvents = 0,
                        AbsentEvents = 0,
                        AttendanceRate = 0
                    };
                }

                var totalEvents = participations.Count;
                var attendedEvents = participations.Count(p => p.Status == AttendanceStatus.Attended);
                var registeredEvents = participations.Count(p => p.Status == AttendanceStatus.Registered);
                var absentEvents = participations.Count(p => p.Status == AttendanceStatus.Absent);

                return new ParticipationStatistics
                {
                    TotalEvents = totalEvents,
                    AttendedEvents = attendedEvents,
                    RegisteredEvents = registeredEvents,
                    AbsentEvents = absentEvents,
                    AttendanceRate = totalEvents > 0 ? (double)attendedEvents / totalEvents * 100 : 0
                };
            }
            catch (Exception ex)
            {
                _loggingService.LogErrorAsync($"Error calculating participation statistics: {ex.Message}").Wait();
                return new ParticipationStatistics
                {
                    TotalEvents = 0,
                    AttendedEvents = 0,
                    RegisteredEvents = 0,
                    AbsentEvents = 0,
                    AttendanceRate = 0
                };
            }
        }

        /// <summary>
        /// Generates a summary of recent events for quick user activity overview.
        ///
        /// Data Processing:
        /// 1. Sort participations by event date (most recent first)
        /// 2. Take configured number of recent events
        /// 3. Project to anonymous object with essential event details
        /// 4. Return formatted summary for UI display
        ///
        /// Used by: User dashboards, activity summaries, quick overviews
        /// </summary>
        /// <param name="participations">List of user's event participation records</param>
        /// <returns>Anonymous object collection with recent event summaries</returns>
        private object GetRecentEventsSummary(List<EventParticipant> participations)
        {
            try
            {
                if (participations == null)
                {
                    return new List<object>();
                }

                return participations
                    .Where(p => p.Event != null) // Ensure event is not null
                    .OrderByDescending(p => p.Event.EventDate) // Most recent events first
                    .Take(ServiceConfiguration.Users.MaxRecentEventsCount) // Limit to configured count
                    .Select(p => new
                    {
                        p.Event.Name,
                        p.Event.EventDate,
                        p.Event.Location,
                        Status = p.Status.ToString(),
                        p.RegistrationDate,
                        p.AttendanceDate
                    });
            }
            catch (Exception ex)
            {
                _loggingService.LogErrorAsync($"Error generating recent events summary: {ex.Message}").Wait();
                return new List<object>();
            }
        }

        /// <summary>
        /// Retrieves the currently authenticated user from the session.
        ///
        /// Session Management:
        /// - Returns the user stored in _currentUser field
        /// - Used throughout the application to get current user context
        /// - Returns null if no user is currently logged in
        ///
        /// Used by: Authorization checks, user context, personalization
        /// </summary>
        /// <returns>Current authenticated user or null if not logged in</returns>
        public Task<User?> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }

        /// <summary>
        /// Sets the current authenticated user for the session.
        ///
        /// Session Management:
        /// - Stores user in _currentUser field for application-wide access
        /// - Called during login to establish user session
        /// - Can be set to null during logout to clear session
        ///
        /// Used by: Login process, session management, user switching
        /// </summary>
        /// <param name="user">User to set as current, or null to clear session</param>
        public void SetCurrentUser(User? user)
        {
            _currentUser = user;
        }

        /// <summary>
        /// Gets the total count of users in the database efficiently.
        ///
        /// Performance:
        /// - Uses COUNT query instead of loading all records
        /// - Optimized for dashboard statistics display
        ///
        /// Used by: Dashboard statistics, admin overview
        /// </summary>
        /// <returns>Total number of users in the system</returns>
        public async Task<int> GetTotalUsersCountAsync()
        {
            try
            {
                return await _context.Users.CountAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting total users count: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the count of new members who joined this month.
        ///
        /// Performance:
        /// - Uses COUNT query with date filtering
        /// - Optimized for dashboard statistics display
        ///
        /// Used by: Dashboard statistics, monthly reports
        /// </summary>
        /// <returns>Number of users who joined this month</returns>
        public async Task<int> GetNewMembersThisMonthCountAsync()
        {
            try
            {
                var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                return await _context.Users
                    .Where(u => u.JoinDate >= startOfMonth)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting new members this month count: {ex.Message}", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Data transfer object for user participation statistics and analytics.
    /// This class encapsulates calculated metrics about a user's event participation behavior.
    ///
    /// Purpose:
    /// - Provides structured data for participation analytics
    /// - Enables consistent statistics calculation across the application
    /// - Supports dashboard widgets and reporting features
    ///
    /// Usage:
    /// - Returned by CalculateParticipationStatistics method
    /// - Used in user profile displays and engagement reports
    /// - Feeds into activity level calculation algorithms
    /// </summary>
    public class ParticipationStatistics
    {
        /// <summary>Total number of events the user has participated in (any status)</summary>
        public int TotalEvents { get; set; }

        /// <summary>Number of events the user actually attended</summary>
        public int AttendedEvents { get; set; }

        /// <summary>Number of events the user registered for but status is still pending</summary>
        public int RegisteredEvents { get; set; }

        /// <summary>Number of events the user was registered for but did not attend</summary>
        public int AbsentEvents { get; set; }

        /// <summary>Percentage of attended events out of total events (0-100)</summary>
        public double AttendanceRate { get; set; }
    }
}
