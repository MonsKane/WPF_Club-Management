using ClubManagementApp.Configuration;
using ClubManagementApp.Data;
using ClubManagementApp.Helpers;
using ClubManagementApp.Models;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;

namespace ClubManagementApp.Services
{
    public interface IDataImportExportService
    {
        // Export operations
        Task<string> ExportUsersAsync(ExportFormat format, ExportOptions? options = null);
        Task<string> ExportClubsAsync(ExportFormat format, ExportOptions? options = null);
        Task<string> ExportEventsAsync(ExportFormat format, ExportOptions? options = null);
        Task<string> ExportReportsAsync(ExportFormat format, ExportOptions? options = null);
        Task<string> ExportAllDataAsync(ExportFormat format, ExportOptions? options = null);

        // Import operations
        Task<ImportResult> ImportUsersAsync(string filePath, ImportFormat format, ImportOptions? options = null);
        Task<ImportResult> ImportClubsAsync(string filePath, ImportFormat format, ImportOptions? options = null);
        Task<ImportResult> ImportEventsAsync(string filePath, ImportFormat format, ImportOptions? options = null);
        Task<ImportResult> ImportAllDataAsync(string filePath, ImportFormat format, ImportOptions? options = null);

        // Template generation
        Task<string> GenerateImportTemplateAsync(DataType dataType, ExportFormat format);
        Task<List<string>> GetSupportedFormatsAsync();

        // Data validation
        Task<ValidationResult> ValidateImportDataAsync(string filePath, ImportFormat format, DataType dataType);
        Task<List<DataConflict>> CheckForConflictsAsync(string filePath, ImportFormat format, DataType dataType);

        // Batch operations
        Task<string> ExportFilteredDataAsync(DataFilter filter, ExportFormat format, ExportOptions? options = null);
        Task<ImportResult> ImportWithMappingAsync(string filePath, ImportFormat format, FieldMapping mapping, ImportOptions? options = null);

        // Scheduled exports
        Task ScheduleExportAsync(ExportSchedule schedule);
        Task<List<ExportSchedule>> GetScheduledExportsAsync();
        Task CancelScheduledExportAsync(string scheduleId);
        Task ProcessScheduledExportsAsync();

        // Data transformation
        Task<string> TransformDataAsync(string inputPath, ImportFormat inputFormat, ExportFormat outputFormat, TransformOptions? options = null);
        Task<string> MergeDataFilesAsync(List<string> filePaths, ExportFormat outputFormat, MergeOptions? options = null);
    }

    public class DataImportExportService : IDataImportExportService
    {
        private readonly ClubManagementDbContext _context;
        private readonly ILoggingService _loggingService;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configurationService;
        private readonly ISecurityService _securityService;

        public DataImportExportService(
            ClubManagementDbContext context,
            ILoggingService loggingService,
            IAuditService auditService,
            IConfiguration configurationService,
            ISecurityService securityService)
        {
            _context = context;
            _loggingService = loggingService;
            _auditService = auditService;
            _configurationService = configurationService;
            _securityService = securityService;
        }

        public async Task<string> ExportUsersAsync(ExportFormat format, ExportOptions? options = null)
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Club)
                    .Include(u => u.EventParticipations)
                    .ToListAsync();

                var exportData = users.Select(u => new
                {
                    u.UserID,
                    u.FullName,
                    u.Email,
                    u.StudentID,
                    SystemRole = u.SystemRole.ToString(),
                    ActivityLevel = "Active",
                    JoinDate = u.CreatedAt,
                    u.IsActive,
                    TwoFactorEnabled = false,
                    ClubName = u.Club?.Name,
                    EventParticipations = u.EventParticipations.Count
                }).ToList();

                var filePath = await ExportDataAsync(exportData, format, "users", options);
                await _auditService.LogSystemEventAsync("Data Export", $"Users exported to {format} format");

                return filePath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to export users", ex);
                throw;
            }
        }

        public async Task<string> ExportClubsAsync(ExportFormat format, ExportOptions? options = null)
        {
            try
            {
                var clubs = await _context.Clubs
                    .Include(c => c.ClubMembers)
                    .Include(c => c.Events)
                    .ToListAsync();

                var exportData = clubs.Select(c => new
                {
                    c.ClubID,
                    c.Name,
                    c.Description,
                    CreatedDate = c.EstablishedDate,
                    c.IsActive,
                    MemberCount = c.ClubMembers.Count,
                    EventCount = c.Events.Count
                }).ToList();

                var filePath = await ExportDataAsync(exportData, format, "clubs", options);
                await _auditService.LogSystemEventAsync("Data Export", $"Clubs exported to {format} format");

                return filePath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to export clubs", ex);
                throw;
            }
        }

        public async Task<string> ExportEventsAsync(ExportFormat format, ExportOptions? options = null)
        {
            try
            {
                var events = await _context.Events
                    .Include(e => e.Club)
                    .Include(e => e.Participants)
                    .ToListAsync();

                var exportData = events.Select(e => new
                {
                    e.EventID,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.MaxParticipants,
                    e.RegistrationDeadline,
                    e.IsActive,
                    ClubName = e.Club?.Name,
                    ParticipantCount = e.Participants.Count,
                    AttendanceCount = e.Participants.Count(p => p.Status == AttendanceStatus.Attended)
                }).ToList();

                var filePath = await ExportDataAsync(exportData, format, "events", options);
                await _auditService.LogSystemEventAsync("Data Export", $"Events exported to {format} format");

                return filePath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to export events", ex);
                throw;
            }
        }

        public async Task<string> ExportReportsAsync(ExportFormat format, ExportOptions? options = null)
        {
            try
            {
                var reports = await _context.Reports
                    .Include(r => r.GeneratedByUser)
                    .Include(r => r.Club)
                    .ToListAsync();

                var exportData = reports.Select(r => new
                {
                    r.ReportID,
                    r.Title,
                    Type = r.Type.ToString(),
                    r.Content,
                    r.GeneratedDate,
                    r.Semester,
                    GeneratedByUser = r.GeneratedByUser?.FullName,
                    ClubName = r.Club?.Name
                }).ToList();

                var filePath = await ExportDataAsync(exportData, format, "reports", options);
                await _auditService.LogSystemEventAsync("Data Export", $"Reports exported to {format} format");

                return filePath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to export reports", ex);
                throw;
            }
        }

        public async Task<string> ExportAllDataAsync(ExportFormat format, ExportOptions? options = null)
        {
            try
            {
                var allData = new
                {
                    Users = await GetUsersForExportAsync(),
                    Clubs = await GetClubsForExportAsync(),
                    Events = await GetEventsForExportAsync(),
                    Reports = await GetReportsForExportAsync(),
                    ExportInfo = new
                    {
                        ExportDate = DateTime.UtcNow,
                        Version = "1.0",
                        TotalRecords = await GetTotalRecordCountAsync()
                    }
                };

                var filePath = await ExportDataAsync(allData, format, "complete_backup", options);
                await _auditService.LogSystemEventAsync("Complete Data Export", $"All data exported to {format} format");

                return filePath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to export all data", ex);
                throw;
            }
        }

        public async Task<ImportResult> ImportUsersAsync(string filePath, ImportFormat format, ImportOptions? options = null)
        {
            try
            {
                var result = new ImportResult { DataType = DataType.Users };

                var userData = await ReadImportDataAsync<UserImportModel>(filePath, format);

                foreach (var userModel in userData)
                {
                    try
                    {
                        var validationResult = await ValidateUserImportModelAsync(userModel);
                        if (!validationResult.IsValid)
                        {
                            result.ErrorRecords++;
                            result.Errors.AddRange(validationResult.Errors);
                            continue;
                        }

                        var existingUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == userModel.Email || u.StudentID == userModel.StudentID);

                        if (existingUser != null && options?.ImportUsers == false)
                        {
                            result.SkippedRecords++;
                            continue;
                        }

                        var user = await CreateUserFromImportModelAsync(userModel);

                        if (existingUser == null)
                        {
                            _context.Users.Add(user);
                            result.ImportedRecords++;
                        }
                        else
                        {
                            result.ErrorRecords++;
                            result.Errors.Add($"Duplicate user: {userModel.Email}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorRecords++;
                        result.Errors.Add($"Error importing user {userModel.Email}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                await _auditService.LogSystemEventAsync("Data Import", $"Users imported: {result.ImportedRecords} new, {result.UpdatedRecords} updated");

                return result;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to import users", ex);
                throw;
            }
        }

        public async Task<ImportResult> ImportClubsAsync(string filePath, ImportFormat format, ImportOptions? options = null)
        {
            try
            {
                var result = new ImportResult { DataType = DataType.Clubs };

                var clubData = await ReadImportDataAsync<ClubImportModel>(filePath, format);

                foreach (var clubModel in clubData)
                {
                    try
                    {
                        var existingClub = await _context.Clubs
                            .FirstOrDefaultAsync(c => c.Name == clubModel.Name);

                        if (existingClub != null && options?.ImportClubs == false)
                        {
                            result.SkippedRecords++;
                            continue;
                        }

                        var club = new Club
                        {
                            Description = clubModel.Description ?? "",
                            EstablishedDate = clubModel.EstablishedDate ?? DateTime.Now
                        };

                        if (existingClub == null)
                        {
                            _context.Clubs.Add(club);
                            result.ImportedRecords++;
                        }
                        else
                        {
                            result.ErrorRecords++;
                            result.Errors.Add($"Duplicate club: {clubModel.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorRecords++;
                        result.Errors.Add($"Error importing club {clubModel.Name}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                await _auditService.LogSystemEventAsync("Data Import", $"Clubs imported: {result.ImportedRecords} new, {result.UpdatedRecords} updated");

                return result;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to import clubs", ex);
                throw;
            }
        }

        public async Task<ImportResult> ImportEventsAsync(string filePath, ImportFormat format, ImportOptions? options = null)
        {
            try
            {
                var result = new ImportResult { DataType = DataType.Events };

                var eventData = await ReadImportDataAsync<EventImportModel>(filePath, format);

                foreach (var eventModel in eventData)
                {
                    try
                    {
                        var club = await _context.Clubs.FirstOrDefaultAsync(c => c.Name == eventModel.ClubName);
                        if (club == null)
                        {
                            result.ErrorRecords++;
                            result.Errors.Add($"Club not found for event {eventModel.Name}: {eventModel.ClubName}");
                            continue;
                        }

                        var existingEvent = await _context.Events
                            .FirstOrDefaultAsync(e => e.Name == eventModel.Name && e.EventDate == eventModel.Date);

                        if (existingEvent != null && options?.ImportEvents == false)
                        {
                            result.SkippedRecords++;
                            continue;
                        }

                        var eventEntity = new Event
                        {
                            Name = eventModel.Name,
                            Description = eventModel.Description ?? "",
                            EventDate = eventModel.Date ?? DateTime.Now,
                            Location = eventModel.Location ?? "",
                            MaxParticipants = eventModel.MaxParticipants,
                            RegistrationDeadline = eventModel.RegistrationDeadline,
                            IsActive = eventModel.IsActive ?? true,
                            ClubID = club.ClubID
                        };

                        if (existingEvent == null)
                        {
                            _context.Events.Add(eventEntity);
                            result.ImportedRecords++;
                        }
                        else
                        {
                            result.ErrorRecords++;
                            result.Errors.Add($"Duplicate event: {eventModel.Name} on {eventModel.Date}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorRecords++;
                        result.Errors.Add($"Error importing event {eventModel.Name}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                await _auditService.LogSystemEventAsync("Data Import", $"Events imported: {result.ImportedRecords} new, {result.UpdatedRecords} updated");

                return result;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to import events", ex);
                throw;
            }
        }

        public async Task<ImportResult> ImportAllDataAsync(string filePath, ImportFormat format, ImportOptions? options = null)
        {
            try
            {
                var result = new ImportResult { DataType = DataType.All };

                // Read the complete backup file
                var allData = await ReadImportDataAsync<CompleteBackupModel>(filePath, format);
                var backupData = allData.FirstOrDefault();

                if (backupData == null)
                {
                    throw new InvalidOperationException("Invalid backup file format");
                }

                // Import in order: Clubs -> Users -> Events -> Reports
                if (backupData.Clubs?.Any() == true)
                {
                    var clubResult = await ImportClubDataAsync(backupData.Clubs, options);
                    result.ImportedRecords += clubResult.ImportedRecords;
                    result.UpdatedRecords += clubResult.UpdatedRecords;
                    result.ErrorRecords += clubResult.ErrorRecords;
                    result.Errors.AddRange(clubResult.Errors);
                }

                if (backupData.Users?.Any() == true)
                {
                    var userResult = await ImportUserDataAsync(backupData.Users, options);
                    result.ImportedRecords += userResult.ImportedRecords;
                    result.UpdatedRecords += userResult.UpdatedRecords;
                    result.ErrorRecords += userResult.ErrorRecords;
                    result.Errors.AddRange(userResult.Errors);
                }

                if (backupData.Events?.Any() == true)
                {
                    var eventResult = await ImportEventDataAsync(backupData.Events, options);
                    result.ImportedRecords += eventResult.ImportedRecords;
                    result.UpdatedRecords += eventResult.UpdatedRecords;
                    result.ErrorRecords += eventResult.ErrorRecords;
                    result.Errors.AddRange(eventResult.Errors);
                }

                await _auditService.LogSystemEventAsync("Complete Data Import",
                    $"All data imported: {result.ImportedRecords} new, {result.UpdatedRecords} updated, {result.ErrorRecords} errors");

                return result;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to import all data", ex);
                throw;
            }
        }

        public async Task<string> GenerateImportTemplateAsync(DataType dataType, ExportFormat format)
        {
            try
            {
                object templateData = dataType switch
                {
                    DataType.Users => new List<UserImportModel>
                    {
                        new UserImportModel
                        {
                            FullName = "John Doe",
                            Email = "john.doe@example.com",
                            StudentID = "STU001",
                            SystemRole = "Member",
                            ActivityLevel = "Active",
                            JoinDate = DateTime.Now,
                            IsActive = true,
                            TwoFactorEnabled = false
                        }
                    },
                    DataType.Clubs => new List<ClubImportModel>
                    {
                        new ClubImportModel
                        {
                            Name = "Sample Club",
                            Description = "This is a sample club description",
                            EstablishedDate = DateTime.Now,
                            IsActive = true
                        }
                    },
                    DataType.Events => new List<EventImportModel>
                    {
                        new EventImportModel
                        {
                            Name = "Sample Event",
                            Description = "This is a sample event",
                            Date = DateTime.Now.AddDays(7),
                            Location = "Conference Room A",
                            MaxParticipants = 50,
                            RegistrationDeadline = DateTime.Now.AddDays(5),
                            IsActive = true,
                            ClubName = "Sample Club"
                        }
                    },
                    _ => throw new ArgumentException($"Unsupported data type: {dataType}")
                };

                var filePath = await ExportDataAsync(templateData, format, $"{dataType.ToString().ToLower()}_template", null);
                await _loggingService.LogInformationAsync($"Import template generated for {dataType}: {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to generate import template for {dataType}", ex);
                throw;
            }
        }

        public Task<List<string>> GetSupportedFormatsAsync()
        {
            return Task.FromResult(new List<string> { "JSON", "CSV", "XML" });
        }

        public async Task<ValidationResult> ValidateImportDataAsync(string filePath, ImportFormat format, DataType dataType)
        {
            try
            {
                var result = new ValidationResult { IsValid = true };

                if (!File.Exists(filePath))
                {
                    result.IsValid = false;
                    result.Errors.Add("File does not exist");
                    return result;
                }

                // Basic file format validation
                try
                {
                    switch (format)
                    {
                        case ImportFormat.JSON:
                            var jsonContent = await File.ReadAllTextAsync(filePath);
                            JsonDocument.Parse(jsonContent);
                            break;
                        case ImportFormat.XML:
                            var xmlDoc = new XmlDocument();
                            xmlDoc.Load(filePath);
                            break;
                        case ImportFormat.CSV:
                            {
                                using var reader = new StringReader(await File.ReadAllTextAsync(filePath));
                                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                                csv.Read();
                                csv.ReadHeader();
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Invalid file format: {ex.Message}");
                    return result;
                }

                // Data-specific validation
                switch (dataType)
                {
                    case DataType.Users:
                        await ValidateUserDataAsync(filePath, format, result);
                        break;
                    case DataType.Clubs:
                        await ValidateClubDataAsync(filePath, format, result);
                        break;
                    case DataType.Events:
                        await ValidateEventDataAsync(filePath, format, result);
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to validate import data", ex);
                return new ValidationResult { IsValid = false, Errors = { ex.Message } };
            }
        }

        public async Task<List<DataConflict>> CheckForConflictsAsync(string filePath, ImportFormat format, DataType dataType)
        {
            try
            {
                var conflicts = new List<DataConflict>();

                switch (dataType)
                {
                    case DataType.Users:
                        var userData = await ReadImportDataAsync<UserImportModel>(filePath, format);
                        foreach (var user in userData)
                        {
                            var existingUser = await _context.Users
                                .FirstOrDefaultAsync(u => u.Email == user.Email || u.StudentID == user.StudentID);

                            if (existingUser != null)
                            {
                                conflicts.Add(new DataConflict
                                {
                                    Type = ConflictType.Duplicate,
                                    Field = "Email/StudentID",
                                    ImportValue = $"{user.Email}/{user.StudentID}",
                                    ExistingValue = $"{existingUser.Email}/{existingUser.StudentID}",
                                    Description = "User with same email or student ID already exists"
                                });
                            }
                        }
                        break;

                    case DataType.Clubs:
                        var clubData = await ReadImportDataAsync<ClubImportModel>(filePath, format);
                        foreach (var club in clubData)
                        {
                            var existingClub = await _context.Clubs
                                .FirstOrDefaultAsync(c => c.Name == club.Name);

                            if (existingClub != null)
                            {
                                conflicts.Add(new DataConflict
                                {
                                    Type = ConflictType.Duplicate,
                                    Field = "Name",
                                    ImportValue = club.Name,
                                    ExistingValue = existingClub.Name,
                                    Description = "Club with same name already exists"
                                });
                            }
                        }
                        break;

                    case DataType.Events:
                        var eventData = await ReadImportDataAsync<EventImportModel>(filePath, format);
                        foreach (var eventModel in eventData)
                        {
                            var existingEvent = await _context.Events
                                .FirstOrDefaultAsync(e => e.Name == eventModel.Name && e.EventDate == eventModel.Date);

                            if (existingEvent != null)
                            {
                                conflicts.Add(new DataConflict
                                {
                                    Type = ConflictType.Duplicate,
                                    Field = "Name/Date",
                                    ImportValue = $"{eventModel.Name} on {eventModel.Date}",
                                    ExistingValue = $"{existingEvent.Name} on {existingEvent.EventDate}",
                                    Description = "Event with same name and date already exists"
                                });
                            }
                        }
                        break;
                }

                return conflicts;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to check for conflicts", ex);
                return new List<DataConflict>();
            }
        }

        public async Task<string> ExportFilteredDataAsync(DataFilter filter, ExportFormat format, ExportOptions? options = null)
        {
            try
            {
                object filteredData = filter.DataType switch
                {
                    DataType.Users => await GetFilteredUsersAsync(filter),
                    DataType.Clubs => await GetFilteredClubsAsync(filter),
                    DataType.Events => await GetFilteredEventsAsync(filter),
                    DataType.Reports => await GetFilteredReportsAsync(filter),
                    _ => throw new ArgumentException($"Unsupported data type: {filter.DataType}")
                };

                var filePath = await ExportDataAsync(filteredData, format, $"filtered_{filter.DataType.ToString().ToLower()}", options);
                await _auditService.LogSystemEventAsync("Filtered Data Export", $"Filtered {filter.DataType} exported to {format} format");

                return filePath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to export filtered data", ex);
                throw;
            }
        }

        public async Task<ImportResult> ImportWithMappingAsync(string filePath, ImportFormat format, FieldMapping mapping, ImportOptions? options = null)
        {
            try
            {
                // This would implement custom field mapping logic
                // For now, we'll use the standard import methods
                return mapping.DataType switch
                {
                    DataType.Users => await ImportUsersAsync(filePath, format, options),
                    DataType.Clubs => await ImportClubsAsync(filePath, format, options),
                    DataType.Events => await ImportEventsAsync(filePath, format, options),
                    _ => throw new ArgumentException($"Unsupported data type: {mapping.DataType}")
                };
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to import with mapping", ex);
                throw;
            }
        }

        public async Task ScheduleExportAsync(ExportSchedule schedule)
        {
            try
            {
                var schedules = await GetAllScheduledExportsAsync();
                schedule.Id = Guid.NewGuid().ToString();
                schedule.CreatedAt = DateTime.UtcNow;
                schedule.IsActive = true;

                schedules.Add(schedule);
                await SaveScheduledExportsAsync(schedules);

                await _auditService.LogSystemEventAsync("Export Scheduled", $"Export scheduled: {schedule.Name}");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to schedule export", ex);
                throw;
            }
        }

        public async Task<List<ExportSchedule>> GetScheduledExportsAsync()
        {
            try
            {
                var schedules = await GetAllScheduledExportsAsync();
                return schedules.Where(s => s.IsActive).ToList();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to get scheduled exports", ex);
                return new List<ExportSchedule>();
            }
        }

        public async Task CancelScheduledExportAsync(string scheduleId)
        {
            try
            {
                var schedules = await GetAllScheduledExportsAsync();
                var schedule = schedules.FirstOrDefault(s => s.Id == scheduleId);

                if (schedule != null)
                {
                    schedule.IsActive = false;
                    schedule.CancelledAt = DateTime.UtcNow;
                    await SaveScheduledExportsAsync(schedules);

                    await _auditService.LogSystemEventAsync("Export Cancelled", $"Scheduled export cancelled: {schedule.Name}");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to cancel scheduled export", ex);
            }
        }

        public async Task ProcessScheduledExportsAsync()
        {
            try
            {
                var schedules = await GetScheduledExportsAsync();
                var dueSchedules = schedules.Where(s => s.NextRunTime <= DateTime.UtcNow).ToList();

                foreach (var schedule in dueSchedules)
                {
                    try
                    {
                        string filePath = schedule.DataType switch
                        {
                            DataType.Users => await ExportUsersAsync(schedule.Format, schedule.Options),
                            DataType.Clubs => await ExportClubsAsync(schedule.Format, schedule.Options),
                            DataType.Events => await ExportEventsAsync(schedule.Format, schedule.Options),
                            DataType.Reports => await ExportReportsAsync(schedule.Format, schedule.Options),
                            DataType.All => await ExportAllDataAsync(schedule.Format, schedule.Options),
                            _ => throw new ArgumentException($"Unsupported data type: {schedule.DataType}")
                        };

                        schedule.LastRunTime = DateTime.UtcNow;
                        schedule.NextRunTime = CalculateNextRunTime(schedule);
                        schedule.RunCount++;

                        await _loggingService.LogInformationAsync($"Scheduled export completed: {schedule.Name} -> {filePath}");
                    }
                    catch (Exception ex)
                    {
                        await _loggingService.LogErrorAsync($"Failed to process scheduled export: {schedule.Name}", ex);
                    }
                }

                if (dueSchedules.Any())
                {
                    await SaveScheduledExportsAsync(schedules);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to process scheduled exports", ex);
            }
        }

        public async Task<string> TransformDataAsync(string inputPath, ImportFormat inputFormat, ExportFormat outputFormat, TransformOptions? options = null)
        {
            try
            {
                // Read data in input format
                var data = await ReadRawDataAsync(inputPath, inputFormat);

                // Apply transformations if specified
                if (options?.Transformations?.Any() == true)
                {
                    data = ApplyTransformations(data, options.Transformations);
                }

                // Export in output format
                var outputPath = Path.ChangeExtension(inputPath, GetFileExtension(outputFormat));
                await WriteDataAsync(data, outputPath, outputFormat);

                await _loggingService.LogInformationAsync($"Data transformed from {inputFormat} to {outputFormat}: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to transform data", ex);
                throw;
            }
        }

        public async Task<string> MergeDataFilesAsync(List<string> filePaths, ExportFormat outputFormat, MergeOptions? options = null)
        {
            try
            {
                var mergedData = new List<object>();

                foreach (var filePath in filePaths)
                {
                    var format = DetermineFormatFromExtension(filePath);
                    var data = await ReadRawDataAsync(filePath, format);
                    mergedData.AddRange(data);
                }

                // Remove duplicates if specified
                if (options?.RemoveDuplicates == true)
                {
                    mergedData = RemoveDuplicates(mergedData, options.DuplicateKeyFields);
                }

                var outputPath = Path.Combine(Path.GetDirectoryName(filePaths.First()) ?? "",
                    $"merged_data_{DateTime.Now:yyyyMMdd_HHmmss}.{GetFileExtension(outputFormat)}");

                await WriteDataAsync(mergedData, outputPath, outputFormat);

                await _loggingService.LogInformationAsync($"Data files merged: {filePaths.Count} files -> {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Failed to merge data files", ex);
                throw;
            }
        }

        // Private helper methods
        private async Task<string> ExportDataAsync(object data, ExportFormat format, string fileName, ExportOptions? options)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var extension = GetFileExtension(format);
            var exportPath = GetExportPath();
            var filePath = Path.Combine(exportPath, $"{fileName}_{timestamp}.{extension}");

            switch (format)
            {
                case ExportFormat.JSON:
                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var jsonData = JsonSerializer.Serialize(data, jsonOptions);
                    await File.WriteAllTextAsync(filePath, jsonData);
                    break;

                case ExportFormat.CSV:
                    await WriteCsvDataAsync(data, filePath);
                    break;

                case ExportFormat.XML:
                    await WriteXmlDataAsync(data, filePath);
                    break;

                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }

            return filePath;
        }

        private async Task<List<T>> ReadImportDataAsync<T>(string filePath, ImportFormat format)
        {
            switch (format)
            {
                case ImportFormat.JSON:
                    var jsonData = await File.ReadAllTextAsync(filePath);
                    return JsonSerializer.Deserialize<List<T>>(jsonData) ?? new List<T>();

                case ImportFormat.CSV:
                    return await ReadCsvDataAsync<T>(filePath);

                case ImportFormat.XML:
                    return await ReadXmlDataAsync<T>(filePath);

                default:
                    throw new ArgumentException($"Unsupported import format: {format}");
            }
        }

        private async Task<List<T>> ReadCsvDataAsync<T>(string filePath)
        {
            using var reader = new StringReader(await File.ReadAllTextAsync(filePath));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<T>().ToList();
        }

        private Task<List<T>> ReadXmlDataAsync<T>(string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<T>));
            using var stream = new FileStream(filePath, FileMode.Open);
            return Task.FromResult((List<T>)(serializer.Deserialize(stream) ?? new List<T>()));
        }

        private async Task WriteCsvDataAsync(object data, string filePath)
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            if (data is IEnumerable<object> enumerable)
            {
                await csv.WriteRecordsAsync(enumerable);
            }
            else
            {
                await csv.WriteRecordsAsync(new[] { data });
            }

            await File.WriteAllTextAsync(filePath, writer.ToString());
        }

        private Task WriteXmlDataAsync(object data, string filePath)
        {
            var serializer = new XmlSerializer(data.GetType());
            using var stream = new FileStream(filePath, FileMode.Create);
            serializer.Serialize(stream, data);
            return Task.CompletedTask;
        }

        private string GetFileExtension(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.JSON => "json",
                ExportFormat.CSV => "csv",
                ExportFormat.XML => "xml",
                _ => "txt"
            };
        }

        private ImportFormat DetermineFormatFromExtension(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".json" => ImportFormat.JSON,
                ".csv" => ImportFormat.CSV,
                ".xml" => ImportFormat.XML,
                _ => ImportFormat.JSON
            };
        }

        private string GetExportPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var exportPath = Path.Combine(appDataPath, "ClubManagement", "Exports");
            Directory.CreateDirectory(exportPath);
            return exportPath;
        }

        // Additional helper methods would be implemented here...
        // For brevity, I'm including just the essential structure

        private async Task<object> GetUsersForExportAsync()
        {
            return await _context.Users.Include(u => u.Club).ToListAsync();
        }

        private async Task<object> GetClubsForExportAsync()
        {
            return await _context.Clubs.Include(c => c.ClubMembers).ToListAsync();
        }

        private async Task<object> GetEventsForExportAsync()
        {
            return await _context.Events.Include(e => e.Club).Include(e => e.Participants).ToListAsync();
        }

        private async Task<object> GetReportsForExportAsync()
        {
            return await _context.Reports.Include(r => r.GeneratedByUser).Include(r => r.Club).ToListAsync();
        }

        private async Task<int> GetTotalRecordCountAsync()
        {
            return await _context.Users.CountAsync() +
                   await _context.Clubs.CountAsync() +
                   await _context.Events.CountAsync() +
                   await _context.Reports.CountAsync();
        }

        private Task<ImportResult> ImportClubDataAsync(List<ClubImportModel> clubs, ImportOptions? options)
        {
            // Implementation similar to ImportClubsAsync but for in-memory data
            var result = new ImportResult { DataType = DataType.Clubs };
            // ... implementation details
            return Task.FromResult(result);
        }

        private Task<ImportResult> ImportUserDataAsync(List<UserImportModel> users, ImportOptions? options)
        {
            // Implementation similar to ImportUsersAsync but for in-memory data
            var result = new ImportResult { DataType = DataType.Users };
            // ... implementation details
            return Task.FromResult(result);
        }

        private Task<ImportResult> ImportEventDataAsync(List<EventImportModel> events, ImportOptions? options)
        {
            // Implementation similar to ImportEventsAsync but for in-memory data
            var result = new ImportResult { DataType = DataType.Events };
            // ... implementation details
            return Task.FromResult(result);
        }

        private async Task ValidateUserDataAsync(string filePath, ImportFormat format, ValidationResult result)
        {
            try
            {
                var userData = await ReadImportDataAsync<UserImportModel>(filePath, format);
                foreach (var user in userData)
                {
                    if (string.IsNullOrEmpty(user.FullName))
                        result.Errors.Add("User full name is required");
                    if (string.IsNullOrEmpty(user.Email) || !IsValidEmail(user.Email))
                        result.Errors.Add($"Invalid email: {user.Email}");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error validating user data: {ex.Message}");
            }
        }

        private async Task ValidateClubDataAsync(string filePath, ImportFormat format, ValidationResult result)
        {
            try
            {
                var clubData = await ReadImportDataAsync<ClubImportModel>(filePath, format);
                foreach (var club in clubData)
                {
                    if (string.IsNullOrEmpty(club.Name))
                        result.Errors.Add("Club name is required");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error validating club data: {ex.Message}");
            }
        }

        private async Task ValidateEventDataAsync(string filePath, ImportFormat format, ValidationResult result)
        {
            try
            {
                var eventData = await ReadImportDataAsync<EventImportModel>(filePath, format);
                foreach (var eventModel in eventData)
                {
                    if (string.IsNullOrEmpty(eventModel.Name))
                        result.Errors.Add("Event name is required");
                    if (string.IsNullOrEmpty(eventModel.ClubName))
                        result.Errors.Add("Club name is required for events");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error validating event data: {ex.Message}");
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async Task<object> GetFilteredUsersAsync(DataFilter filter)
        {
            var query = _context.Users.Include(u => u.Club).AsQueryable();

            if (filter.DateFrom.HasValue)
                query = query.Where(u => u.CreatedAt >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(u => u.CreatedAt <= filter.DateTo.Value);
            if (!string.IsNullOrEmpty(filter.SearchTerm))
                query = query.Where(u => u.FullName.Contains(filter.SearchTerm) || u.Email.Contains(filter.SearchTerm));

            return await query.ToListAsync();
        }

        private async Task<object> GetFilteredClubsAsync(DataFilter filter)
        {
            var query = _context.Clubs.Include(c => c.ClubMembers).AsQueryable();

            if (filter.DateFrom.HasValue)
                query = query.Where(c => c.EstablishedDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(c => c.EstablishedDate <= filter.DateTo.Value);
            if (!string.IsNullOrEmpty(filter.SearchTerm))
                query = query.Where(c => c.ClubName.Contains(filter.SearchTerm) || (c.Description != null && c.Description.Contains(filter.SearchTerm)));

            return await query.ToListAsync();
        }

        private async Task<object> GetFilteredEventsAsync(DataFilter filter)
        {
            var query = _context.Events.Include(e => e.Club).Include(e => e.Participants).AsQueryable();

            if (filter.DateFrom.HasValue)
                query = query.Where(e => e.EventDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(e => e.EventDate <= filter.DateTo.Value);
            if (!string.IsNullOrEmpty(filter.SearchTerm))
                query = query.Where(e => e.Name.Contains(filter.SearchTerm) || (e.Description != null && e.Description.Contains(filter.SearchTerm)));

            return await query.ToListAsync();
        }

        private async Task<object> GetFilteredReportsAsync(DataFilter filter)
        {
            var query = _context.Reports.Include(r => r.GeneratedByUser).Include(r => r.Club).AsQueryable();

            if (filter.DateFrom.HasValue)
                query = query.Where(r => r.GeneratedDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(r => r.GeneratedDate <= filter.DateTo.Value);
            if (!string.IsNullOrEmpty(filter.SearchTerm))
                query = query.Where(r => r.Title.Contains(filter.SearchTerm) || (r.Content != null && r.Content.Contains(filter.SearchTerm)));

            return await query.ToListAsync();
        }

        private async Task<List<ExportSchedule>> GetAllScheduledExportsAsync()
        {
            var filePath = GetScheduleFilePath();
            if (!File.Exists(filePath))
                return new List<ExportSchedule>();

            var jsonData = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<ExportSchedule>>(jsonData) ?? new List<ExportSchedule>();
        }

        private async Task SaveScheduledExportsAsync(List<ExportSchedule> schedules)
        {
            var filePath = GetScheduleFilePath();
            var jsonData = JsonSerializer.Serialize(schedules, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, jsonData);
        }

        private string GetScheduleFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var schedulePath = Path.Combine(appDataPath, "ClubManagement", "Schedules");
            Directory.CreateDirectory(schedulePath);
            return Path.Combine(schedulePath, "export_schedules.json");
        }

        private DateTime CalculateNextRunTime(ExportSchedule schedule)
        {
            return schedule.Frequency switch
            {
                ScheduleFrequency.Daily => DateTime.UtcNow.AddDays(1),
                ScheduleFrequency.Weekly => DateTime.UtcNow.AddDays(7),
                ScheduleFrequency.Monthly => DateTime.UtcNow.AddMonths(1),
                ScheduleFrequency.Yearly => DateTime.UtcNow.AddYears(1),
                _ => DateTime.UtcNow.AddDays(1)
            };
        }

        private async Task<List<object>> ReadRawDataAsync(string filePath, ImportFormat format)
        {
            // Implementation for reading raw data without specific type
            switch (format)
            {
                case ImportFormat.JSON:
                    var jsonData = await File.ReadAllTextAsync(filePath);
                    var jsonDoc = JsonDocument.Parse(jsonData);
                    return new List<object> { jsonDoc.RootElement };

                case ImportFormat.CSV:
                    var csvLines = await File.ReadAllLinesAsync(filePath);
                    return csvLines.Cast<object>().ToList();

                case ImportFormat.XML:
                    var xmlData = await File.ReadAllTextAsync(filePath);
                    return new List<object> { xmlData };

                default:
                    throw new ArgumentException($"Unsupported format: {format}");
            }
        }

        private async Task WriteDataAsync(object data, string filePath, ExportFormat format)
        {
            await ExportDataAsync(data, format, Path.GetFileNameWithoutExtension(filePath), null);
        }

        private List<object> ApplyTransformations(List<object> data, List<DataTransformation> transformations)
        {
            // Implementation for applying data transformations
            // This would be a complex feature requiring specific transformation logic
            return data;
        }

        private List<object> RemoveDuplicates(List<object> data, List<string>? keyFields)
        {
            // Implementation for removing duplicates based on key fields
            // This would require reflection or dynamic property access
            return data.Distinct().ToList();
        }

        // Private helper methods for import operations
        private Task<ValidationResult> ValidateUserImportModelAsync(UserImportModel userModel)
        {
            var result = new ValidationResult { IsValid = true };

            if (!ServiceValidationHelper.IsValidEmail(userModel.Email))
            {
                result.IsValid = false;
                result.Errors.Add($"Invalid email format: {userModel.Email}");
            }

            if (string.IsNullOrWhiteSpace(userModel.FullName) || userModel.FullName.Length > ServiceConfiguration.Users.MaxNameLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Invalid full name: {userModel.FullName}");
            }

            if (!string.IsNullOrEmpty(userModel.StudentID) && userModel.StudentID.Length > ServiceConfiguration.Users.MaxStudentIdLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Student ID too long: {userModel.StudentID}");
            }

            return Task.FromResult(result);
        }

        private async Task<User> CreateUserFromImportModelAsync(UserImportModel userModel)
        {
            return new User
            {
                FullName = userModel.FullName,
                Email = userModel.Email,
                StudentID = userModel.StudentID,
                Password = await _securityService.HashPasswordAsync(userModel.Password ?? ServiceConfiguration.Security.DefaultPassword),
                SystemRole = Enum.Parse<SystemRole>(userModel.SystemRole ?? "Member"),
                CreatedAt = userModel.JoinDate ?? DateTime.Now,
                IsActive = userModel.IsActive ?? true
            };
        }

        private async Task UpdateExistingUserAsync(User existingUser, User newUser)
        {
            existingUser.FullName = newUser.FullName;
            existingUser.StudentID = newUser.StudentID;
            existingUser.SystemRole = newUser.SystemRole;
            existingUser.IsActive = newUser.IsActive;

            await Task.CompletedTask;
        }
    }

    // Supporting classes and enums
    public class ImportResult
    {
        public DataType DataType { get; set; }
        public int ImportedRecords { get; set; }
        public int UpdatedRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int ErrorRecords { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class DataConflict
    {
        public ConflictType Type { get; set; }
        public string Field { get; set; } = string.Empty;
        public string ImportValue { get; set; } = string.Empty;
        public string ExistingValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }



    public class DataFilter
    {
        public DataType DataType { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? SearchTerm { get; set; }
        public Dictionary<string, object>? CustomFilters { get; set; }
    }

    public class FieldMapping
    {
        public DataType DataType { get; set; }
        public Dictionary<string, string> FieldMappings { get; set; } = new();
        public Dictionary<string, object> DefaultValues { get; set; } = new();
    }

    public class ExportSchedule
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DataType DataType { get; set; }
        public ExportFormat Format { get; set; }
        public ScheduleFrequency Frequency { get; set; }
        public DateTime NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public bool IsActive { get; set; }
        public int RunCount { get; set; }
        public ExportOptions? Options { get; set; }
    }

    public class TransformOptions
    {
        public List<DataTransformation> Transformations { get; set; } = new();
        public bool PreserveOriginal { get; set; } = true;
    }

    public class MergeOptions
    {
        public bool RemoveDuplicates { get; set; } = true;
        public List<string>? DuplicateKeyFields { get; set; }
        public MergeStrategy Strategy { get; set; } = MergeStrategy.Append;
    }

    public class DataTransformation
    {
        public string Field { get; set; } = string.Empty;
        public TransformationType Type { get; set; }
        public string? Parameter { get; set; }
    }

    public class CompleteBackupModel
    {
        public List<UserImportModel>? Users { get; set; }
        public List<ClubImportModel>? Clubs { get; set; }
        public List<EventImportModel>? Events { get; set; }
        public List<ReportImportModel>? Reports { get; set; }
        public BackupInfo? ExportInfo { get; set; }
    }



    // Import model classes
    public class UserImportModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StudentID { get; set; }
        public string? Password { get; set; }
        public string? SystemRole { get; set; }
        public string? ActivityLevel { get; set; }
        public DateTime? JoinDate { get; set; }
        public bool? IsActive { get; set; }
        public bool? TwoFactorEnabled { get; set; }
        public string? ClubName { get; set; }
    }

    // Enums
    public enum ExportFormat
    {
        JSON,
        CSV,
        XML
    }

    public enum ImportFormat
    {
        JSON,
        CSV,
        XML
    }

    public enum DataType
    {
        Users,
        Clubs,
        Events,
        Reports,
        All
    }

    public enum ConflictType
    {
        Duplicate,
        InvalidReference,
        DataMismatch,
        ValidationError
    }

    public enum ScheduleFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public enum MergeStrategy
    {
        Append,
        Replace,
        Merge
    }

    public enum TransformationType
    {
        Uppercase,
        Lowercase,
        Trim,
        Replace,
        Format,
        Calculate
    }

    public class ClubImportModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? EstablishedDate { get; set; }
        public bool? IsActive { get; set; }
    }

    public class EventImportModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? Date { get; set; }
        public string? Location { get; set; }
        public int? MaxParticipants { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
        public bool? IsActive { get; set; }
        public string? ClubName { get; set; }
    }

    public class ReportImportModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Content { get; set; }
        public DateTime? GeneratedDate { get; set; }
        public string? Semester { get; set; }
        public string? GeneratedByEmail { get; set; }
        public string? ClubName { get; set; }
    }
}