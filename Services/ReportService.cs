using ClubManagementApp.Data;
using ClubManagementApp.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.IO;
using System.Text.Json;

namespace ClubManagementApp.Services
{
    public class ReportService : IReportService
    {
        private readonly ClubManagementDbContext _context;

        public ReportService(ClubManagementDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Report>> GetAllReportsAsync()
        {
            try
            {
                Console.WriteLine("[REPORT_SERVICE] Getting all reports from database...");
                var reports = await _context.Reports
                    .Include(r => r.Club)
                    .Include(r => r.GeneratedByUser)
                    .OrderByDescending(r => r.GeneratedDate)
                    .ToListAsync();
                Console.WriteLine($"[REPORT_SERVICE] Retrieved {reports.Count} reports from database");
                return reports;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error getting all reports: {ex.Message}");
                throw;
            }
        }

        public async Task<Report?> GetReportByIdAsync(int reportId)
        {
            try
            {
                Console.WriteLine($"[REPORT_SERVICE] Getting report by ID: {reportId}");

                // Input validation
                if (reportId <= 0)
                    throw new ArgumentException("Invalid report ID", nameof(reportId));

                var report = await _context.Reports
                    .Include(r => r.Club)
                    .Include(r => r.GeneratedByUser)
                    .FirstOrDefaultAsync(r => r.ReportID == reportId);

                if (report != null)
                {
                    Console.WriteLine($"[REPORT_SERVICE] Found report: {report.Title} ({report.Type}) generated on {report.GeneratedDate:yyyy-MM-dd}");
                }
                else
                {
                    Console.WriteLine($"[REPORT_SERVICE] Report not found with ID: {reportId}");
                }
                return report;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error getting report {reportId}: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Report>> GetReportsByClubAsync(int clubId)
        {
            try
            {
                // Input validation
                if (clubId <= 0)
                    throw new ArgumentException("Invalid club ID", nameof(clubId));

                // Verify club exists
                var club = await _context.Clubs.FindAsync(clubId);
                if (club == null)
                    throw new InvalidOperationException($"Club with ID {clubId} not found");

                return await _context.Reports
                    .Include(r => r.Club)
                    .Include(r => r.GeneratedByUser)
                    .Where(r => r.ClubID == clubId)
                    .OrderByDescending(r => r.GeneratedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error getting reports for club {clubId}: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Report>> GetReportsByTypeAsync(ReportType type)
        {
            try
            {
                if (!Enum.IsDefined(typeof(ReportType), type))
                    throw new ArgumentException("Invalid report type", nameof(type));

                return await _context.Reports
                    .Include(r => r.Club)
                    .Include(r => r.GeneratedByUser)
                    .Where(r => r.Type == type)
                    .OrderByDescending(r => r.GeneratedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error getting reports by type {type}: {ex.Message}");
                throw;
            }
        }

        public async Task<Report> CreateReportAsync(Report report)
        {
            try
            {
                Console.WriteLine($"[REPORT_SERVICE] Creating new report: {report.Title} ({report.Type})");
                Console.WriteLine($"[REPORT_SERVICE] Report for club ID: {report.ClubID}, semester: {report.Semester}");

                // Input validation
                if (report == null)
                    throw new ArgumentNullException(nameof(report), "Report cannot be null");

                if (string.IsNullOrWhiteSpace(report.Title))
                    throw new ArgumentException("Report title is required", nameof(report));

                if (string.IsNullOrWhiteSpace(report.Semester))
                    throw new ArgumentException("Semester is required", nameof(report));

                if (report.GeneratedByUserID <= 0)
                    throw new ArgumentException("Valid user ID is required", nameof(report));

                // Verify club exists (if ClubID is provided)
                if (report.ClubID.HasValue)
                {
                    if (report.ClubID <= 0)
                        throw new ArgumentException("Valid club ID is required", nameof(report));
                        
                    var club = await _context.Clubs.FindAsync(report.ClubID);
                    if (club == null)
                        throw new InvalidOperationException($"Club with ID {report.ClubID} not found");
                }

                // Verify user exists
                var user = await _context.Users.FindAsync(report.GeneratedByUserID);
                if (user == null)
                    throw new InvalidOperationException($"User with ID {report.GeneratedByUserID} not found");

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[REPORT_SERVICE] Report created successfully with ID: {report.ReportID}");
                return report;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error creating report: {ex.Message}");
                throw;
            }
        }

        public async Task<Report> UpdateReportAsync(Report report)
        {
            try
            {
                Console.WriteLine($"[REPORT_SERVICE] Updating report: {report.Title} (ID: {report.ReportID})");

                // Input validation
                if (report == null)
                    throw new ArgumentNullException(nameof(report), "Report cannot be null");

                if (report.ReportID <= 0)
                    throw new ArgumentException("Invalid report ID", nameof(report.ReportID));

                if (string.IsNullOrWhiteSpace(report.Title))
                    throw new ArgumentException("Report title is required", nameof(report.Title));

                if (string.IsNullOrWhiteSpace(report.Content))
                    throw new ArgumentException("Report content is required", nameof(report.Content));

                // Check if report exists
                var existingReport = await _context.Reports.FindAsync(report.ReportID);
                if (existingReport == null)
                    throw new InvalidOperationException($"Report with ID {report.ReportID} not found");

                // Update properties
                existingReport.Title = report.Title;
                existingReport.Content = report.Content;
                existingReport.Type = report.Type;
                existingReport.Semester = report.Semester;
                // Note: We don't update GeneratedDate, ClubID, or GeneratedByUserID for audit purposes

                _context.Reports.Update(existingReport);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[REPORT_SERVICE] Report updated successfully: {existingReport.Title}");
                return existingReport;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error updating report {report?.ReportID}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteReportAsync(int reportId)
        {
            try
            {
                Console.WriteLine($"[REPORT_SERVICE] Attempting to delete report with ID: {reportId}");

                // Input validation
                if (reportId <= 0)
                    throw new ArgumentException("Invalid report ID", nameof(reportId));

                var report = await _context.Reports.FindAsync(reportId);
                if (report == null)
                {
                    Console.WriteLine($"[REPORT_SERVICE] Report not found for deletion: {reportId}");
                    return false;
                }

                Console.WriteLine($"[REPORT_SERVICE] Deleting report: {report.Title} ({report.Type})");
                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[REPORT_SERVICE] Report deleted successfully: {report.Title}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error deleting report {reportId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Report> GenerateMemberStatisticsReportAsync(int clubId, string semester, int generatedByUserId)
        {
            try
            {
                Console.WriteLine($"[REPORT_SERVICE] Generating member statistics report for club {clubId}, semester {semester}");

                // Input validation
                if (clubId <= 0)
                    throw new ArgumentException("Invalid club ID", nameof(clubId));

                if (string.IsNullOrWhiteSpace(semester))
                    throw new ArgumentException("Semester is required", nameof(semester));

                if (generatedByUserId <= 0)
                    throw new ArgumentException("Invalid user ID", nameof(generatedByUserId));

                var club = await _context.Clubs.Include(c => c.Members).FirstOrDefaultAsync(c => c.ClubID == clubId);
                if (club == null)
                {
                    Console.WriteLine($"[REPORT_SERVICE] Club not found: {clubId}");
                    throw new InvalidOperationException($"Club with ID {clubId} not found");
                }

                // Verify user exists
                var user = await _context.Users.FindAsync(generatedByUserId);
                if (user == null)
                    throw new InvalidOperationException($"User with ID {generatedByUserId} not found");

                Console.WriteLine($"[REPORT_SERVICE] Found club: {club.Name} with {club.Members.Count} members");

            var memberStats = new
            {
                TotalMembers = club.Members.Count(m => m.IsActive),
                ActiveMembers = club.Members.Count(m => m.ActivityLevel == ActivityLevel.Active),
                NormalMembers = club.Members.Count(m => m.ActivityLevel == ActivityLevel.Normal),
                InactiveMembers = club.Members.Count(m => m.ActivityLevel == ActivityLevel.Inactive),
                NewMembersThisSemester = club.Members.Count(m => m.JoinDate >= GetSemesterStartDate(semester)),
                MembersByRole = club.Members.GroupBy(m => m.Role).ToDictionary(g => g.Key.ToString(), g => g.Count())
            };

            var report = new Report
            {
                Title = $"Member Statistics Report - {club.Name} ({semester})",
                Type = ReportType.MemberStatistics,
                Content = JsonSerializer.Serialize(memberStats, new JsonSerializerOptions { WriteIndented = true }),
                Semester = semester,
                ClubID = clubId,
                GeneratedByUserID = generatedByUserId
            };

                Console.WriteLine($"[REPORT_SERVICE] Member statistics calculated - Total: {memberStats.TotalMembers}, Active: {memberStats.ActiveMembers}, Normal: {memberStats.NormalMembers}, Inactive: {memberStats.InactiveMembers}");
                return await CreateReportAsync(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error generating member statistics report: {ex.Message}");
                throw;
            }
        }

        public async Task<Report> GenerateEventOutcomesReportAsync(int clubId, string semester, int generatedByUserId)
        {
            try
            {
                Console.WriteLine($"[REPORT_SERVICE] Generating event outcomes report for club {clubId}, semester {semester}");

                // Input validation
                if (clubId <= 0)
                    throw new ArgumentException("Invalid club ID", nameof(clubId));

                if (string.IsNullOrWhiteSpace(semester))
                    throw new ArgumentException("Semester is required", nameof(semester));

                if (generatedByUserId <= 0)
                    throw new ArgumentException("Invalid user ID", nameof(generatedByUserId));

                // Verify club exists
                var clubExists = await _context.Clubs.FindAsync(clubId);
                if (clubExists == null)
                    throw new InvalidOperationException($"Club with ID {clubId} not found");

                // Verify user exists
                var user = await _context.Users.FindAsync(generatedByUserId);
                if (user == null)
                    throw new InvalidOperationException($"User with ID {generatedByUserId} not found");

                var semesterStart = GetSemesterStartDate(semester);
                var semesterEnd = GetSemesterEndDate(semester);
                Console.WriteLine($"[REPORT_SERVICE] Semester period: {semesterStart:yyyy-MM-dd} to {semesterEnd:yyyy-MM-dd}");

            var events = await _context.Events
                .Include(e => e.Participants)
                .Where(e => e.ClubID == clubId && e.EventDate >= semesterStart && e.EventDate <= semesterEnd)
                .ToListAsync();
            Console.WriteLine($"[REPORT_SERVICE] Found {events.Count} events in the specified period");

            var eventStats = new
            {
                TotalEvents = events.Count,
                CompletedEvents = events.Count(e => e.EventDate <= DateTime.Now),
                UpcomingEvents = events.Count(e => e.EventDate > DateTime.Now),
                TotalParticipants = events.Sum(e => e.Participants.Count),
                AverageAttendance = events.Any() ? events.Average(e => e.Participants.Count(p => p.Status == AttendanceStatus.Attended)) : 0,
                EventDetails = events.Select(e => new
                {
                    e.Name,
                    e.EventDate,
                    e.Location,
                    Registered = e.Participants.Count(p => p.Status == AttendanceStatus.Registered),
                    Attended = e.Participants.Count(p => p.Status == AttendanceStatus.Attended),
                    Absent = e.Participants.Count(p => p.Status == AttendanceStatus.Absent)
                })
            };

            var club = await _context.Clubs.FindAsync(clubId);
            var report = new Report
            {
                Title = $"Event Outcomes Report - {club?.Name} ({semester})",
                Type = ReportType.EventOutcomes,
                Content = JsonSerializer.Serialize(eventStats, new JsonSerializerOptions { WriteIndented = true }),
                Semester = semester,
                ClubID = clubId,
                GeneratedByUserID = generatedByUserId
            };

                Console.WriteLine($"[REPORT_SERVICE] Event outcomes calculated - Total events: {eventStats.TotalEvents}, Completed: {eventStats.CompletedEvents}, Upcoming: {eventStats.UpcomingEvents}");
                return await CreateReportAsync(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error generating event outcomes report: {ex.Message}");
                throw;
            }
        }

        public async Task<Report> GenerateActivityTrackingReportAsync(int clubId, string semester, int generatedByUserId)
        {
            try
            {
                Console.WriteLine($"[REPORT_SERVICE] Generating activity tracking report for club {clubId}, semester {semester}");

                // Input validation
                if (clubId <= 0)
                    throw new ArgumentException("Invalid club ID", nameof(clubId));

                if (string.IsNullOrWhiteSpace(semester))
                    throw new ArgumentException("Semester is required", nameof(semester));

                if (generatedByUserId <= 0)
                    throw new ArgumentException("Invalid user ID", nameof(generatedByUserId));

                // Verify club exists
                var clubExists = await _context.Clubs.FindAsync(clubId);
                if (clubExists == null)
                    throw new InvalidOperationException($"Club with ID {clubId} not found");

                // Verify user exists
                var user = await _context.Users.FindAsync(generatedByUserId);
                if (user == null)
                    throw new InvalidOperationException($"User with ID {generatedByUserId} not found");

                var semesterStart = GetSemesterStartDate(semester);
                var semesterEnd = GetSemesterEndDate(semester);
                Console.WriteLine($"[REPORT_SERVICE] Analyzing member activity from {semesterStart:yyyy-MM-dd} to {semesterEnd:yyyy-MM-dd}");

            var members = await _context.Users
                .Include(u => u.EventParticipations)
                    .ThenInclude(ep => ep.Event)
                .Where(u => u.ClubID == clubId && u.IsActive)
                .ToListAsync();
            Console.WriteLine($"[REPORT_SERVICE] Analyzing activity for {members.Count} active members");

            var activityData = members.Select(member =>
            {
                var semesterEvents = member.EventParticipations
                    .Where(ep => ep.Event.EventDate >= semesterStart && ep.Event.EventDate <= semesterEnd)
                    .ToList();

                var attendedEvents = semesterEvents.Count(ep => ep.Status == AttendanceStatus.Attended);
                var totalEvents = semesterEvents.Count;
                var attendancePercentage = totalEvents > 0 ? (double)attendedEvents / totalEvents * 100 : 0;

                return new
                {
                    member.FullName,
                    member.Email,
                    TotalEventsRegistered = totalEvents,
                    EventsAttended = attendedEvents,
                    AttendancePercentage = Math.Round(attendancePercentage, 2),
                    ActivityLevel = member.ActivityLevel.ToString()
                };
            });

            var activityStats = new
            {
                TotalMembers = members.Count,
                HighPerformers = activityData.Count(m => m.AttendancePercentage >= 80),
                AverageAttendance = activityData.Any() ? Math.Round(activityData.Average(m => m.AttendancePercentage), 2) : 0,
                MemberDetails = activityData.OrderByDescending(m => m.AttendancePercentage)
            };

            var club = await _context.Clubs.FindAsync(clubId);
            var report = new Report
            {
                Title = $"Activity Tracking Report - {club?.Name} ({semester})",
                Type = ReportType.ActivityTracking,
                Content = JsonSerializer.Serialize(activityStats, new JsonSerializerOptions { WriteIndented = true }),
                Semester = semester,
                ClubID = clubId,
                GeneratedByUserID = generatedByUserId
            };

                Console.WriteLine($"[REPORT_SERVICE] Activity tracking calculated - Total members: {activityStats.TotalMembers}, High performers: {activityStats.HighPerformers}, Average attendance: {activityStats.AverageAttendance}%");
                return await CreateReportAsync(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error generating activity tracking report: {ex.Message}");
                throw;
            }
        }

        public async Task<Report> GenerateSemesterSummaryReportAsync(int clubId, string semester, int generatedByUserId)
        {
            try
            {
                Console.WriteLine($"[REPORT_SERVICE] Generating comprehensive semester summary report for club {clubId}, semester {semester}");

                // Input validation
                if (clubId <= 0)
                    throw new ArgumentException("Invalid club ID", nameof(clubId));

                if (string.IsNullOrWhiteSpace(semester))
                    throw new ArgumentException("Semester is required", nameof(semester));

                if (generatedByUserId <= 0)
                    throw new ArgumentException("Invalid user ID", nameof(generatedByUserId));

                var memberReport = await GenerateMemberStatisticsReportAsync(clubId, semester, generatedByUserId);
                var eventReport = await GenerateEventOutcomesReportAsync(clubId, semester, generatedByUserId);
                var activityReport = await GenerateActivityTrackingReportAsync(clubId, semester, generatedByUserId);
                Console.WriteLine($"[REPORT_SERVICE] Combined all sub-reports for semester summary");

            var summaryData = new
            {
                Semester = semester,
                GeneratedDate = DateTime.Now,
                MemberStatistics = JsonSerializer.Deserialize<object>(memberReport.Content),
                EventOutcomes = JsonSerializer.Deserialize<object>(eventReport.Content),
                ActivityTracking = JsonSerializer.Deserialize<object>(activityReport.Content)
            };

            var club = await _context.Clubs.FindAsync(clubId);
            var report = new Report
            {
                Title = $"Semester Summary Report - {club?.Name} ({semester})",
                Type = ReportType.SemesterSummary,
                Content = JsonSerializer.Serialize(summaryData, new JsonSerializerOptions { WriteIndented = true }),
                Semester = semester,
                ClubID = clubId,
                GeneratedByUserID = generatedByUserId
            };

                return await CreateReportAsync(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error generating semester summary report: {ex.Message}");
                throw;
            }
        }

        public async Task<byte[]> ExportReportToPdfAsync(int reportId)
        {
            try
            {
                Console.WriteLine($"[REPORT_SERVICE] Exporting report {reportId} to PDF format");

                // Input validation
                if (reportId <= 0)
                    throw new ArgumentException("Invalid report ID", nameof(reportId));

                var report = await GetReportByIdAsync(reportId);
                if (report == null)
                {
                    Console.WriteLine($"[REPORT_SERVICE] Cannot export - report {reportId} not found");
                    throw new InvalidOperationException($"Report with ID {reportId} not found");
                }

            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            // Add title
            document.Add(new Paragraph(report.Title)
                .SetFontSize(18)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER));

            // Add metadata
            document.Add(new Paragraph($"Generated: {report.GeneratedDate:yyyy-MM-dd HH:mm}")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(new Paragraph($"Club: {report.Club?.Name ?? "All Clubs"}")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(new Paragraph($"Semester: {report.Semester}")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(new Paragraph("\n"));

            // Parse and format content
            try
            {
                var contentData = JsonSerializer.Deserialize<Dictionary<string, object>>(report.Content);
                if (contentData != null)
                {
                    FormatContentForPdf(document, contentData);
                }
                else
                {
                    document.Add(new Paragraph(report.Content ?? "No content available"));
                }
            }
            catch
            {
                document.Add(new Paragraph(report.Content ?? "No content available"));
            }

            document.Close();
                Console.WriteLine($"[REPORT_SERVICE] PDF export completed for report: {report.Title}");
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error exporting report {reportId} to PDF: {ex.Message}");
                throw;
            }
        }

        public async Task<byte[]> ExportReportToExcelAsync(int reportId)
        {
            try
            {
                Console.WriteLine($"[REPORT_SERVICE] Exporting report {reportId} to Excel format");

                // Input validation
                if (reportId <= 0)
                    throw new ArgumentException("Invalid report ID", nameof(reportId));

                var report = await GetReportByIdAsync(reportId);
                if (report == null)
                {
                    Console.WriteLine($"[REPORT_SERVICE] Cannot export - report {reportId} not found");
                    throw new InvalidOperationException($"Report with ID {reportId} not found");
                }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Report");

            // Add title
            worksheet.Cells[1, 1].Value = report.Title;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1, 1, 5].Merge = true;

            // Add metadata
            worksheet.Cells[2, 1].Value = "Generated:";
            worksheet.Cells[2, 2].Value = report.GeneratedDate.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cells[3, 1].Value = "Club:";
            worksheet.Cells[3, 2].Value = report.Club?.Name ?? "All Clubs";
            worksheet.Cells[4, 1].Value = "Semester:";
            worksheet.Cells[4, 2].Value = report.Semester;

            // Parse and format content
            try
            {
                var contentData = JsonSerializer.Deserialize<Dictionary<string, object>>(report.Content);
                if (contentData != null)
                {
                    FormatContentForExcel(worksheet, contentData, 6);
                }
                else
                {
                    worksheet.Cells[6, 1].Value = "Content:";
                    worksheet.Cells[6, 2].Value = report.Content ?? "No content available";
                }
            }
            catch
            {
                worksheet.Cells[6, 1].Value = "Content:";
                worksheet.Cells[6, 2].Value = report.Content ?? "No content available";
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

                Console.WriteLine($"[REPORT_SERVICE] Excel export completed for report: {report.Title}");
                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error exporting report {reportId} to Excel: {ex.Message}");
                throw;
            }
        }

        private void FormatContentForPdf(Document document, Dictionary<string, object> content)
        {
            try
            {
                if (document == null || content == null)
                    return;

                foreach (var item in content)
                {
                    document.Add(new Paragraph(item.Key ?? "Unknown")
                        .SetFontSize(14)
                        .SetBold());

                    if (item.Value is JsonElement jsonElement)
                    {
                        FormatJsonElementForPdf(document, jsonElement);
                    }
                    else
                    {
                        document.Add(new Paragraph(item.Value?.ToString() ?? "N/A")
                            .SetMarginLeft(20));
                    }

                    document.Add(new Paragraph("\n"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error formatting content for PDF: {ex.Message}");
                document?.Add(new Paragraph("Error formatting report content"));
            }
        }

        private void FormatJsonElementForPdf(Document document, JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        document.Add(new Paragraph($"{property.Name}: {property.Value}")
                            .SetMarginLeft(20));
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        document.Add(new Paragraph($"• {item}")
                            .SetMarginLeft(20));
                    }
                    break;
                default:
                    document.Add(new Paragraph(element.ToString())
                        .SetMarginLeft(20));
                    break;
            }
        }

        private void FormatContentForExcel(ExcelWorksheet worksheet, Dictionary<string, object> content, int startRow)
        {
            int currentRow = startRow;

            foreach (var item in content)
            {
                worksheet.Cells[currentRow, 1].Value = item.Key;
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;

                if (item.Value is JsonElement jsonElement)
                {
                    currentRow = FormatJsonElementForExcel(worksheet, jsonElement, currentRow, 2);
                }
                else
                {
                    worksheet.Cells[currentRow, 2].Value = item.Value?.ToString() ?? "N/A";
                    currentRow++;
                }

                currentRow++; // Add spacing
            }
        }

        private int FormatJsonElementForExcel(ExcelWorksheet worksheet, JsonElement element, int startRow, int startCol)
        {
            int currentRow = startRow;

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        worksheet.Cells[currentRow, startCol].Value = property.Name;
                        worksheet.Cells[currentRow, startCol + 1].Value = property.Value.ToString();
                        currentRow++;
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        worksheet.Cells[currentRow, startCol].Value = item.ToString();
                        currentRow++;
                    }
                    break;
                default:
                    worksheet.Cells[currentRow, startCol].Value = element.ToString();
                    currentRow++;
                    break;
            }

            return currentRow;
        }

        private DateTime GetSemesterStartDate(string semester)
        {
            // Parse semester string like "Fall 2024" or "Spring 2024"
            var parts = semester.Split(' ');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int year))
                return DateTime.Now.AddMonths(-6);

            return parts[0].ToLower() switch
            {
                "spring" => new DateTime(year, 1, 1),
                "summer" => new DateTime(year, 5, 1),
                "fall" => new DateTime(year, 8, 1),
                _ => DateTime.Now.AddMonths(-6)
            };
        }

        private DateTime GetSemesterEndDate(string semester)
        {
            var startDate = GetSemesterStartDate(semester);
            return startDate.AddMonths(4).AddDays(-1);
        }

        /// <summary>
        /// Gets the total count of reports in the database efficiently.
        ///
        /// Performance:
        /// - Uses COUNT query instead of loading all records
        /// - Optimized for dashboard statistics display
        ///
        /// Used by: Dashboard statistics, admin overview
        /// </summary>
        /// <returns>Total number of reports in the system</returns>
        public async Task<int> GetTotalReportsCountAsync()
        {
            try
            {
                return await _context.Reports.CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT_SERVICE] Error getting total reports count: {ex.Message}");
                throw;
            }
        }
    }
}
