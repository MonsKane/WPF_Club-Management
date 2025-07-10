using ClubManagementApp.Models;
using ClubManagementApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;

namespace ClubManagementApp.Services
{
    public interface ISecurityService
    {
        // Password management
        Task<string> HashPasswordAsync(string password);
        Task<bool> VerifyPasswordAsync(string password, string hashedPassword);
        Task<bool> ValidatePasswordStrengthAsync(string password);
        Task<string> GenerateSecurePasswordAsync(int length = 12);
        
        // Token management
        Task<string> GenerateTokenAsync(TokenType tokenType, string userId, TimeSpan? expiration = null);
        Task<bool> ValidateTokenAsync(string token, TokenType tokenType, string userId);
        Task<bool> RevokeTokenAsync(string token);
        Task<List<SecurityToken>> GetActiveTokensAsync(string userId);
        Task CleanupExpiredTokensAsync();
        
        // Session management
        Task<string> CreateSessionAsync(string userId, string ipAddress, string userAgent);
        Task<bool> ValidateSessionAsync(string sessionId, string userId);
        Task<bool> RevokeSessionAsync(string sessionId);
        Task<List<UserSession>> GetActiveSessionsAsync(string userId);
        Task RevokeAllSessionsAsync(string userId);
        
        // Security monitoring
        Task LogSecurityEventAsync(SecurityEventType eventType, string userId, string details, string? ipAddress = null);
        Task<List<SecurityEvent>> GetSecurityEventsAsync(string? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<bool> CheckSuspiciousActivityAsync(string userId, string ipAddress);
        Task<SecurityReport> GenerateSecurityReportAsync(DateTime fromDate, DateTime toDate);
        
        // Account security
        Task<bool> IsAccountLockedAsync(string userId);
        Task LockAccountAsync(string userId, TimeSpan? lockDuration = null, string? reason = null);
        Task UnlockAccountAsync(string userId);
        Task<int> GetFailedLoginAttemptsAsync(string userId);
        Task IncrementFailedLoginAttemptsAsync(string userId);
        Task ResetFailedLoginAttemptsAsync(string userId);
        
        // Two-factor authentication
        Task<string> GenerateTwoFactorCodeAsync(string userId);
        Task<bool> ValidateTwoFactorCodeAsync(string userId, string code);
        Task<bool> IsTwoFactorEnabledAsync(string userId);
        Task EnableTwoFactorAsync(string userId);
        Task DisableTwoFactorAsync(string userId);
        
        // Data encryption
        Task<string> EncryptDataAsync(string data, string? key = null);
        Task<string> DecryptDataAsync(string encryptedData, string? key = null);
        Task<string> GenerateEncryptionKeyAsync();
        
        // Security configuration
        Task<SecurityConfiguration> GetSecurityConfigurationAsync();
        Task UpdateSecurityConfigurationAsync(SecurityConfiguration configuration);
        Task<bool> ValidateSecurityConfigurationAsync(SecurityConfiguration configuration);
    }

    public class SecurityService : ISecurityService
    {
        private readonly ClubManagementDbContext _context;
        private readonly ILoggingService _loggingService;
        private readonly IAuditService _auditService;
        private readonly IConfigurationService _configurationService;
        private SecurityConfiguration? _securityConfiguration;
        private readonly string _encryptionKey;

        public SecurityService(
            ClubManagementDbContext context,
            ILoggingService loggingService,
            IAuditService auditService,
            IConfigurationService configurationService)
        {
            _context = context;
            _loggingService = loggingService;
            _auditService = auditService;
            _configurationService = configurationService;
            _encryptionKey = GetOrCreateEncryptionKey();
        }

        public async Task<string> HashPasswordAsync(string password)
        {
            try
            {
                using (var rng = RandomNumberGenerator.Create())
                {
                    byte[] salt = new byte[16];
                    rng.GetBytes(salt);

                    using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
                    {
                        byte[] hash = pbkdf2.GetBytes(32);
                        byte[] hashBytes = new byte[48];
                        Array.Copy(salt, 0, hashBytes, 0, 16);
                        Array.Copy(hash, 0, hashBytes, 16, 32);
                        return Convert.ToBase64String(hashBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to hash password", ex);
                throw;
            }
        }

        public async Task<bool> VerifyPasswordAsync(string password, string hashedPassword)
        {
            try
            {
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(32);
                    for (int i = 0; i < 32; i++)
                    {
                        if (hashBytes[i + 16] != hash[i])
                            return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to verify password", ex);
                return false;
            }
        }

        public async Task<bool> ValidatePasswordStrengthAsync(string password)
        {
            try
            {
                var config = await GetSecurityConfigurationAsync();
                
                if (password.Length < config.MinPasswordLength)
                    return false;

                if (config.RequireUppercase && !password.Any(char.IsUpper))
                    return false;

                if (config.RequireLowercase && !password.Any(char.IsLower))
                    return false;

                if (config.RequireDigits && !password.Any(char.IsDigit))
                    return false;

                if (config.RequireSpecialCharacters && !password.Any(c => !char.IsLetterOrDigit(c)))
                    return false;

                // Check for common weak passwords
                var weakPasswords = new[] { "password", "123456", "qwerty", "admin", "letmein" };
                if (weakPasswords.Contains(password.ToLower()))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to validate password strength", ex);
                return false;
            }
        }

        public async Task<string> GenerateSecurePasswordAsync(int length = 12)
        {
            try
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
                using (var rng = RandomNumberGenerator.Create())
                {
                    var password = new StringBuilder();
                    byte[] randomBytes = new byte[length];
                    rng.GetBytes(randomBytes);

                    for (int i = 0; i < length; i++)
                    {
                        password.Append(chars[randomBytes[i] % chars.Length]);
                    }

                    var generatedPassword = password.ToString();
                    
                    // Ensure it meets strength requirements
                    if (await ValidatePasswordStrengthAsync(generatedPassword))
                        return generatedPassword;
                    else
                        return await GenerateSecurePasswordAsync(length); // Retry
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to generate secure password", ex);
                throw;
            }
        }

        public async Task<string> GenerateTokenAsync(TokenType tokenType, string userId, TimeSpan? expiration = null)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
                
                if (expiration.HasValue && expiration.Value <= TimeSpan.Zero)
                    throw new ArgumentException("Expiration time must be positive", nameof(expiration));

                var token = Guid.NewGuid().ToString("N");
                var expirationTime = expiration ?? TimeSpan.FromHours(24);

                var securityToken = new SecurityToken
                {
                    Id = Guid.NewGuid().ToString(),
                    Token = token,
                    TokenType = tokenType,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(expirationTime),
                    IsRevoked = false
                };

                await SaveTokenAsync(securityToken);
                await LogSecurityEventAsync(SecurityEventType.TokenGenerated, userId, $"Token generated: {tokenType}");
                
                return token;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to generate token for user {userId}", ex);
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token, TokenType tokenType, string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(token))
                    throw new ArgumentException("Token cannot be null or empty", nameof(token));
                
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var securityToken = await GetTokenAsync(token);
                
                if (securityToken == null || 
                    securityToken.TokenType != tokenType ||
                    securityToken.UserId != userId ||
                    securityToken.IsRevoked ||
                    securityToken.ExpiresAt < DateTime.UtcNow)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to validate token for user {userId}", ex);
                return false;
            }
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(token))
                    throw new ArgumentException("Token cannot be null or empty", nameof(token));

                var securityToken = await GetTokenAsync(token);
                if (securityToken != null)
                {
                    securityToken.IsRevoked = true;
                    securityToken.RevokedAt = DateTime.UtcNow;
                    await UpdateTokenAsync(securityToken);
                    
                    await LogSecurityEventAsync(SecurityEventType.TokenRevoked, securityToken.UserId, $"Token revoked: {securityToken.TokenType}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to revoke token", ex);
                return false;
            }
        }

        public async Task<List<SecurityToken>> GetActiveTokensAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var tokens = await GetUserTokensAsync(userId);
                return tokens.Where(t => !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow).ToList();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get active tokens for user {userId}", ex);
                return new List<SecurityToken>();
            }
        }

        public async Task CleanupExpiredTokensAsync()
        {
            try
            {
                var expiredTokens = await GetExpiredTokensAsync();
                foreach (var token in expiredTokens)
                {
                    await DeleteTokenAsync(token.Id);
                }

                if (expiredTokens.Any())
                {
                    await _loggingService.LogInformationAsync($"Cleaned up {expiredTokens.Count} expired tokens");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to cleanup expired tokens", ex);
            }
        }

        public async Task<string> CreateSessionAsync(string userId, string ipAddress, string userAgent)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
                
                if (string.IsNullOrWhiteSpace(ipAddress))
                    throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));

                var sessionId = Guid.NewGuid().ToString();
                var session = new UserSession
                {
                    Id = sessionId,
                    UserId = userId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    IsActive = true
                };

                await SaveSessionAsync(session);
                await LogSecurityEventAsync(SecurityEventType.SessionCreated, userId, $"Session created from {ipAddress}", ipAddress);
                
                return sessionId;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to create session for user {userId}", ex);
                throw;
            }
        }

        public async Task<bool> ValidateSessionAsync(string sessionId, string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(sessionId))
                    throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
                
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var session = await GetSessionAsync(sessionId);
                
                if (session == null || 
                    session.UserId != userId ||
                    !session.IsActive ||
                    session.LastActivityAt < DateTime.UtcNow.AddHours(-24)) // 24-hour timeout
                {
                    return false;
                }

                // Update last activity
                session.LastActivityAt = DateTime.UtcNow;
                await UpdateSessionAsync(session);
                
                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to validate session {sessionId}", ex);
                return false;
            }
        }

        public async Task<bool> RevokeSessionAsync(string sessionId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(sessionId))
                    throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

                var session = await GetSessionAsync(sessionId);
                if (session != null)
                {
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow;
                    await UpdateSessionAsync(session);
                    
                    await LogSecurityEventAsync(SecurityEventType.SessionEnded, session.UserId, "Session revoked");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to revoke session {sessionId}", ex);
                return false;
            }
        }

        public async Task<List<UserSession>> GetActiveSessionsAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var sessions = await GetUserSessionsAsync(userId);
                return sessions.Where(s => s.IsActive && s.LastActivityAt > DateTime.UtcNow.AddHours(-24)).ToList();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get active sessions for user {userId}", ex);
                return new List<UserSession>();
            }
        }

        public async Task RevokeAllSessionsAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var sessions = await GetActiveSessionsAsync(userId);
                foreach (var session in sessions)
                {
                    await RevokeSessionAsync(session.Id);
                }

                await LogSecurityEventAsync(SecurityEventType.AllSessionsRevoked, userId, "All sessions revoked");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to revoke all sessions for user {userId}", ex);
            }
        }

        public async Task LogSecurityEventAsync(SecurityEventType eventType, string userId, string details, string? ipAddress = null)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
                
                if (string.IsNullOrWhiteSpace(details))
                    throw new ArgumentException("Details cannot be null or empty", nameof(details));

                var securityEvent = new SecurityEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    EventType = eventType,
                    UserId = userId,
                    Details = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                await SaveSecurityEventAsync(securityEvent);
                if (int.TryParse(userId, out int userIdInt))
                {
                    await _auditService.LogSecurityEventAsync(eventType.ToString(), details, userIdInt, ipAddress);
                }
                else
                {
                    await _auditService.LogSecurityEventAsync(eventType.ToString(), details, null, ipAddress);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to log security event", ex);
            }
        }

        public async Task<List<SecurityEvent>> GetSecurityEventsAsync(string? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var events = await GetAllSecurityEventsAsync();
                
                if (!string.IsNullOrEmpty(userId))
                    events = events.Where(e => e.UserId == userId).ToList();
                
                if (fromDate.HasValue)
                    events = events.Where(e => e.Timestamp >= fromDate.Value).ToList();
                
                if (toDate.HasValue)
                    events = events.Where(e => e.Timestamp <= toDate.Value).ToList();

                return events.OrderByDescending(e => e.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get security events", ex);
                return new List<SecurityEvent>();
            }
        }

        public async Task<bool> CheckSuspiciousActivityAsync(string userId, string ipAddress)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
                
                if (string.IsNullOrWhiteSpace(ipAddress))
                    throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));

                var recentEvents = await GetSecurityEventsAsync(userId, DateTime.UtcNow.AddHours(-1));
                
                return await CheckFailedLoginAttemptsAsync(userId, ipAddress, recentEvents) ||
                       await CheckMultipleIpLoginsAsync(userId, ipAddress, recentEvents);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to check suspicious activity for user {userId}", ex);
                return false;
            }
        }

        private async Task<bool> CheckFailedLoginAttemptsAsync(string userId, string ipAddress, List<SecurityEvent> recentEvents)
        {
            const int maxFailedLogins = 5;
            var failedLogins = recentEvents.Count(e => e.EventType == SecurityEventType.LoginFailed);
            
            if (failedLogins >= maxFailedLogins)
            {
                await LogSecurityEventAsync(SecurityEventType.SuspiciousActivity, userId, 
                    $"Multiple failed login attempts: {failedLogins}", ipAddress);
                return true;
            }
            return false;
        }

        private async Task<bool> CheckMultipleIpLoginsAsync(string userId, string ipAddress, List<SecurityEvent> recentEvents)
        {
            const int maxUniqueIps = 3;
            var uniqueIPs = recentEvents
                .Where(e => e.EventType == SecurityEventType.LoginSuccessful && !string.IsNullOrEmpty(e.IpAddress))
                .Select(e => e.IpAddress)
                .Distinct()
                .Count();
            
            if (uniqueIPs >= maxUniqueIps)
            {
                await LogSecurityEventAsync(SecurityEventType.SuspiciousActivity, userId, 
                    $"Login from multiple IPs: {uniqueIPs}", ipAddress);
                return true;
            }
            return false;
        }

        public async Task<SecurityReport> GenerateSecurityReportAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var events = await GetSecurityEventsAsync(null, fromDate, toDate);
                
                return new SecurityReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalEvents = events.Count,
                    LoginAttempts = events.Count(e => e.EventType == SecurityEventType.LoginAttempt),
                    SuccessfulLogins = events.Count(e => e.EventType == SecurityEventType.LoginSuccessful),
                    FailedLogins = events.Count(e => e.EventType == SecurityEventType.LoginFailed),
                    SuspiciousActivities = events.Count(e => e.EventType == SecurityEventType.SuspiciousActivity),
                    AccountLockouts = events.Count(e => e.EventType == SecurityEventType.AccountLocked),
                    PasswordResets = events.Count(e => e.EventType == SecurityEventType.PasswordReset),
                    UniqueUsers = events.Select(e => e.UserId).Distinct().Count(),
                    UniqueIpAddresses = events.Where(e => !string.IsNullOrEmpty(e.IpAddress))
                                             .Select(e => e.IpAddress).Distinct().Count(),
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to generate security report", ex);
                throw;
            }
        }

        public async Task<bool> IsAccountLockedAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var lockInfo = await GetAccountLockInfoAsync(userId);
                return lockInfo != null && lockInfo.IsLocked && 
                       (lockInfo.LockExpiresAt == null || lockInfo.LockExpiresAt > DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to check if account {userId} is locked", ex);
                return false;
            }
        }

        public async Task LockAccountAsync(string userId, TimeSpan? lockDuration = null, string? reason = null)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
                
                if (lockDuration.HasValue && lockDuration.Value <= TimeSpan.Zero)
                    throw new ArgumentException("Lock duration must be positive", nameof(lockDuration));

                var lockInfo = new AccountLockInfo
                {
                    UserId = userId,
                    IsLocked = true,
                    LockedAt = DateTime.UtcNow,
                    LockExpiresAt = lockDuration.HasValue ? DateTime.UtcNow.Add(lockDuration.Value) : null,
                    Reason = reason ?? "Security policy violation"
                };

                await SaveAccountLockInfoAsync(lockInfo);
                await LogSecurityEventAsync(SecurityEventType.AccountLocked, userId, $"Account locked: {reason}");
                await RevokeAllSessionsAsync(userId);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to lock account {userId}", ex);
                throw;
            }
        }

        public async Task UnlockAccountAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var lockInfo = await GetAccountLockInfoAsync(userId);
                if (lockInfo != null)
                {
                    lockInfo.IsLocked = false;
                    lockInfo.UnlockedAt = DateTime.UtcNow;
                    await UpdateAccountLockInfoAsync(lockInfo);
                }

                await ResetFailedLoginAttemptsAsync(userId);
                await LogSecurityEventAsync(SecurityEventType.AccountUnlocked, userId, "Account unlocked");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to unlock account {userId}", ex);
                throw;
            }
        }

        public async Task<int> GetFailedLoginAttemptsAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var loginInfo = await GetLoginInfoAsync(userId);
                return loginInfo?.FailedAttempts ?? 0;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get failed login attempts for user {userId}", ex);
                return 0;
            }
        }

        public async Task IncrementFailedLoginAttemptsAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var loginInfo = await GetLoginInfoAsync(userId) ?? new LoginInfo { UserId = userId };
                loginInfo.FailedAttempts++;
                loginInfo.LastFailedAttempt = DateTime.UtcNow;
                
                await SaveLoginInfoAsync(loginInfo);
                await LogSecurityEventAsync(SecurityEventType.LoginFailed, userId, $"Failed login attempt #{loginInfo.FailedAttempts}");

                // Auto-lock account after too many failed attempts
                var config = await GetSecurityConfigurationAsync();
                if (loginInfo.FailedAttempts >= config.MaxFailedLoginAttempts)
                {
                    await LockAccountAsync(userId, TimeSpan.FromMinutes(config.AccountLockoutDurationMinutes), 
                        "Too many failed login attempts");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to increment failed login attempts for user {userId}", ex);
            }
        }

        public async Task ResetFailedLoginAttemptsAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var loginInfo = await GetLoginInfoAsync(userId);
                if (loginInfo != null)
                {
                    loginInfo.FailedAttempts = 0;
                    loginInfo.LastSuccessfulLogin = DateTime.UtcNow;
                    await SaveLoginInfoAsync(loginInfo);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to reset failed login attempts for user {userId}", ex);
            }
        }

        public async Task<string> GenerateTwoFactorCodeAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var code = new Random().Next(100000, 999999).ToString();
                var twoFactorInfo = new TwoFactorInfo
                {
                    UserId = userId,
                    Code = code,
                    GeneratedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false
                };

                await SaveTwoFactorInfoAsync(twoFactorInfo);
                await LogSecurityEventAsync(SecurityEventType.TwoFactorCodeGenerated, userId, "2FA code generated");
                
                return code;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to generate 2FA code for user {userId}", ex);
                throw;
            }
        }

        public async Task<bool> ValidateTwoFactorCodeAsync(string userId, string code)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
                
                if (string.IsNullOrWhiteSpace(code))
                    throw new ArgumentException("Code cannot be null or empty", nameof(code));

                var twoFactorInfo = await GetTwoFactorInfoAsync(userId, code);
                
                if (twoFactorInfo == null || 
                    twoFactorInfo.IsUsed ||
                    twoFactorInfo.ExpiresAt < DateTime.UtcNow)
                {
                    await LogSecurityEventAsync(SecurityEventType.TwoFactorCodeFailed, userId, "Invalid 2FA code");
                    return false;
                }

                twoFactorInfo.IsUsed = true;
                twoFactorInfo.UsedAt = DateTime.UtcNow;
                await UpdateTwoFactorInfoAsync(twoFactorInfo);
                
                await LogSecurityEventAsync(SecurityEventType.TwoFactorCodeValidated, userId, "2FA code validated");
                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to validate 2FA code for user {userId}", ex);
                return false;
            }
        }

        public async Task<bool> IsTwoFactorEnabledAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var user = await _context.Users.FindAsync(userId);
                return user?.TwoFactorEnabled ?? false;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to check 2FA status for user {userId}", ex);
                return false;
            }
        }

        public async Task EnableTwoFactorAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.TwoFactorEnabled = true;
                    await _context.SaveChangesAsync();
                    await LogSecurityEventAsync(SecurityEventType.TwoFactorEnabled, userId, "2FA enabled");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to enable 2FA for user {userId}", ex);
                throw;
            }
        }

        public async Task DisableTwoFactorAsync(string userId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.TwoFactorEnabled = false;
                    await _context.SaveChangesAsync();
                    await LogSecurityEventAsync(SecurityEventType.TwoFactorDisabled, userId, "2FA disabled");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to disable 2FA for user {userId}", ex);
                throw;
            }
        }

        public async Task<string> EncryptDataAsync(string data, string? key = null)
        {
            try
            {
                // Input validation
                if (string.IsNullOrEmpty(data))
                    throw new ArgumentException("Data cannot be null or empty", nameof(data));

                var encryptionKey = key ?? _encryptionKey;
                using (var aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(encryptionKey);
                    aes.GenerateIV();

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    {
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            await swEncrypt.WriteAsync(data);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to encrypt data", ex);
                throw;
            }
        }

        public async Task<string> DecryptDataAsync(string encryptedData, string? key = null)
        {
            try
            {
                // Input validation
                if (string.IsNullOrEmpty(encryptedData))
                    throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));

                var encryptionKey = key ?? _encryptionKey;
                var fullCipher = Convert.FromBase64String(encryptedData);

                using (var aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(encryptionKey);
                    var iv = new byte[aes.BlockSize / 8];
                    var cipher = new byte[fullCipher.Length - iv.Length];

                    Array.Copy(fullCipher, iv, iv.Length);
                    Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(cipher))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return await srDecrypt.ReadToEndAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to decrypt data", ex);
                throw;
            }
        }

        public Task<string> GenerateEncryptionKeyAsync()
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.GenerateKey();
                    return Task.FromResult(Convert.ToBase64String(aes.Key));
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogErrorAsync("Failed to generate encryption key", ex);
                throw;
            }
        }

        public Task<SecurityConfiguration> GetSecurityConfigurationAsync()
        {
            if (_securityConfiguration == null)
            {
                _securityConfiguration = new SecurityConfiguration
                {
                    MinPasswordLength = _configurationService.GetValue<int>("Security:MinPasswordLength", 8),
                RequireUppercase = _configurationService.GetValue<bool>("Security:RequireUppercase", true),
                RequireLowercase = _configurationService.GetValue<bool>("Security:RequireLowercase", true),
                RequireDigits = _configurationService.GetValue<bool>("Security:RequireDigits", true),
                RequireSpecialCharacters = _configurationService.GetValue<bool>("Security:RequireSpecialCharacters", true),
                MaxFailedLoginAttempts = _configurationService.GetValue<int>("Security:MaxFailedLoginAttempts", 5),
                AccountLockoutDurationMinutes = _configurationService.GetValue<int>("Security:AccountLockoutDurationMinutes", 30),
                SessionTimeoutMinutes = _configurationService.GetValue<int>("Security:SessionTimeoutMinutes", 1440),
                RequireTwoFactor = _configurationService.GetValue<bool>("Security:RequireTwoFactor", false),
                PasswordExpirationDays = _configurationService.GetValue<int>("Security:PasswordExpirationDays", 90)
                };
            }

            return Task.FromResult(_securityConfiguration);
        }

        public async Task UpdateSecurityConfigurationAsync(SecurityConfiguration configuration)
        {
            try
            {
                // Input validation
                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));
                
                if (!await ValidateSecurityConfigurationAsync(configuration))
                    throw new ArgumentException("Invalid security configuration", nameof(configuration));

                _configurationService.SetValue("Security:MinPasswordLength", configuration.MinPasswordLength);
                _configurationService.SetValue("Security:RequireUppercase", configuration.RequireUppercase);
                _configurationService.SetValue("Security:RequireLowercase", configuration.RequireLowercase);
                _configurationService.SetValue("Security:RequireDigits", configuration.RequireDigits);
                _configurationService.SetValue("Security:RequireSpecialCharacters", configuration.RequireSpecialCharacters);
                _configurationService.SetValue("Security:MaxFailedLoginAttempts", configuration.MaxFailedLoginAttempts);
                _configurationService.SetValue("Security:AccountLockoutDurationMinutes", configuration.AccountLockoutDurationMinutes);
                _configurationService.SetValue("Security:SessionTimeoutMinutes", configuration.SessionTimeoutMinutes);
                _configurationService.SetValue("Security:RequireTwoFactor", configuration.RequireTwoFactor);
                _configurationService.SetValue("Security:PasswordExpirationDays", configuration.PasswordExpirationDays);
                
                await _configurationService.SaveAsync();
                _securityConfiguration = null; // Reset cache
                
                await _auditService.LogSystemEventAsync("Security Configuration Updated", "Security settings have been updated");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to update security configuration", ex);
                throw;
            }
        }

        public async Task<bool> ValidateSecurityConfigurationAsync(SecurityConfiguration configuration)
        {
            try
            {
                // Input validation
                if (configuration == null)
                    return false;

                if (configuration.MinPasswordLength < 6 || configuration.MinPasswordLength > 128)
                    return false;

                if (configuration.MaxFailedLoginAttempts < 1 || configuration.MaxFailedLoginAttempts > 20)
                    return false;

                if (configuration.AccountLockoutDurationMinutes < 1 || configuration.AccountLockoutDurationMinutes > 1440)
                    return false;

                if (configuration.SessionTimeoutMinutes < 5 || configuration.SessionTimeoutMinutes > 10080)
                    return false;

                if (configuration.PasswordExpirationDays < 1 || configuration.PasswordExpirationDays > 365)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to validate security configuration", ex);
                return false;
            }
        }

        // Private helper methods for data persistence
        private string GetOrCreateEncryptionKey()
        {
            var keyPath = GetSecurityDataPath("encryption.key");
            if (File.Exists(keyPath))
            {
                return File.ReadAllText(keyPath);
            }
            else
            {
                using (var aes = Aes.Create())
                {
                    aes.GenerateKey();
                    var key = Convert.ToBase64String(aes.Key);
                    File.WriteAllText(keyPath, key);
                    return key;
                }
            }
        }

        private string GetSecurityDataPath(string fileName)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var securityPath = Path.Combine(appDataPath, "ClubManagement", "Security");
            Directory.CreateDirectory(securityPath);
            return Path.Combine(securityPath, fileName);
        }

        // Data persistence methods (using JSON files for simplicity)
        private async Task SaveTokenAsync(SecurityToken token)
        {
            var tokens = await GetAllTokensAsync();
            tokens.Add(token);
            await SaveTokensAsync(tokens);
        }

        private async Task<SecurityToken?> GetTokenAsync(string token)
        {
            var tokens = await GetAllTokensAsync();
            return tokens.FirstOrDefault(t => t.Token == token);
        }

        private async Task<List<SecurityToken>> GetUserTokensAsync(string userId)
        {
            var tokens = await GetAllTokensAsync();
            return tokens.Where(t => t.UserId == userId).ToList();
        }

        private async Task<List<SecurityToken>> GetExpiredTokensAsync()
        {
            var tokens = await GetAllTokensAsync();
            return tokens.Where(t => t.ExpiresAt < DateTime.UtcNow).ToList();
        }

        private async Task<List<SecurityToken>> GetAllTokensAsync()
        {
            var filePath = GetSecurityDataPath("tokens.json");
            if (!File.Exists(filePath))
                return new List<SecurityToken>();

            var jsonData = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<SecurityToken>>(jsonData) ?? new List<SecurityToken>();
        }

        private async Task SaveTokensAsync(List<SecurityToken> tokens)
        {
            var filePath = GetSecurityDataPath("tokens.json");
            var jsonData = JsonSerializer.Serialize(tokens, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, jsonData);
        }

        private async Task UpdateTokenAsync(SecurityToken token)
        {
            var tokens = await GetAllTokensAsync();
            var existingToken = tokens.FirstOrDefault(t => t.Id == token.Id);
            if (existingToken != null)
            {
                var index = tokens.IndexOf(existingToken);
                tokens[index] = token;
                await SaveTokensAsync(tokens);
            }
        }

        private async Task DeleteTokenAsync(string tokenId)
        {
            var tokens = await GetAllTokensAsync();
            tokens.RemoveAll(t => t.Id == tokenId);
            await SaveTokensAsync(tokens);
        }

        // Similar methods for sessions, security events, etc. would follow the same pattern
        // For brevity, I'll include just the key ones

        private async Task SaveSessionAsync(UserSession session)
        {
            var sessions = await GetAllSessionsAsync();
            sessions.Add(session);
            await SaveSessionsAsync(sessions);
        }

        private async Task<UserSession?> GetSessionAsync(string sessionId)
        {
            var sessions = await GetAllSessionsAsync();
            return sessions.FirstOrDefault(s => s.Id == sessionId);
        }

        private async Task<List<UserSession>> GetUserSessionsAsync(string userId)
        {
            var sessions = await GetAllSessionsAsync();
            return sessions.Where(s => s.UserId == userId).ToList();
        }

        private async Task<List<UserSession>> GetAllSessionsAsync()
        {
            var filePath = GetSecurityDataPath("sessions.json");
            if (!File.Exists(filePath))
                return new List<UserSession>();

            var jsonData = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<UserSession>>(jsonData) ?? new List<UserSession>();
        }

        private async Task SaveSessionsAsync(List<UserSession> sessions)
        {
            var filePath = GetSecurityDataPath("sessions.json");
            var jsonData = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, jsonData);
        }

        private async Task UpdateSessionAsync(UserSession session)
        {
            var sessions = await GetAllSessionsAsync();
            var existingSession = sessions.FirstOrDefault(s => s.Id == session.Id);
            if (existingSession != null)
            {
                var index = sessions.IndexOf(existingSession);
                sessions[index] = session;
                await SaveSessionsAsync(sessions);
            }
        }

        private async Task SaveSecurityEventAsync(SecurityEvent securityEvent)
        {
            var events = await GetAllSecurityEventsAsync();
            events.Add(securityEvent);
            await SaveSecurityEventsAsync(events);
        }

        private async Task<List<SecurityEvent>> GetAllSecurityEventsAsync()
        {
            var filePath = GetSecurityDataPath("security_events.json");
            if (!File.Exists(filePath))
                return new List<SecurityEvent>();

            var jsonData = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<SecurityEvent>>(jsonData) ?? new List<SecurityEvent>();
        }

        private async Task SaveSecurityEventsAsync(List<SecurityEvent> events)
        {
            var filePath = GetSecurityDataPath("security_events.json");
            var jsonData = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, jsonData);
        }

        // Additional helper methods for other data types would follow similar patterns
        private async Task<AccountLockInfo?> GetAccountLockInfoAsync(string userId)
        {
            var filePath = GetSecurityDataPath("account_locks.json");
            if (!File.Exists(filePath))
                return null;

            var jsonData = await File.ReadAllTextAsync(filePath);
            var lockInfos = JsonSerializer.Deserialize<List<AccountLockInfo>>(jsonData) ?? new List<AccountLockInfo>();
            return lockInfos.FirstOrDefault(l => l.UserId == userId);
        }

        private async Task SaveAccountLockInfoAsync(AccountLockInfo lockInfo)
        {
            var filePath = GetSecurityDataPath("account_locks.json");
            var lockInfos = new List<AccountLockInfo>();
            
            if (File.Exists(filePath))
            {
                var jsonData = await File.ReadAllTextAsync(filePath);
                lockInfos = JsonSerializer.Deserialize<List<AccountLockInfo>>(jsonData) ?? new List<AccountLockInfo>();
            }
            
            var existing = lockInfos.FirstOrDefault(l => l.UserId == lockInfo.UserId);
            if (existing != null)
            {
                var index = lockInfos.IndexOf(existing);
                lockInfos[index] = lockInfo;
            }
            else
            {
                lockInfos.Add(lockInfo);
            }
            
            var updatedJsonData = JsonSerializer.Serialize(lockInfos, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, updatedJsonData);
        }

        private async Task UpdateAccountLockInfoAsync(AccountLockInfo lockInfo)
        {
            await SaveAccountLockInfoAsync(lockInfo);
        }

        private async Task<LoginInfo?> GetLoginInfoAsync(string userId)
        {
            var filePath = GetSecurityDataPath("login_info.json");
            if (!File.Exists(filePath))
                return null;

            var jsonData = await File.ReadAllTextAsync(filePath);
            var loginInfos = JsonSerializer.Deserialize<List<LoginInfo>>(jsonData) ?? new List<LoginInfo>();
            return loginInfos.FirstOrDefault(l => l.UserId == userId);
        }

        private async Task SaveLoginInfoAsync(LoginInfo loginInfo)
        {
            var filePath = GetSecurityDataPath("login_info.json");
            var loginInfos = new List<LoginInfo>();
            
            if (File.Exists(filePath))
            {
                var jsonData = await File.ReadAllTextAsync(filePath);
                loginInfos = JsonSerializer.Deserialize<List<LoginInfo>>(jsonData) ?? new List<LoginInfo>();
            }
            
            var existing = loginInfos.FirstOrDefault(l => l.UserId == loginInfo.UserId);
            if (existing != null)
            {
                var index = loginInfos.IndexOf(existing);
                loginInfos[index] = loginInfo;
            }
            else
            {
                loginInfos.Add(loginInfo);
            }
            
            var updatedJsonData = JsonSerializer.Serialize(loginInfos, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, updatedJsonData);
        }

        private async Task SaveTwoFactorInfoAsync(TwoFactorInfo twoFactorInfo)
        {
            var filePath = GetSecurityDataPath("two_factor.json");
            var twoFactorInfos = new List<TwoFactorInfo>();
            
            if (File.Exists(filePath))
            {
                var jsonData = await File.ReadAllTextAsync(filePath);
                twoFactorInfos = JsonSerializer.Deserialize<List<TwoFactorInfo>>(jsonData) ?? new List<TwoFactorInfo>();
            }
            
            twoFactorInfos.Add(twoFactorInfo);
            
            var updatedJsonData = JsonSerializer.Serialize(twoFactorInfos, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, updatedJsonData);
        }

        private async Task<TwoFactorInfo?> GetTwoFactorInfoAsync(string userId, string code)
        {
            var filePath = GetSecurityDataPath("two_factor.json");
            if (!File.Exists(filePath))
                return null;

            var jsonData = await File.ReadAllTextAsync(filePath);
            var twoFactorInfos = JsonSerializer.Deserialize<List<TwoFactorInfo>>(jsonData) ?? new List<TwoFactorInfo>();
            return twoFactorInfos.FirstOrDefault(t => t.UserId == userId && t.Code == code);
        }

        private async Task UpdateTwoFactorInfoAsync(TwoFactorInfo twoFactorInfo)
        {
            var filePath = GetSecurityDataPath("two_factor.json");
            if (!File.Exists(filePath))
                return;

            var jsonData = await File.ReadAllTextAsync(filePath);
            var twoFactorInfos = JsonSerializer.Deserialize<List<TwoFactorInfo>>(jsonData) ?? new List<TwoFactorInfo>();
            
            var existing = twoFactorInfos.FirstOrDefault(t => t.UserId == twoFactorInfo.UserId && t.Code == twoFactorInfo.Code);
            if (existing != null)
            {
                var index = twoFactorInfos.IndexOf(existing);
                twoFactorInfos[index] = twoFactorInfo;
                
                var updatedJsonData = JsonSerializer.Serialize(twoFactorInfos, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, updatedJsonData);
            }
        }
    }

    // Supporting classes and enums
    public class SecurityToken
    {
        public string Id { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public TokenType TokenType { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
    }

    public class UserSession
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class SecurityEvent
    {
        public string Id { get; set; } = string.Empty;
        public SecurityEventType EventType { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SecurityReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalEvents { get; set; }
        public int LoginAttempts { get; set; }
        public int SuccessfulLogins { get; set; }
        public int FailedLogins { get; set; }
        public int SuspiciousActivities { get; set; }
        public int AccountLockouts { get; set; }
        public int PasswordResets { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueIpAddresses { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class SecurityConfiguration
    {
        public int MinPasswordLength { get; set; } = 8;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigits { get; set; } = true;
        public bool RequireSpecialCharacters { get; set; } = true;
        public int MaxFailedLoginAttempts { get; set; } = 5;
        public int AccountLockoutDurationMinutes { get; set; } = 30;
        public int SessionTimeoutMinutes { get; set; } = 1440;
        public bool RequireTwoFactor { get; set; } = false;
        public int PasswordExpirationDays { get; set; } = 90;
    }

    public class AccountLockInfo
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public DateTime LockedAt { get; set; }
        public DateTime? LockExpiresAt { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LoginInfo
    {
        public string UserId { get; set; } = string.Empty;
        public int FailedAttempts { get; set; }
        public DateTime? LastFailedAttempt { get; set; }
        public DateTime? LastSuccessfulLogin { get; set; }
    }

    public class TwoFactorInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
    }

    public enum TokenType
    {
        PasswordReset,
        EmailVerification,
        AccountActivation,
        ApiAccess,
        TwoFactorBackup
    }

    public enum SecurityEventType
    {
        LoginAttempt,
        LoginSuccessful,
        LoginFailed,
        Logout,
        PasswordChanged,
        PasswordReset,
        AccountLocked,
        AccountUnlocked,
        TwoFactorEnabled,
        TwoFactorDisabled,
        TwoFactorCodeGenerated,
        TwoFactorCodeValidated,
        TwoFactorCodeFailed,
        TokenGenerated,
        TokenRevoked,
        SessionCreated,
        SessionEnded,
        AllSessionsRevoked,
        SuspiciousActivity,
        DataAccessed,
        DataModified,
        PermissionDenied,
        SecurityConfigurationChanged
    }
}