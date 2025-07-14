using ClubManagementApp.Data;
using ClubManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementApp.Services
{
    /// <summary>
    /// Business service for managing club operations and organizational structure.
    /// Handles club CRUD operations, membership management, leadership roles, and analytics.
    ///
    /// Responsibilities:
    /// - Club lifecycle management (create, read, update, delete)
    /// - Member management and role assignments
    /// - Leadership hierarchy and permissions
    /// - Club statistics and reporting
    /// - Organizational structure maintenance
    ///
    /// Data Flow:
    /// ViewModels -> ClubService -> DbContext -> Database
    ///
    /// Key Features:
    /// - Hierarchical role management (Chairman, ViceChairman, TeamLeader, Member)
    /// - Automatic leadership succession (Chairman demotion to ViceChairman)
    /// - Role-based permission configuration
    /// - Comprehensive club analytics and statistics
    /// - Member activity tracking and engagement metrics
    /// </summary>
    public class ClubService : IClubService
    {
        /// <summary>Database context for club and user data operations</summary>
        private readonly ClubManagementDbContext _context;

        /// <summary>
        /// Initializes the ClubService with database context dependency.
        ///
        /// Data Flow:
        /// - Dependency injection provides DbContext instance
        /// - Service becomes ready for club management operations
        /// - All database operations flow through this context
        /// </summary>
        /// <param name="context">Entity Framework database context for data access</param>
        public ClubService(ClubManagementDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all clubs with their complete organizational data.
        /// Includes member lists and event histories for comprehensive club information.
        ///
        /// Data Flow:
        /// 1. Query database for all clubs
        /// 2. Include related Members and Events via Entity Framework navigation
        /// 3. Sort alphabetically by club name for consistent ordering
        /// 4. Return complete club objects with relationships
        ///
        /// Usage: Club directory displays, administrative overviews, reporting dashboards
        /// </summary>
        /// <returns>Collection of all clubs with members and events included</returns>
        public async Task<IEnumerable<Club>> GetAllClubsAsync()
        {
            try
            {
                Console.WriteLine("[CLUB_SERVICE] Getting all clubs");
                var clubs = await _context.Clubs
                    .Include(c => c.ClubMembers.Where(cm => cm.IsActive))
                        .ThenInclude(cm => cm.User)
                    .Include(c => c.Events)
                    .ToListAsync();

                Console.WriteLine($"[CLUB_SERVICE] Found {clubs.Count} clubs");
                return clubs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting clubs: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a specific club by its unique identifier.
        /// Includes complete member roster and event history for detailed club view.
        ///
        /// Data Flow:
        /// 1. Query database for club with specified ID
        /// 2. Include related Members and Events collections
        /// 3. Return club object or null if not found
        ///
        /// Usage: Club detail pages, member management, event planning
        /// </summary>
        /// <param name="clubId">Unique identifier of the club to retrieve</param>
        /// <returns>Club object with relationships, or null if not found</returns>
        public async Task<Club?> GetClubByIdAsync(int clubId)
        {
            try
            {
                if (clubId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID: {clubId}");
                    return null;
                }

                Console.WriteLine($"[CLUB_SERVICE] Getting club by ID: {clubId}");
                var club = await _context.Clubs
                    .Include(c => c.ClubMembers.Where(cm => cm.IsActive))
                        .ThenInclude(cm => cm.User)
                    .Include(c => c.Events)
                    .FirstOrDefaultAsync(c => c.ClubID == clubId);

                if (club != null)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Found club: {club.Name} with {club.ClubMembers?.Count ?? 0} members");
                }
                else
                {
                    Console.WriteLine($"[CLUB_SERVICE] Club not found with ID: {clubId}");
                }
                return club;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting club by ID {clubId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a club by its name for lookup and validation purposes.
        /// Includes member and event data for complete club information.
        ///
        /// Data Flow:
        /// 1. Query database for club with exact name match
        /// 2. Include related Members and Events collections
        /// 3. Return club object or null if not found
        ///
        /// Usage: Club name validation, search functionality, duplicate prevention
        /// </summary>
        /// <param name="name">Exact name of the club to find</param>
        /// <returns>Club object with relationships, or null if not found</returns>
        public async Task<Club?> GetClubByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine("[CLUB_SERVICE] Club name cannot be null or empty");
                    return null;
                }

                return await _context.Clubs
                    .Include(c => c.ClubMembers.Where(cm => cm.IsActive))
                        .ThenInclude(cm => cm.User)
                    .Include(c => c.Events)
                    .FirstOrDefaultAsync(c => c.Name == name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting club by name '{name}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a new club in the system with initial organizational structure.
        /// Establishes the foundation for member management and event planning.
        ///
        /// Data Flow:
        /// 1. Receive club object from ViewModel/UI layer
        /// 2. Add club entity to DbContext tracking
        /// 3. Persist changes to database
        /// 4. Return created club with generated ID
        ///
        /// Business Logic:
        /// - Assigns unique ClubID via database auto-increment
        /// - Sets creation timestamp for audit trail
        /// - Initializes empty member and event collections
        ///
        /// Usage: Club registration forms, administrative club creation
        /// </summary>
        /// <param name="club">Club object containing name, description, and initial settings</param>
        /// <returns>Created club object with assigned ID and timestamps</returns>
        public async Task<Club> CreateClubAsync(Club club)
        {
            try
            {
                Console.WriteLine($"[CLUB_SERVICE] Creating new club: {club.Name}");
                Console.WriteLine($"[CLUB_SERVICE] Club description: {club.Description}");

                // Input validation
                if (club == null)
                    throw new ArgumentNullException(nameof(club), "Club cannot be null");

                if (string.IsNullOrWhiteSpace(club.Name))
                    throw new ArgumentException("Club name is required", nameof(club));

                // Check for duplicate club name
                var existingClub = await _context.Clubs.FirstOrDefaultAsync(c => c.Name == club.Name);
                if (existingClub != null)
                    throw new InvalidOperationException($"Club with name '{club.Name}' already exists");

                // Ensure ClubID is 0 for new entities
                club.ClubID = 0;

                _context.Clubs.Add(club);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[CLUB_SERVICE] Club created successfully with ID: {club.ClubID}");
                return club;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error creating club: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates existing club information and organizational settings.
        /// Preserves member relationships while allowing club metadata changes.
        ///
        /// Data Flow:
        /// 1. Receive updated club object from ViewModel
        /// 2. Mark entity as modified in DbContext
        /// 3. Persist changes to database
        /// 4. Return updated club object
        ///
        /// Business Logic:
        /// - Maintains existing member and event relationships
        /// - Updates club metadata (name, description, settings)
        /// - Preserves role configurations and permissions
        ///
        /// Usage: Club settings pages, administrative updates, role configuration
        /// </summary>
        /// <param name="club">Club object with updated information</param>
        /// <returns>Updated club object reflecting database changes</returns>
        public async Task<Club> UpdateClubAsync(Club club)
        {
            try
            {
                Console.WriteLine($"[CLUB_SERVICE] Updating club: {club.Name} (ID: {club.ClubID})");

                // Input validation
                if (club == null)
                    throw new ArgumentNullException(nameof(club), "Club cannot be null");

                if (club.ClubID <= 0)
                    throw new ArgumentException("Invalid club ID", nameof(club));

                if (string.IsNullOrWhiteSpace(club.Name))
                    throw new ArgumentException("Club name is required", nameof(club));

                // Check if club exists and update properties
                var existingClub = await _context.Clubs.FindAsync(club.ClubID);
                if (existingClub == null)
                    throw new InvalidOperationException($"Club with ID {club.ClubID} not found");

                // Update properties instead of replacing the entity
                existingClub.ClubName = club.ClubName;
                existingClub.Description = club.Description;
                existingClub.IsActive = club.IsActive;

                await _context.SaveChangesAsync();
                Console.WriteLine($"[CLUB_SERVICE] Club updated successfully: {existingClub.Name}");
                return existingClub;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error updating club: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Removes a club from the system along with all organizational data.
        /// Handles cascading deletion of related member assignments and events.
        ///
        /// Data Flow:
        /// 1. Query database for club by ID
        /// 2. Validate club exists before deletion
        /// 3. Remove club entity (cascades to related data)
        /// 4. Persist changes and return success status
        ///
        /// Business Logic:
        /// - Validates club existence before deletion
        /// - Cascades deletion to member assignments and events
        /// - Maintains data integrity through foreign key constraints
        ///
        /// Usage: Administrative club dissolution, cleanup operations
        /// </summary>
        /// <param name="clubId">Unique identifier of club to delete</param>
        /// <returns>True if deletion successful, false if club not found</returns>
        public async Task<bool> DeleteClubAsync(int clubId)
        {
            try
            {
                Console.WriteLine($"[CLUB_SERVICE] Attempting to delete club with ID: {clubId}");

                if (clubId <= 0)
                    throw new ArgumentException("Invalid club ID", nameof(clubId));

                var club = await _context.Clubs
                    .Include(c => c.ClubMembers)
                    .Include(c => c.Events)
                    .FirstOrDefaultAsync(c => c.ClubID == clubId);

                if (club == null)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Club not found for deletion: {clubId}");
                    return false;
                }

                // Check if club has members or events
                if (club.ClubMembers?.Any() == true)
                    throw new InvalidOperationException($"Cannot delete club '{club.Name}' because it has {club.ClubMembers.Count} members");

                if (club.Events?.Any() == true)
                    throw new InvalidOperationException($"Cannot delete club '{club.Name}' because it has {club.Events.Count} events");

                Console.WriteLine($"[CLUB_SERVICE] Deleting club: {club.Name}");
                _context.Clubs.Remove(club);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[CLUB_SERVICE] Club deleted successfully: {club.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error deleting club {clubId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the current active member count for a specific club.
        /// Provides real-time membership statistics for dashboards and reporting.
        ///
        /// Data Flow:
        /// 1. Query Users table filtered by club ID and active status
        /// 2. Count matching records using efficient database aggregation
        /// 3. Return integer count for display
        ///
        /// Business Logic:
        /// - Only counts active members (IsActive = true)
        /// - Excludes deactivated or suspended members
        /// - Provides accurate membership metrics
        ///
        /// Usage: Dashboard widgets, membership reports, capacity planning
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Number of active members in the club</returns>
        public async Task<int> GetMemberCountAsync(int clubId)
        {
            try
            {
                if (clubId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID: {clubId}");
                    return 0;
                }

                Console.WriteLine($"[CLUB_SERVICE] Getting member count for club: {clubId}");
                var count = await _context.ClubMembers
                    .CountAsync(cm => cm.ClubID == clubId && cm.IsActive);
                Console.WriteLine($"[CLUB_SERVICE] Club {clubId} has {count} active members");
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting member count for club {clubId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all active members of a specific club.
        /// Provides complete member roster for management and communication.
        ///
        /// Data Flow:
        /// 1. Query ClubMembers table filtered by club ID and active status
        /// 2. Include User navigation property for complete member information
        /// 3. Sort members alphabetically by full name
        /// 4. Return ordered collection of ClubMember objects
        ///
        /// Business Logic:
        /// - Only includes active members (IsActive = true)
        /// - Alphabetical sorting for consistent user experience
        /// - Complete ClubMember objects with role and user information
        ///
        /// Usage: Member directories, communication lists, role assignment interfaces
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Ordered collection of active club members</returns>
        public async Task<IEnumerable<ClubMember>> GetClubMembersAsync(int clubId)
        {
            try
            {
                if (clubId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID: {clubId}");
                    return new List<ClubMember>();
                }

                Console.WriteLine($"[CLUB_SERVICE] Getting members for club: {clubId}");
                var members = await _context.ClubMembers
                    .Include(cm => cm.User)
                    .Where(cm => cm.ClubID == clubId && cm.IsActive)
                    .OrderBy(cm => cm.User.FullName)
                    .ToListAsync();
                Console.WriteLine($"[CLUB_SERVICE] Retrieved {members.Count} members for club {clubId}");
                return members;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting members for club {clubId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Assigns leadership roles within a club's organizational hierarchy.
        /// Implements automatic succession logic for Chairman transitions.
        ///
        /// Data Flow:
        /// 1. Validate role is a leadership position (Chairman, ViceChairman, TeamLeader)
        /// 2. Verify user exists and belongs to the specified club
        /// 3. Handle Chairman succession (demote current Chairman to ViceChairman)
        /// 4. Assign new role to user and persist changes
        ///
        /// Business Logic:
        /// - Only allows assignment of leadership roles
        /// - Enforces single Chairman rule with automatic demotion
        /// - Maintains organizational hierarchy integrity
        /// - Validates user membership before role assignment
        ///
        /// Leadership Hierarchy:
        /// Chairman (1) -> ViceChairman (multiple) -> TeamLeader (multiple) -> Member
        ///
        /// Usage: Role management interfaces, succession planning, organizational restructuring
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <param name="userId">Unique identifier of the user to promote</param>
        /// <param name="role">Leadership role to assign (Chairman, ViceChairman, or TeamLeader)</param>
        /// <returns>True if assignment successful, false if validation fails</returns>
        /// <summary>
        /// Assigns a club role to a user within a specific club.
        /// Creates or updates ClubMember record with the specified role.
        ///
        /// Data Flow:
        /// 1. Validate club and user exist
        /// 2. Check if user is already a member of the club
        /// 3. Create or update ClubMember record with new role
        /// 4. Handle Chairman succession if necessary
        ///
        /// Business Logic:
        /// - Enforces single Chairman rule per club
        /// - Creates ClubMember record if user not already in club
        /// - Updates existing ClubMember role if user already in club
        ///
        /// Usage: Role assignment interfaces, leadership management
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <param name="userId">Unique identifier of the user</param>
        /// <param name="role">Club role to assign</param>
        /// <returns>True if assignment successful, false otherwise</returns>
        public async Task<bool> AssignClubRoleAsync(int clubId, int userId, ClubRole role)
        {
            try
            {
                if (clubId <= 0 || userId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID ({clubId}) or user ID ({userId})");
                    return false;
                }

                // Verify club and user exist
                var club = await _context.Clubs.FindAsync(clubId);
                var user = await _context.Users.FindAsync(userId);

                if (club == null || user == null)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Club or user not found. Club: {club != null}, User: {user != null}");
                    return false;
                }

                // Handle Chairman succession - demote current Chairman if assigning new one
                if (role == ClubRole.Chairman)
                {
                    var currentChairman = await _context.ClubMembers
                        .FirstOrDefaultAsync(cm => cm.ClubID == clubId && cm.ClubRole == ClubRole.Chairman && cm.IsActive);

                    if (currentChairman != null && currentChairman.UserID != userId)
                    {
                        currentChairman.ClubRole = ClubRole.Admin;
                        Console.WriteLine($"[CLUB_SERVICE] Demoted current Chairman (User {currentChairman.UserID}) to Admin");
                    }
                }

                // Check if user is already a member
                var existingMembership = await _context.ClubMembers
                    .FirstOrDefaultAsync(cm => cm.ClubID == clubId && cm.UserID == userId);

                if (existingMembership != null)
                {
                    // Update existing membership
                    existingMembership.ClubRole = role;
                    existingMembership.IsActive = true;
                    Console.WriteLine($"[CLUB_SERVICE] Updated existing membership for User {userId} in Club {clubId} to role {role}");
                }
                else
                {
                    // Create new membership
                    var newMembership = new ClubMember
                    {
                        ClubID = clubId,
                        UserID = userId,
                        ClubRole = role,
                        JoinDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.ClubMembers.Add(newMembership);
                    Console.WriteLine($"[CLUB_SERVICE] Created new membership for User {userId} in Club {clubId} with role {role}");
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error assigning club role: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AssignClubLeadershipAsync(int clubId, int userId, SystemRole role)
        {
            try
            {
                // Map SystemRole to ClubRole for leadership positions
                ClubRole clubRole = role switch
                {
                    SystemRole.Admin => ClubRole.Admin,
                    SystemRole.ClubOwner => ClubRole.Chairman,
                    _ => ClubRole.Member // Default to member for other roles
                };

                // Use existing AssignClubRoleAsync method
                return await AssignClubRoleAsync(clubId, userId, clubRole);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error assigning club leadership: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Configures role-based permissions for club leadership positions.
        /// Stores permission mappings for different organizational roles.
        ///
        /// Data Flow:
        /// 1. Validate club exists in database
        /// 2. Serialize role permissions to JSON format
        /// 3. Embed configuration in club description field
        /// 4. Update club record with new permission settings
        ///
        /// Business Logic:
        /// - Maps SystemRole enum to list of permission strings
        /// - Stores configuration as embedded JSON in club description
        /// - Enables role-based access control throughout application
        /// - Supports customizable permission schemes per club
        ///
        /// Permission Examples:
        /// - Chairman: ["ManageMembers", "CreateEvents", "EditClub", "AssignRoles"]
        /// - ViceChairman: ["CreateEvents", "ManageEvents", "ViewReports"]
        /// - TeamLeader: ["CreateEvents", "ManageTeamEvents"]
        ///
        /// Usage: Administrative role configuration, permission management interfaces
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <param name="rolePermissions">Dictionary mapping roles to their permission lists</param>
        /// <returns>True if configuration successful, false if club not found</returns>
        [Obsolete("This method is deprecated. Use new role configuration system with ClubRole instead.")]
        public Task<bool> ConfigureLeadershipRolesAsync(int clubId, Dictionary<SystemRole, List<string>> rolePermissions)
        {
            // This method is deprecated - use new role configuration system with ClubRole instead
            return Task.FromResult(false);
        }

        /// <summary>
        /// Retrieves role-based permission configuration for a club.
        /// Extracts and deserializes permission mappings from club settings.
        ///
        /// Data Flow:
        /// 1. Retrieve club record from database
        /// 2. Extract JSON configuration from description field
        /// 3. Parse and deserialize role permission mappings
        /// 4. Return structured permission dictionary
        ///
        /// Business Logic:
        /// - Parses embedded JSON configuration from club description
        /// - Handles malformed or missing configuration gracefully
        /// - Returns empty dictionary if no configuration found
        /// - Supports role-based access control decisions
        ///
        /// Error Handling:
        /// - Returns empty dictionary for missing clubs
        /// - Gracefully handles JSON parsing errors
        /// - Provides safe fallback for permission checks
        ///
        /// Usage: Permission validation, role management interfaces, access control
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Dictionary mapping roles to their permission lists</returns>
        [Obsolete("This method is deprecated. Use new role configuration system with ClubRole instead.")]
        public Task<Dictionary<SystemRole, List<string>>> GetLeadershipRolePermissionsAsync(int clubId)
        {
            // This method is deprecated - use new role configuration system with ClubRole instead
            return Task.FromResult(new Dictionary<SystemRole, List<string>>());
        }

        /// <summary>
        /// Retrieves the current Chairman of a specific club.
        /// Returns the single active user holding the highest leadership position.
        ///
        /// Data Flow:
        /// 1. Query Users table for club members with Chairman role
        /// 2. Filter for active users only
        /// 3. Return single Chairman or null if position vacant
        ///
        /// Business Logic:
        /// - Enforces single Chairman rule per club
        /// - Only returns active users (excludes suspended/deactivated)
        /// - Provides null result for vacant Chairman position
        ///
        /// Usage: Leadership displays, contact information, succession planning
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Chairman user object, or null if position vacant</returns>
        public async Task<ClubMember?> GetClubChairmanAsync(int clubId)
        {
            try
            {
                if (clubId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID: {clubId}");
                    return null;
                }

                return await _context.ClubMembers
                    .Include(cm => cm.User)
                    .FirstOrDefaultAsync(cm => cm.ClubID == clubId && cm.ClubRole == ClubRole.Chairman && cm.IsActive);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting club chairman for club {clubId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all Vice Chairmen of a specific club.
        /// Returns multiple users holding secondary leadership positions.
        ///
        /// Data Flow:
        /// 1. Query Users table for club members with ViceChairman role
        /// 2. Filter for active users only
        /// 3. Sort alphabetically by full name
        /// 4. Return ordered collection of Vice Chairmen
        ///
        /// Business Logic:
        /// - Supports multiple Vice Chairmen per club
        /// - Only includes active users
        /// - Alphabetical ordering for consistent display
        ///
        /// Usage: Leadership directories, delegation assignments, committee formation
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Ordered collection of Vice Chairman users</returns>
        public async Task<IEnumerable<ClubMember>> GetClubAdminsAsync(int clubId)
        {
            try
            {
                if (clubId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID: {clubId}");
                    return new List<ClubMember>();
                }

                return await _context.ClubMembers
                    .Include(cm => cm.User)
                    .Where(cm => cm.ClubID == clubId && cm.ClubRole == ClubRole.Admin && cm.IsActive)
                    .OrderBy(cm => cm.User.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting club admins for club {clubId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all Team Leaders of a specific club.
        /// Returns users responsible for specific teams or functional areas.
        ///
        /// Data Flow:
        /// 1. Query Users table for club members with TeamLeader role
        /// 2. Filter for active users only
        /// 3. Sort alphabetically by full name
        /// 4. Return ordered collection of Team Leaders
        ///
        /// Business Logic:
        /// - Supports multiple Team Leaders per club
        /// - Only includes active users
        /// - Alphabetical ordering for consistent display
        ///
        /// Usage: Team management, project assignments, specialized leadership roles
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Ordered collection of Team Leader users</returns>
        public async Task<IEnumerable<ClubMember>> GetClubModeratorsAsync(int clubId)
        {
            try
            {
                if (clubId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID: {clubId}");
                    return new List<ClubMember>();
                }

                return await _context.ClubMembers
                    .Include(cm => cm.User)
                    .Where(cm => cm.ClubID == clubId && cm.ClubRole == ClubRole.Member && cm.IsActive)
                    .OrderBy(cm => cm.User.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting club moderators for club {clubId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Analyzes role distribution across club membership.
        /// Provides organizational structure metrics for planning and reporting.
        ///
        /// Data Flow:
        /// 1. Query active club members from Users table
        /// 2. Group members by their assigned roles
        /// 3. Count members in each role category
        /// 4. Return dictionary mapping roles to member counts
        ///
        /// Business Logic:
        /// - Only includes active members in analysis
        /// - Groups by ClubRole enum values
        /// - Provides comprehensive organizational overview
        ///
        /// Analytics Provided:
        /// - Leadership density (leaders vs members ratio)
        /// - Organizational balance assessment
        /// - Succession planning insights
        ///
        /// Usage: Dashboard analytics, organizational reports, capacity planning
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Dictionary mapping each role to its member count</returns>
        public async Task<Dictionary<ClubRole, int>> GetClubRoleDistributionAsync(int clubId)
        {
            try
            {
                if (clubId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID: {clubId}");
                    return new Dictionary<ClubRole, int>();
                }

                return await _context.ClubMembers
                    .Where(cm => cm.ClubID == clubId && cm.IsActive)
                    .GroupBy(cm => cm.ClubRole)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting club role distribution for club {clubId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Removes leadership roles and demotes users to regular membership.
        /// Handles leadership transitions and organizational restructuring.
        ///
        /// Data Flow:
        /// 1. Validate user exists and belongs to specified club
        /// 2. Check if user currently holds a leadership role
        /// 3. Demote user to Member role if applicable
        /// 4. Persist changes to database
        ///
        /// Business Logic:
        /// - Only affects users with leadership roles
        /// - Demotes to Member role (preserves club membership)
        /// - Validates user belongs to correct club
        /// - Maintains organizational integrity
        ///
        /// Leadership Transitions:
        /// - Chairman -> Member
        /// - ViceChairman -> Member
        /// - TeamLeader -> Member
        /// - Member -> No change (returns false)
        ///
        /// Usage: Role management, disciplinary actions, organizational restructuring
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <param name="userId">Unique identifier of the user to demote</param>
        /// <returns>True if demotion successful, false if user not leader or validation fails</returns>
        public async Task<bool> RemoveClubLeadershipAsync(int clubId, int userId)
        {
            try
            {
                if (clubId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID: {clubId}");
                    return false;
                }

                if (userId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid user ID: {userId}");
                    return false;
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.ClubID != clubId) return false;

                // This method is deprecated - club roles are now handled through ClubMembers table
                // TODO: Implement club membership removal through ClubMembers
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error removing leadership for user {userId} in club {clubId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates comprehensive statistics and analytics for a specific club.
        /// Aggregates membership, event, and organizational data for reporting and dashboards.
        ///
        /// Data Flow:
        /// 1. Retrieve club with complete relationship data
        /// 2. Calculate current membership statistics
        /// 3. Analyze event history and recent activity
        /// 4. Generate role distribution analytics
        /// 5. Compile comprehensive statistics dictionary
        ///
        /// Business Logic:
        /// - Combines multiple data sources for holistic view
        /// - Includes recent events for activity assessment
        /// - Provides organizational structure analysis
        /// - Returns flexible dictionary for various display needs
        ///
        /// Statistics Included:
        /// - MemberCount: Current active membership
        /// - EventCount: Total events organized
        /// - RecentEvents: Last 5 events with details
        /// - RoleDistribution: Leadership vs member breakdown
        /// - EstablishedDate: Club founding date
        /// - Description: Club information and settings
        ///
        /// Usage: Dashboard widgets, club profile pages, administrative reports, analytics
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <returns>Dictionary containing comprehensive club statistics and metrics</returns>
        public async Task<Dictionary<string, object>> GetClubStatisticsAsync(int clubId)
        {
            try
            {
                if (clubId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID: {clubId}");
                    return new Dictionary<string, object>();
                }

                var club = await GetClubByIdAsync(clubId);
                if (club == null) return new Dictionary<string, object>();

                var memberCount = await GetMemberCountAsync(clubId);
                var eventCount = club.Events?.Count ?? 0;
                var recentEvents = (club.Events ?? new List<Event>())
                    .OrderByDescending(e => e.EventDate)
                    .Take(5)
                    .Select(e => new { e.Name, e.EventDate, e.Location })
                    .ToList();

                var roleDistribution = await GetClubRoleDistributionAsync(clubId);

                return new Dictionary<string, object>
                {
                    ["MemberCount"] = memberCount,
                    ["EventCount"] = eventCount,
                    ["RecentEvents"] = recentEvents,
                    ["RoleDistribution"] = roleDistribution,
                    ["EstablishedDate"] = club!.EstablishedDate,
                    ["Description"] = club!.Description ?? ""
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting club statistics for club {clubId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adds a user to a club with the specified role.
        /// Creates a new ClubMember record to establish the relationship.
        ///
        /// Data Flow:
        /// 1. Validate user and club exist
        /// 2. Check if user is already a member
        /// 3. Create new ClubMember record with specified role
        /// 4. Handle Chairman succession if necessary
        ///
        /// Business Logic:
        /// - Prevents duplicate memberships
        /// - Enforces single Chairman rule per club
        /// - Creates active membership by default
        ///
        /// Usage: Member recruitment, role assignment, club expansion
        /// </summary>
        /// <param name="userId">Unique identifier of the user</param>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <param name="role">Club role to assign to the user</param>
        /// <returns>True if user added successfully, false otherwise</returns>
        public async Task<bool> AddUserToClubAsync(int userId, int clubId, ClubRole role)
        {
            try
            {
                if (userId <= 0 || clubId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid user ID ({userId}) or club ID ({clubId})");
                    return false;
                }

                var user = await _context.Users.FindAsync(userId);
                var club = await _context.Clubs.FindAsync(clubId);

                if (user == null || club == null)
                {
                    Console.WriteLine($"[CLUB_SERVICE] User or club not found. User: {user != null}, Club: {club != null}");
                    return false;
                }

                // Check if user is already a member
                var existingMembership = await _context.ClubMembers
                    .FirstOrDefaultAsync(cm => cm.UserID == userId && cm.ClubID == clubId);

                if (existingMembership != null)
                {
                    Console.WriteLine($"[CLUB_SERVICE] User {userId} is already a member of club {clubId}");
                    return false;
                }

                // Handle Chairman succession - demote current Chairman if assigning new one
                if (role == ClubRole.Chairman)
                {
                    var currentChairman = await _context.ClubMembers
                        .FirstOrDefaultAsync(cm => cm.ClubID == clubId && cm.ClubRole == ClubRole.Chairman && cm.IsActive);

                    if (currentChairman != null)
                    {
                        currentChairman.ClubRole = ClubRole.Admin;
                        Console.WriteLine($"[CLUB_SERVICE] Demoted current Chairman (User {currentChairman.UserID}) to Admin");
                    }
                }

                // Create new membership
                var newMembership = new ClubMember
                {
                    UserID = userId,
                    ClubID = clubId,
                    ClubRole = role,
                    JoinDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ClubMembers.Add(newMembership);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[CLUB_SERVICE] Successfully added user {userId} to club {clubId} with role {role}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error adding user {userId} to club {clubId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes a user's membership from a club completely.
        /// Deactivates the ClubMember record and handles leadership succession.
        ///
        /// Data Flow:
        /// 1. Validate user and club exist
        /// 2. Find active ClubMember record
        /// 3. Deactivate membership or remove record
        /// 4. Handle leadership succession if removing Chairman
        ///
        /// Business Logic:
        /// - Completely removes club membership
        /// - Handles Chairman succession automatically
        /// - Maintains audit trail through IsActive flag
        ///
        /// Usage: Member removal, disciplinary actions, voluntary departures
        /// </summary>
        /// <param name="clubId">Unique identifier of the club</param>
        /// <param name="userId">Unique identifier of the user to remove</param>
        /// <returns>True if removal successful, false otherwise</returns>
        public async Task<bool> RemoveClubMembershipAsync(int clubId, int userId)
        {
            try
            {
                if (clubId <= 0 || userId <= 0)
                {
                    Console.WriteLine($"[CLUB_SERVICE] Invalid club ID ({clubId}) or user ID ({userId})");
                    return false;
                }

                var membership = await _context.ClubMembers
                    .FirstOrDefaultAsync(cm => cm.ClubID == clubId && cm.UserID == userId && cm.IsActive);

                if (membership == null)
                {
                    Console.WriteLine($"[CLUB_SERVICE] No active membership found for user {userId} in club {clubId}");
                    return false;
                }

                // Handle Chairman succession - promote Vice Chairman or Admin
                if (membership.ClubRole == ClubRole.Chairman)
                {
                    var successor = await _context.ClubMembers
                        .Where(cm => cm.ClubID == clubId && cm.IsActive && cm.UserID != userId)
                        .OrderBy(cm => cm.ClubRole == ClubRole.Admin ? 0 : cm.ClubRole == ClubRole.Member ? 1 : 2)
                        .FirstOrDefaultAsync();

                    if (successor != null)
                    {
                        successor.ClubRole = ClubRole.Chairman;
                        Console.WriteLine($"[CLUB_SERVICE] Promoted user {successor.UserID} to Chairman after removing user {userId}");
                    }
                }

                // Remove membership
                membership.IsActive = false;
                await _context.SaveChangesAsync();

                Console.WriteLine($"[CLUB_SERVICE] Successfully removed user {userId} from club {clubId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error removing user {userId} from club {clubId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the total count of clubs in the database efficiently.
        ///
        /// Performance:
        /// - Uses COUNT query instead of loading all records
        /// - Optimized for dashboard statistics display
        ///
        /// Used by: Dashboard statistics, admin overview
        /// </summary>
        /// <returns>Total number of clubs in the system</returns>
        public async Task<int> GetTotalClubsCountAsync()
        {
            try
            {
                return await _context.Clubs.CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLUB_SERVICE] Error getting total clubs count: {ex.Message}");
                throw;
            }
        }
    }
}
