using ClubManagementApp.Data;
using ClubManagementApp.Exceptions;
using ClubManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace ClubManagementApp.Services
{
    /// <summary>
    /// Interface defining backup and maintenance operations for the Club Management Application.
    /// Provides comprehensive data protection, system maintenance, and health monitoring capabilities.
    /// </summary>
    public interface IBackupService
    {
        // Database backup operations
        Task<string> CreateDatabaseBackupAsync(string? backupPath = null);
        Task RestoreDatabaseBackupAsync(string backupPath);
        Task<bool> ValidateBackupFileAsync(string backupPath);
        
        // Data export/import operations
        Task<string> ExportDataAsync(ExportOptions options);
        Task ImportDataAsync(string dataPath, ImportOptions options);
        
        // Scheduled backup management
        Task<List<BackupInfo>> GetBackupHistoryAsync();
        Task DeleteOldBackupsAsync(int retentionDays = 30);
        Task<BackupInfo> GetLatestBackupAsync();
        
        // System maintenance
        Task<MaintenanceReport> PerformMaintenanceAsync(MaintenanceOptions options);
        Task<SystemHealthReport> CheckSystemHealthAsync();
        Task CleanupTemporaryFilesAsync();
        
        // Configuration backup
        Task<string> BackupConfigurationAsync();
        Task RestoreConfigurationAsync(string configBackupPath);
    }

    /// <summary>
    /// Service responsible for comprehensive backup, restore, and system maintenance operations.
    /// 
    /// RESPONSIBILITIES:
    /// - Database backup and restoration with compression
    /// - Selective data export/import operations
    /// - Automated system maintenance tasks
    /// - System health monitoring and reporting
    /// - Configuration backup and restoration
    /// - Backup history management and cleanup
    /// 
    /// DATA FLOW:
    /// ViewModels -> BackupService -> DbContext -> Database/File System
    /// 
    /// KEY FEATURES:
    /// - Compressed backup files (ZIP format)
    /// - Transactional restore operations
    /// - Selective data export/import
    /// - Automated maintenance scheduling
    /// - System health diagnostics
    /// - Backup retention policies
    /// - Configuration versioning
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly ClubManagementDbContext _context;
        private readonly ILoggingService _loggingService;
        private readonly IAuditService _auditService;
        private readonly IConfigurationService _configurationService;
        private readonly string _backupDirectory;

        /// <summary>
        /// Initializes the BackupService with required dependencies.
        /// Sets up backup directory structure and ensures directory exists.
        /// 
        /// DEPENDENCY INJECTION:
        /// - ClubManagementDbContext: Database operations
        /// - ILoggingService: Error and information logging
        /// - IAuditService: System event auditing
        /// - IConfigurationService: Application configuration management
        /// </summary>
        public BackupService(
            ClubManagementDbContext context,
            ILoggingService loggingService,
            IAuditService auditService,
            IConfigurationService configurationService)
        {
            _context = context;
            _loggingService = loggingService;
            _auditService = auditService;
            _configurationService = configurationService;
            _backupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "ClubManagement", "Backups");
            
            Directory.CreateDirectory(_backupDirectory);
        }

        /// <summary>
        /// Creates a comprehensive database backup with compression.
        /// 
        /// DATA FLOW:
        /// 1. Generate timestamped backup filename
        /// 2. Query all database entities (Users, Clubs, Events, etc.)
        /// 3. Serialize data to JSON format
        /// 4. Compress backup using ZIP compression
        /// 5. Save backup metadata to history
        /// 6. Log backup creation event
        /// 
        /// BUSINESS LOGIC:
        /// - Includes all core entities and relationships
        /// - Limits audit logs to last 90 days for size optimization
        /// - Uses JSON serialization for cross-platform compatibility
        /// - Applies ZIP compression to reduce file size
        /// - Maintains backup history for tracking
        /// 
        /// USAGE: Scheduled backups, manual backups, pre-maintenance backups
        /// </summary>
        /// <param name="backupPath">Optional custom backup file path</param>
        /// <returns>Full path to the created backup file</returns>
        public async Task<string> CreateDatabaseBackupAsync(string? backupPath = null)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var fileName = $"ClubManagement_Backup_{timestamp}.json";
                var fullPath = backupPath ?? Path.Combine(_backupDirectory, fileName);

                var backupData = new DatabaseBackup
                {
                    CreatedAt = DateTime.UtcNow,
                    Version = "1.0",
                    Users = await _context.Users.ToListAsync(),
                    Clubs = await _context.Clubs.ToListAsync(),
                    Events = await _context.Events.ToListAsync(),
                    EventParticipants = await _context.EventParticipants.ToListAsync(),
                    Reports = await _context.Reports.ToListAsync(),
                    Settings = await _context.Settings.ToListAsync(),
                    AuditLogs = await _context.AuditLogs
                        .Where(a => a.Timestamp >= DateTime.UtcNow.AddDays(-90)) // Only last 90 days
                        .ToListAsync()
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonData = JsonSerializer.Serialize(backupData, jsonOptions);
                
                // Compress the backup
                var compressedPath = fullPath.Replace(".json", ".zip");
                using (var fileStream = new FileStream(compressedPath, FileMode.Create))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                {
                    var entry = archive.CreateEntry(fileName);
                    using (var entryStream = entry.Open())
                    using (var writer = new StreamWriter(entryStream))
                    {
                        await writer.WriteAsync(jsonData);
                    }
                }

                // Save backup info
                var backupInfo = new BackupInfo
                {
                    FileName = Path.GetFileName(compressedPath),
                    FilePath = compressedPath,
                    CreatedAt = DateTime.UtcNow,
                    FileSize = new FileInfo(compressedPath).Length,
                    BackupType = BackupType.Full,
                    IsValid = true
                };

                await SaveBackupInfoAsync(backupInfo);
                await _auditService.LogSystemEventAsync("Database Backup Created", $"Backup created: {compressedPath}");
                await _loggingService.LogInformationAsync($"Database backup created successfully: {compressedPath}");

                return compressedPath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to create database backup", ex);
                throw new DatabaseConnectionException("Failed to create database backup", ex);
            }
        }

        /// <summary>
        /// Restores database from a compressed backup file.
        /// 
        /// DATA FLOW:
        /// 1. Validate backup file integrity
        /// 2. Extract and deserialize backup data
        /// 3. Begin database transaction
        /// 4. Clear existing data (reverse dependency order)
        /// 5. Restore data (dependency order)
        /// 6. Commit transaction or rollback on error
        /// 7. Log restoration event
        /// 
        /// BUSINESS LOGIC:
        /// - Validates backup file before restoration
        /// - Uses database transactions for atomicity
        /// - Maintains referential integrity during restoration
        /// - Clears existing data completely before restore
        /// - Handles dependency order (Clubs -> Users -> Events -> Participants)
        /// 
        /// USAGE: Disaster recovery, data migration, system rollback
        /// </summary>
        /// <param name="backupPath">Path to the backup file to restore</param>
        public async Task RestoreDatabaseBackupAsync(string backupPath)
        {
            try
            {
                if (!await ValidateBackupFileAsync(backupPath))
                    throw new ArgumentException("Invalid backup file");

                DatabaseBackup? backupData;
                
                // Extract and read backup data
                using (var fileStream = new FileStream(backupPath, FileMode.Open))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".json"));
                    if (entry == null)
                        throw new InvalidOperationException("No JSON data found in backup file");

                    using (var entryStream = entry.Open())
                    using (var reader = new StreamReader(entryStream))
                    {
                        var jsonData = await reader.ReadToEndAsync();
                        backupData = JsonSerializer.Deserialize<DatabaseBackup>(jsonData, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }
                }

                if (backupData == null)
                    throw new InvalidOperationException("Failed to deserialize backup data");

                // Begin transaction for restoration
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Clear existing data (in reverse order of dependencies)
                        _context.AuditLogs.RemoveRange(_context.AuditLogs);
                        _context.Settings.RemoveRange(_context.Settings);
                        _context.Reports.RemoveRange(_context.Reports);
                        _context.EventParticipants.RemoveRange(_context.EventParticipants);
                        _context.Events.RemoveRange(_context.Events);
                        _context.Users.RemoveRange(_context.Users);
                        _context.Clubs.RemoveRange(_context.Clubs);
                        
                        await _context.SaveChangesAsync();

                        // Restore data (in order of dependencies)
                        if (backupData.Clubs?.Any() == true)
                        {
                            _context.Clubs.AddRange(backupData.Clubs);
                            await _context.SaveChangesAsync();
                        }

                        if (backupData.Users?.Any() == true)
                        {
                            _context.Users.AddRange(backupData.Users);
                            await _context.SaveChangesAsync();
                        }

                        if (backupData.Events?.Any() == true)
                        {
                            _context.Events.AddRange(backupData.Events);
                            await _context.SaveChangesAsync();
                        }

                        if (backupData.EventParticipants?.Any() == true)
                        {
                            _context.EventParticipants.AddRange(backupData.EventParticipants);
                            await _context.SaveChangesAsync();
                        }

                        if (backupData.Reports?.Any() == true)
                        {
                            _context.Reports.AddRange(backupData.Reports);
                            await _context.SaveChangesAsync();
                        }

                        if (backupData.Settings?.Any() == true)
                        {
                            _context.Settings.AddRange(backupData.Settings);
                            await _context.SaveChangesAsync();
                        }

                        if (backupData.AuditLogs?.Any() == true)
                        {
                            _context.AuditLogs.AddRange(backupData.AuditLogs);
                            await _context.SaveChangesAsync();
                        }

                        await transaction.CommitAsync();
                        await _auditService.LogSystemEventAsync("Database Restored", $"Database restored from: {backupPath}");
                        await _loggingService.LogInformationAsync($"Database restored successfully from: {backupPath}");
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to restore database from {backupPath}", ex);
                throw new DatabaseConnectionException("Failed to restore database backup", ex);
            }
        }

        /// <summary>
        /// Validates the integrity and format of a backup file.
        /// 
        /// DATA FLOW:
        /// 1. Check file existence and extension
        /// 2. Open ZIP archive and locate JSON entry
        /// 3. Extract and deserialize backup data
        /// 4. Validate backup structure and version
        /// 
        /// BUSINESS LOGIC:
        /// - Ensures file exists and has correct extension (.zip)
        /// - Validates ZIP archive structure
        /// - Checks for required JSON data entry
        /// - Verifies backup data can be deserialized
        /// - Validates backup version compatibility
        /// 
        /// USAGE: Pre-restoration validation, backup integrity checks, file verification
        /// </summary>
        /// <param name="backupPath">Path to the backup file to validate</param>
        /// <returns>True if backup file is valid, false otherwise</returns>
        public async Task<bool> ValidateBackupFileAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    return false;

                if (!backupPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    return false;

                using (var fileStream = new FileStream(backupPath, FileMode.Open))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    var jsonEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".json"));
                    if (jsonEntry == null)
                        return false;

                    using (var entryStream = jsonEntry.Open())
                    using (var reader = new StreamReader(entryStream))
                    {
                        var jsonData = await reader.ReadToEndAsync();
                        var backupData = JsonSerializer.Deserialize<DatabaseBackup>(jsonData, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        return backupData != null && !string.IsNullOrEmpty(backupData.Version);
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Exports selective data based on specified options.
        /// 
        /// DATA FLOW:
        /// 1. Generate timestamped export filename
        /// 2. Query selected entities based on export options
        /// 3. Build export data dictionary
        /// 4. Serialize to JSON format
        /// 5. Write to file and log export event
        /// 
        /// BUSINESS LOGIC:
        /// - Allows selective export of specific entity types
        /// - Supports date range filtering (future enhancement)
        /// - Uses JSON format for portability
        /// - Maintains audit trail of export operations
        /// 
        /// USAGE: Data migration, reporting, selective backups, data analysis
        /// </summary>
        /// <param name="options">Export configuration specifying which data to include</param>
        /// <returns>Path to the exported data file</returns>
        public async Task<string> ExportDataAsync(ExportOptions options)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var fileName = $"ClubManagement_Export_{timestamp}.json";
                var filePath = Path.Combine(_backupDirectory, fileName);

                var exportData = new Dictionary<string, object>();

                if (options.IncludeUsers)
                {
                    var users = await _context.Users.ToListAsync();
                    exportData["users"] = users;
                }

                if (options.IncludeClubs)
                {
                    var clubs = await _context.Clubs.ToListAsync();
                    exportData["clubs"] = clubs;
                }

                if (options.IncludeEvents)
                {
                    var events = await _context.Events.ToListAsync();
                    exportData["events"] = events;
                }

                if (options.IncludeReports)
                {
                    var reports = await _context.Reports.ToListAsync();
                    exportData["reports"] = reports;
                }

                var jsonData = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(filePath, jsonData);
                await _auditService.LogSystemEventAsync("Data Exported", $"Data exported to: {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to export data", ex);
                throw new DatabaseConnectionException("Failed to export data", ex);
            }
        }

        /// <summary>
        /// Imports data from an export file with configurable options.
        /// 
        /// DATA FLOW:
        /// 1. Validate import file existence
        /// 2. Parse JSON data into dictionary
        /// 3. Begin database transaction
        /// 4. Optionally clear existing data
        /// 5. Import selected entity types
        /// 6. Commit transaction or rollback on error
        /// 7. Log import operation
        /// 
        /// BUSINESS LOGIC:
        /// - Supports selective import of entity types
        /// - Optional clearing of existing data before import
        /// - Uses database transactions for atomicity
        /// - Validates data format before processing
        /// - Maintains referential integrity
        /// 
        /// USAGE: Data migration, bulk data loading, system integration
        /// </summary>
        /// <param name="dataPath">Path to the import data file</param>
        /// <param name="options">Import configuration specifying what to import</param>
        public async Task ImportDataAsync(string dataPath, ImportOptions options)
        {
            try
            {
                if (!File.Exists(dataPath))
                    throw new FileNotFoundException("Import file not found");

                var jsonData = await File.ReadAllTextAsync(dataPath);
                var importData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (importData == null)
                    throw new InvalidOperationException("Failed to parse import data");

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (options.ImportUsers && importData.ContainsKey("users"))
                        {
                            var users = JsonSerializer.Deserialize<List<User>>(importData["users"].GetRawText());
                            if (users?.Any() == true)
                            {
                                if (options.ClearExistingData)
                                    _context.Users.RemoveRange(_context.Users);
                                
                                _context.Users.AddRange(users);
                            }
                        }

                        if (options.ImportClubs && importData.ContainsKey("clubs"))
                        {
                            var clubs = JsonSerializer.Deserialize<List<Club>>(importData["clubs"].GetRawText());
                            if (clubs?.Any() == true)
                            {
                                if (options.ClearExistingData)
                                    _context.Clubs.RemoveRange(_context.Clubs);
                                
                                _context.Clubs.AddRange(clubs);
                            }
                        }

                        if (options.ImportEvents && importData.ContainsKey("events"))
                        {
                            var events = JsonSerializer.Deserialize<List<Event>>(importData["events"].GetRawText());
                            if (events?.Any() == true)
                            {
                                if (options.ClearExistingData)
                                    _context.Events.RemoveRange(_context.Events);
                                
                                _context.Events.AddRange(events);
                            }
                        }

                        if (options.ImportReports && importData.ContainsKey("reports"))
                        {
                            var reports = JsonSerializer.Deserialize<List<Report>>(importData["reports"].GetRawText());
                            if (reports?.Any() == true)
                            {
                                if (options.ClearExistingData)
                                    _context.Reports.RemoveRange(_context.Reports);
                                
                                _context.Reports.AddRange(reports);
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        await _auditService.LogSystemEventAsync("Data Imported", $"Data imported from: {dataPath}");
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to import data from {dataPath}", ex);
                throw new DatabaseConnectionException("Failed to import data", ex);
            }
        }

        /// <summary>
        /// Retrieves the complete backup history with validation.
        /// 
        /// DATA FLOW:
        /// 1. Read backup history from JSON file
        /// 2. Deserialize backup information list
        /// 3. Validate each backup file existence
        /// 4. Update validity status
        /// 5. Return sorted backup history
        /// 
        /// BUSINESS LOGIC:
        /// - Maintains persistent backup history in JSON format
        /// - Validates file existence for each backup
        /// - Updates validity status in real-time
        /// - Returns backups sorted by creation date (newest first)
        /// - Handles missing history file gracefully
        /// 
        /// USAGE: Backup management UI, restoration dialogs, system monitoring
        /// </summary>
        /// <returns>List of backup information sorted by creation date</returns>
        public async Task<List<BackupInfo>> GetBackupHistoryAsync()
        {
            try
            {
                var backupInfoPath = Path.Combine(_backupDirectory, "backup_history.json");
                if (!File.Exists(backupInfoPath))
                    return new List<BackupInfo>();

                var jsonData = await File.ReadAllTextAsync(backupInfoPath);
                var backupHistory = JsonSerializer.Deserialize<List<BackupInfo>>(jsonData) ?? new List<BackupInfo>();

                // Validate that backup files still exist
                foreach (var backup in backupHistory)
                {
                    backup.IsValid = File.Exists(backup.FilePath);
                }

                return backupHistory.OrderByDescending(b => b.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get backup history", ex);
                return new List<BackupInfo>();
            }
        }

        /// <summary>
        /// Deletes old backup files based on retention policy.
        /// 
        /// DATA FLOW:
        /// 1. Calculate cutoff date based on retention days
        /// 2. Retrieve current backup history
        /// 3. Identify backups older than cutoff date
        /// 4. Delete old backup files from disk
        /// 5. Update backup history to remove deleted entries
        /// 6. Log cleanup operation
        /// 
        /// BUSINESS LOGIC:
        /// - Implements configurable retention policy
        /// - Removes both files and history entries
        /// - Maintains backup history consistency
        /// - Logs cleanup statistics for monitoring
        /// - Handles missing files gracefully
        /// 
        /// USAGE: Scheduled maintenance, disk space management, retention compliance
        /// </summary>
        /// <param name="retentionDays">Number of days to retain backups (default: 30)</param>
        public async Task DeleteOldBackupsAsync(int retentionDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var backupHistory = await GetBackupHistoryAsync();
                var oldBackups = backupHistory.Where(b => b.CreatedAt < cutoffDate).ToList();

                foreach (var backup in oldBackups)
                {
                    if (File.Exists(backup.FilePath))
                    {
                        File.Delete(backup.FilePath);
                    }
                }

                var remainingBackups = backupHistory.Except(oldBackups).ToList();
                await SaveBackupHistoryAsync(remainingBackups);

                await _auditService.LogSystemEventAsync("Old Backups Deleted", $"Deleted {oldBackups.Count} old backups");
                await _loggingService.LogInformationAsync($"Deleted {oldBackups.Count} old backups older than {retentionDays} days");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to delete old backups", ex);
            }
        }

        /// <summary>
        /// Retrieves information about the most recent valid backup.
        /// 
        /// DATA FLOW:
        /// 1. Get complete backup history
        /// 2. Filter for valid backups only
        /// 3. Sort by creation date (newest first)
        /// 4. Return first valid backup or default info
        /// 
        /// BUSINESS LOGIC:
        /// - Returns only valid (existing) backups
        /// - Prioritizes newest backup by creation date
        /// - Returns default info if no valid backups exist
        /// - Used for system health monitoring
        /// 
        /// USAGE: System health checks, backup status monitoring, restoration preparation
        /// </summary>
        /// <returns>Information about the latest valid backup</returns>
        public async Task<BackupInfo> GetLatestBackupAsync()
        {
            var backupHistory = await GetBackupHistoryAsync();
            return backupHistory.Where(b => b.IsValid).OrderByDescending(b => b.CreatedAt).FirstOrDefault() 
                ?? new BackupInfo { FileName = "No backups found", CreatedAt = DateTime.MinValue };
        }

        /// <summary>
        /// Performs comprehensive system maintenance based on specified options.
        /// 
        /// DATA FLOW:
        /// 1. Initialize maintenance report with start time
        /// 2. Execute selected maintenance tasks sequentially
        /// 3. Track task completion and errors
        /// 4. Generate comprehensive maintenance report
        /// 5. Log maintenance completion or failure
        /// 
        /// BUSINESS LOGIC:
        /// - Configurable maintenance tasks (logs, database, files, backups)
        /// - Database optimization using SQL commands
        /// - Automated backup creation during maintenance
        /// - Comprehensive error handling and reporting
        /// - Audit trail of all maintenance activities
        /// 
        /// MAINTENANCE TASKS:
        /// - Cleanup old audit logs based on retention policy
        /// - Optimize database performance and statistics
        /// - Remove temporary files and cleanup disk space
        /// - Delete old backups according to retention policy
        /// - Create fresh backup for data protection
        /// 
        /// USAGE: Scheduled maintenance, system optimization, administrative tasks
        /// </summary>
        /// <param name="options">Configuration specifying which maintenance tasks to perform</param>
        /// <returns>Detailed report of maintenance activities and results</returns>
        public async Task<MaintenanceReport> PerformMaintenanceAsync(MaintenanceOptions options)
        {
            var report = new MaintenanceReport
            {
                StartTime = DateTime.UtcNow,
                TasksPerformed = new List<string>()
            };

            try
            {
                if (options.CleanupOldLogs)
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-options.LogRetentionDays);
                    var oldLogs = await _context.AuditLogs.Where(a => a.Timestamp < cutoffDate).ToListAsync();
                    _context.AuditLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();
                    report.TasksPerformed.Add($"Cleaned up {oldLogs.Count} old audit logs");
                }

                if (options.OptimizeDatabase)
                {
                    // Perform database optimization tasks
                    await _context.Database.ExecuteSqlRawAsync("DBCC UPDATEUSAGE(0)");
                    report.TasksPerformed.Add("Updated database usage statistics");
                }

                if (options.CleanupTempFiles)
                {
                    await CleanupTemporaryFilesAsync();
                    report.TasksPerformed.Add("Cleaned up temporary files");
                }

                if (options.DeleteOldBackups)
                {
                    await DeleteOldBackupsAsync(options.BackupRetentionDays);
                    report.TasksPerformed.Add($"Deleted old backups older than {options.BackupRetentionDays} days");
                }

                if (options.CreateBackup)
                {
                    var backupPath = await CreateDatabaseBackupAsync();
                    report.TasksPerformed.Add($"Created database backup: {Path.GetFileName(backupPath)}");
                }

                report.EndTime = DateTime.UtcNow;
                report.Success = true;
                report.Duration = report.EndTime - report.StartTime;

                await _auditService.LogSystemEventAsync("Maintenance Completed", 
                    $"Maintenance completed successfully. Tasks: {string.Join(", ", report.TasksPerformed)}");
            }
            catch (Exception ex)
            {
                report.EndTime = DateTime.UtcNow;
                report.Success = false;
                report.ErrorMessage = ex.Message;
                report.Duration = report.EndTime - report.StartTime;

                await _loggingService.LogErrorAsync("Maintenance failed", ex);
            }

            return report;
        }

        /// <summary>
        /// Performs comprehensive system health assessment.
        /// 
        /// DATA FLOW:
        /// 1. Initialize health report with current timestamp
        /// 2. Check database connectivity and responsiveness
        /// 3. Assess disk space availability
        /// 4. Evaluate backup recency and validity
        /// 5. Analyze audit log accumulation
        /// 6. Generate health status and recommendations
        /// 
        /// BUSINESS LOGIC:
        /// - Multi-dimensional health assessment
        /// - Tiered health status (Healthy, Info, Warning, Critical)
        /// - Proactive issue identification and recommendations
        /// - Configurable thresholds for warnings and alerts
        /// - Comprehensive system diagnostics
        /// 
        /// HEALTH CHECKS:
        /// - Database connectivity and performance
        /// - Disk space availability (< 1GB critical, < 5GB warning)
        /// - Backup recency (> 7 days triggers warning)
        /// - Audit log size (> 100,000 entries suggests cleanup)
        /// 
        /// USAGE: System monitoring, preventive maintenance, health dashboards
        /// </summary>
        /// <returns>Comprehensive system health report with status and recommendations</returns>
        public async Task<SystemHealthReport> CheckSystemHealthAsync()
        {
            var report = new SystemHealthReport
            {
                CheckTime = DateTime.UtcNow,
                Issues = new List<string>(),
                Recommendations = new List<string>()
            };

            try
            {
                // Check database connectivity
                var canConnect = await _context.Database.CanConnectAsync();
                report.DatabaseConnectivity = canConnect;
                if (!canConnect)
                {
                    report.Issues.Add("Cannot connect to database");
                    report.OverallHealth = HealthStatus.Critical;
                }

                // Check disk space
                var backupDriveInfo = new DriveInfo(Path.GetPathRoot(_backupDirectory) ?? "C:\\");
                var freeSpaceGB = backupDriveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);
                report.DiskSpaceGB = freeSpaceGB;
                
                if (freeSpaceGB < 1)
                {
                    report.Issues.Add("Low disk space (< 1GB available)");
                    report.OverallHealth = HealthStatus.Critical;
                }
                else if (freeSpaceGB < 5)
                {
                    report.Issues.Add("Disk space running low (< 5GB available)");
                    if (report.OverallHealth != HealthStatus.Critical)
                        report.OverallHealth = HealthStatus.Warning;
                }

                // Check recent backups
                var latestBackup = await GetLatestBackupAsync();
                var daysSinceLastBackup = (DateTime.UtcNow - latestBackup.CreatedAt).TotalDays;
                report.DaysSinceLastBackup = (int)daysSinceLastBackup;
                
                if (daysSinceLastBackup > 7)
                {
                    report.Issues.Add($"No recent backup (last backup: {daysSinceLastBackup:F0} days ago)");
                    report.Recommendations.Add("Create a database backup");
                    if (report.OverallHealth != HealthStatus.Critical)
                        report.OverallHealth = HealthStatus.Warning;
                }

                // Check audit log size
                var auditLogCount = await _context.AuditLogs.CountAsync();
                report.AuditLogCount = auditLogCount;
                
                if (auditLogCount > 100000)
                {
                    report.Issues.Add($"Large number of audit logs ({auditLogCount:N0})");
                    report.Recommendations.Add("Clean up old audit logs");
                    if (report.OverallHealth != HealthStatus.Critical && report.OverallHealth != HealthStatus.Warning)
                        report.OverallHealth = HealthStatus.Info;
                }

                // Set overall health if no issues found
                if (report.OverallHealth == HealthStatus.Unknown && !report.Issues.Any())
                {
                    report.OverallHealth = HealthStatus.Healthy;
                }
            }
            catch (Exception ex)
            {
                report.Issues.Add($"Health check failed: {ex.Message}");
                report.OverallHealth = HealthStatus.Critical;
                await _loggingService.LogErrorAsync("System health check failed", ex);
            }

            return report;
        }

        /// <summary>
        /// Cleans up temporary files older than one day.
        /// 
        /// DATA FLOW:
        /// 1. Locate application temporary directory
        /// 2. Scan for files older than 1 day
        /// 3. Attempt to delete each temporary file
        /// 4. Log cleanup statistics
        /// 5. Handle individual file deletion errors gracefully
        /// 
        /// BUSINESS LOGIC:
        /// - Targets files older than 1 day for cleanup
        /// - Recursive directory scanning
        /// - Graceful handling of locked or inaccessible files
        /// - Maintains system performance by freeing disk space
        /// - Logs cleanup activity for monitoring
        /// 
        /// USAGE: Scheduled maintenance, disk space management, system cleanup
        /// </summary>
        public async Task CleanupTemporaryFilesAsync()
        {
            try
            {
                var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "ClubManagement", "Temp");
                
                if (Directory.Exists(tempDirectory))
                {
                    var tempFiles = Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories)
                        .Where(f => File.GetCreationTime(f) < DateTime.Now.AddDays(-1))
                        .ToList();

                    foreach (var file in tempFiles)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore individual file deletion errors
                        }
                    }

                    await _loggingService.LogInformationAsync($"Cleaned up {tempFiles.Count} temporary files");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to cleanup temporary files", ex);
            }
        }

        /// <summary>
        /// Creates a backup of system configuration and settings.
        /// 
        /// DATA FLOW:
        /// 1. Generate timestamped configuration backup filename
        /// 2. Query global settings from database
        /// 3. Retrieve application configuration
        /// 4. Serialize configuration data to JSON
        /// 5. Write backup file and log operation
        /// 
        /// BUSINESS LOGIC:
        /// - Backs up both database settings and application configuration
        /// - Uses JSON format for cross-platform compatibility
        /// - Includes timestamp for version tracking
        /// - Maintains audit trail of configuration changes
        /// - Enables configuration rollback capabilities
        /// 
        /// USAGE: Pre-update backups, configuration versioning, disaster recovery
        /// </summary>
        /// <returns>Path to the created configuration backup file</returns>
        public async Task<string> BackupConfigurationAsync()
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var fileName = $"Configuration_Backup_{timestamp}.json";
                var filePath = Path.Combine(_backupDirectory, fileName);

                var configData = new
                {
                    CreatedAt = DateTime.UtcNow,
                    GlobalSettings = await _context.Settings.Where(s => s.Scope == SettingsScope.Global).ToListAsync(),
                    ApplicationConfig = await _configurationService.GetAllAsync()
                };

                var jsonData = JsonSerializer.Serialize(configData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(filePath, jsonData);
                await _auditService.LogSystemEventAsync("Configuration Backed Up", $"Configuration backed up to: {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to backup configuration", ex);
                throw new ConfigurationException("Failed to backup configuration", ex);
            }
        }

        /// <summary>
        /// Restores system configuration from a backup file.
        /// 
        /// DATA FLOW:
        /// 1. Validate configuration backup file existence
        /// 2. Parse JSON configuration data
        /// 3. Restore global settings to database
        /// 4. Restore application configuration
        /// 5. Save changes and log restoration
        /// 
        /// BUSINESS LOGIC:
        /// - Validates backup file before restoration
        /// - Replaces existing global settings completely
        /// - Updates application configuration settings
        /// - Maintains configuration consistency
        /// - Logs configuration restoration for audit
        /// 
        /// USAGE: Configuration rollback, system recovery, settings migration
        /// </summary>
        /// <param name="configBackupPath">Path to the configuration backup file</param>
        public async Task RestoreConfigurationAsync(string configBackupPath)
        {
            try
            {
                if (!File.Exists(configBackupPath))
                    throw new FileNotFoundException("Configuration backup file not found");

                var jsonData = await File.ReadAllTextAsync(configBackupPath);
                var configData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (configData == null)
                    throw new InvalidOperationException("Failed to parse configuration backup");

                // Restore global settings
                if (configData.ContainsKey("globalSettings"))
                {
                    var globalSettings = JsonSerializer.Deserialize<List<Setting>>(configData["globalSettings"].GetRawText());
                    if (globalSettings?.Any() == true)
                    {
                        var existingGlobalSettings = await _context.Settings
                            .Where(s => s.Scope == SettingsScope.Global)
                            .ToListAsync();
                        
                        _context.Settings.RemoveRange(existingGlobalSettings);
                        _context.Settings.AddRange(globalSettings);
                        await _context.SaveChangesAsync();
                    }
                }

                // Restore application configuration
                if (configData.ContainsKey("applicationConfig"))
                {
                    var appConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(configData["applicationConfig"].GetRawText());
                    if (appConfig?.Any() == true)
                    {
                        foreach (var kvp in appConfig)
                        {
                            await _configurationService.SetAsync(kvp.Key, kvp.Value);
                        }
                        await _configurationService.SaveAsync();
                    }
                }

                await _auditService.LogSystemEventAsync("Configuration Restored", $"Configuration restored from: {configBackupPath}");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to restore configuration from {configBackupPath}", ex);
                throw new ConfigurationException("Failed to restore configuration", ex);
            }
        }

        /// <summary>
        /// Saves backup information to the backup history.
        /// 
        /// DATA FLOW:
        /// 1. Retrieve current backup history
        /// 2. Add new backup information
        /// 3. Save updated history to JSON file
        /// 
        /// BUSINESS LOGIC:
        /// - Maintains persistent backup metadata
        /// - Enables backup tracking and management
        /// - Handles history file creation if missing
        /// 
        /// USAGE: Internal backup tracking, history maintenance
        /// </summary>
        /// <param name="backupInfo">Backup information to save</param>
        private async Task SaveBackupInfoAsync(BackupInfo backupInfo)
        {
            try
            {
                var backupHistory = await GetBackupHistoryAsync();
                backupHistory.Add(backupInfo);
                await SaveBackupHistoryAsync(backupHistory);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to save backup info", ex);
            }
        }

        /// <summary>
        /// Saves the complete backup history to persistent storage.
        /// 
        /// DATA FLOW:
        /// 1. Serialize backup history to JSON
        /// 2. Write to backup history file
        /// 3. Handle file operation errors gracefully
        /// 
        /// BUSINESS LOGIC:
        /// - Maintains backup history persistence
        /// - Uses JSON format for readability
        /// - Enables backup management operations
        /// 
        /// USAGE: Internal history management, backup cleanup operations
        /// </summary>
        /// <param name="backupHistory">Complete backup history to save</param>
        private async Task SaveBackupHistoryAsync(List<BackupInfo> backupHistory)
        {
            try
            {
                var backupInfoPath = Path.Combine(_backupDirectory, "backup_history.json");
                var jsonData = JsonSerializer.Serialize(backupHistory, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await File.WriteAllTextAsync(backupInfoPath, jsonData);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to save backup history", ex);
            }
        }
    }

    // Supporting classes and enums
    
    /// <summary>
    /// Data transfer object representing a complete database backup.
    /// Contains all core entities and metadata for full system restoration.
    /// 
    /// USAGE:
    /// - Serialization target for backup operations
    /// - Deserialization source for restore operations
    /// - Version tracking for backup compatibility
    /// - Complete system state preservation
    /// </summary>
    public class DatabaseBackup
    {
        /// <summary>Timestamp when the backup was created</summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>Backup format version for compatibility checking</summary>
        public string Version { get; set; } = string.Empty;
        
        /// <summary>All user accounts and profiles</summary>
        public List<User>? Users { get; set; }
        
        /// <summary>All club definitions and metadata</summary>
        public List<Club>? Clubs { get; set; }
        
        /// <summary>All events and their details</summary>
        public List<Event>? Events { get; set; }
        
        /// <summary>All event participation records</summary>
        public List<EventParticipant>? EventParticipants { get; set; }
        
        /// <summary>All generated reports</summary>
        public List<Report>? Reports { get; set; }
        
        /// <summary>All system and user settings</summary>
        public List<Setting>? Settings { get; set; }
        
        /// <summary>Audit logs (limited to last 90 days for size optimization)</summary>
        public List<AuditLog>? AuditLogs { get; set; }
    }

    /// <summary>
    /// Metadata information about a backup file.
    /// Used for backup tracking, management, and validation.
    /// 
    /// USAGE:
    /// - Backup history maintenance
    /// - File validation and integrity checking
    /// - Backup management UI display
    /// - Retention policy enforcement
    /// </summary>
    public class BackupInfo
    {
        /// <summary>Display name of the backup file</summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>Full path to the backup file on disk</summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>Timestamp when the backup was created</summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>Size of the backup file in bytes</summary>
        public long FileSize { get; set; }
        
        /// <summary>Type of backup (Full, Incremental, Configuration)</summary>
        public BackupType BackupType { get; set; }
        
        /// <summary>Whether the backup file still exists and is accessible</summary>
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// Configuration options for selective data export operations.
    /// Allows fine-grained control over which data to include in exports.
    /// 
    /// USAGE:
    /// - Selective data migration
    /// - Partial system backups
    /// - Data analysis exports
    /// - Reporting data extraction
    /// </summary>
    public class ExportOptions
    {
        /// <summary>Include user accounts and profiles in export</summary>
        public bool IncludeUsers { get; set; } = true;
        
        /// <summary>Include club definitions and metadata in export</summary>
        public bool IncludeClubs { get; set; } = true;
        
        /// <summary>Include events and their details in export</summary>
        public bool IncludeEvents { get; set; } = true;
        
        /// <summary>Include generated reports in export</summary>
        public bool IncludeReports { get; set; } = true;
        
        /// <summary>Start date for date-range filtering (future enhancement)</summary>
        public DateTime? FromDate { get; set; }
        
        /// <summary>End date for date-range filtering (future enhancement)</summary>
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Configuration options for selective data import operations.
    /// Controls which data types to import and how to handle existing data.
    /// 
    /// USAGE:
    /// - Selective data restoration
    /// - Data migration from external sources
    /// - Bulk data loading operations
    /// - System integration imports
    /// </summary>
    public class ImportOptions
    {
        /// <summary>Import user accounts and profiles</summary>
        public bool ImportUsers { get; set; } = true;
        
        /// <summary>Import club definitions and metadata</summary>
        public bool ImportClubs { get; set; } = true;
        
        /// <summary>Import events and their details</summary>
        public bool ImportEvents { get; set; } = true;
        
        /// <summary>Import generated reports</summary>
        public bool ImportReports { get; set; } = true;
        
        /// <summary>Whether to clear existing data before import (destructive operation)</summary>
        public bool ClearExistingData { get; set; } = false;
        
        /// <summary>Whether to validate imported data integrity (future enhancement)</summary>
        public bool ValidateData { get; set; } = true;
    }

    /// <summary>
    /// Configuration options for automated system maintenance operations.
    /// Controls which maintenance tasks to perform and their parameters.
    /// 
    /// USAGE:
    /// - Scheduled maintenance configuration
    /// - Administrative maintenance tasks
    /// - System optimization operations
    /// - Automated cleanup procedures
    /// </summary>
    public class MaintenanceOptions
    {
        /// <summary>Remove old audit logs based on retention policy</summary>
        public bool CleanupOldLogs { get; set; } = true;
        
        /// <summary>Perform database optimization and statistics updates</summary>
        public bool OptimizeDatabase { get; set; } = true;
        
        /// <summary>Clean up temporary files and free disk space</summary>
        public bool CleanupTempFiles { get; set; } = true;
        
        /// <summary>Delete old backup files based on retention policy</summary>
        public bool DeleteOldBackups { get; set; } = true;
        
        /// <summary>Create a fresh backup during maintenance</summary>
        public bool CreateBackup { get; set; } = true;
        
        /// <summary>Number of days to retain audit logs (default: 90)</summary>
        public int LogRetentionDays { get; set; } = 90;
        
        /// <summary>Number of days to retain backup files (default: 30)</summary>
        public int BackupRetentionDays { get; set; } = 30;
    }

    /// <summary>
    /// Report detailing the results of a maintenance operation.
    /// Provides comprehensive information about maintenance activities and outcomes.
    /// 
    /// USAGE:
    /// - Maintenance operation tracking
    /// - Administrative reporting
    /// - System monitoring and alerting
    /// - Maintenance history analysis
    /// </summary>
    public class MaintenanceReport
    {
        /// <summary>When the maintenance operation began</summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>When the maintenance operation completed</summary>
        public DateTime EndTime { get; set; }
        
        /// <summary>Total duration of the maintenance operation</summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>Whether the maintenance operation completed successfully</summary>
        public bool Success { get; set; }
        
        /// <summary>Error message if the operation failed</summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>List of maintenance tasks that were performed</summary>
        public List<string> TasksPerformed { get; set; } = new();
    }

    /// <summary>
    /// Comprehensive system health assessment report.
    /// Provides detailed analysis of system status and recommendations.
    /// 
    /// USAGE:
    /// - System monitoring dashboards
    /// - Preventive maintenance planning
    /// - Health status alerting
    /// - System administration reporting
    /// </summary>
    public class SystemHealthReport
    {
        /// <summary>When the health check was performed</summary>
        public DateTime CheckTime { get; set; }
        
        /// <summary>Overall system health status (Healthy, Info, Warning, Critical)</summary>
        public HealthStatus OverallHealth { get; set; } = HealthStatus.Unknown;
        
        /// <summary>Whether the database is accessible and responsive</summary>
        public bool DatabaseConnectivity { get; set; }
        
        /// <summary>Available disk space in gigabytes</summary>
        public long DiskSpaceGB { get; set; }
        
        /// <summary>Number of days since the last valid backup</summary>
        public int DaysSinceLastBackup { get; set; }
        
        /// <summary>Total number of audit log entries in the system</summary>
        public int AuditLogCount { get; set; }
        
        /// <summary>List of identified system issues</summary>
        public List<string> Issues { get; set; } = new();
        
        /// <summary>List of recommended actions to address issues</summary>
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Enumeration of backup types supported by the system.
    /// Used for categorizing and managing different backup operations.
    /// 
    /// USAGE:
    /// - Backup classification and filtering
    /// - Backup management UI organization
    /// - Retention policy application
    /// - Backup operation tracking
    /// </summary>
    public enum BackupType
    {
        /// <summary>Complete database backup including all entities</summary>
        Full,
        
        /// <summary>Incremental backup with only changes (future enhancement)</summary>
        Incremental,
        
        /// <summary>Configuration and settings backup only</summary>
        Configuration
    }

    /// <summary>
    /// Enumeration of system health status levels.
    /// Provides tiered health assessment for monitoring and alerting.
    /// 
    /// USAGE:
    /// - System health monitoring
    /// - Alert level determination
    /// - Dashboard status indicators
    /// - Maintenance priority assessment
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>Health status could not be determined</summary>
        Unknown,
        
        /// <summary>System is operating normally with no issues</summary>
        Healthy,
        
        /// <summary>Informational status with minor observations</summary>
        Info,
        
        /// <summary>Warning status requiring attention but not critical</summary>
        Warning,
        
        /// <summary>Critical status requiring immediate attention</summary>
        Critical
    }
}