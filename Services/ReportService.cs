using ClubManagementApp.Data;
using ClubManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using OfficeOpenXml;
using OfficeOpenXml.Style;

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
            return await _context.Reports
                .Include(r => r.Club)
                .Include(r => r.GeneratedByUser)
                .OrderByDescending(r => r.GeneratedDate)
                .ToListAsync();
        }

        public async Task<Report?> GetReportByIdAsync(int reportId)
        {
            return await _context.Reports
                .Include(r => r.Club)
                .Include(r => r.GeneratedByUser)
                .FirstOrDefaultAsync(r => r.ReportID == reportId);
        }

        public async Task<IEnumerable<Report>> GetReportsByClubAsync(int clubId)
        {
            return await _context.Reports
                .Include(r => r.Club)
                .Include(r => r.GeneratedByUser)
                .Where(r => r.ClubID == clubId)
                .OrderByDescending(r => r.GeneratedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetReportsByTypeAsync(ReportType type)
        {
            return await _context.Reports
                .Include(r => r.Club)
                .Include(r => r.GeneratedByUser)
                .Where(r => r.Type == type)
                .OrderByDescending(r => r.GeneratedDate)
                .ToListAsync();
        }

        public async Task<Report> CreateReportAsync(Report report)
        {
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<bool> DeleteReportAsync(int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null) return false;

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Report> GenerateMemberStatisticsReportAsync(int clubId, string semester, int generatedByUserId)
        {
            var club = await _context.Clubs.Include(c => c.Members).FirstOrDefaultAsync(c => c.ClubID == clubId);
            if (club == null) throw new ArgumentException("Club not found");

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

            return await CreateReportAsync(report);
        }

        public async Task<Report> GenerateEventOutcomesReportAsync(int clubId, string semester, int generatedByUserId)
        {
            var semesterStart = GetSemesterStartDate(semester);
            var semesterEnd = GetSemesterEndDate(semester);

            var events = await _context.Events
                .Include(e => e.Participants)
                .Where(e => e.ClubID == clubId && e.EventDate >= semesterStart && e.EventDate <= semesterEnd)
                .ToListAsync();

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

            return await CreateReportAsync(report);
        }

        public async Task<Report> GenerateActivityTrackingReportAsync(int clubId, string semester, int generatedByUserId)
        {
            var semesterStart = GetSemesterStartDate(semester);
            var semesterEnd = GetSemesterEndDate(semester);

            var members = await _context.Users
                .Include(u => u.EventParticipations)
                    .ThenInclude(ep => ep.Event)
                .Where(u => u.ClubID == clubId && u.IsActive)
                .ToListAsync();

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

            return await CreateReportAsync(report);
        }

        public async Task<Report> GenerateSemesterSummaryReportAsync(int clubId, string semester, int generatedByUserId)
        {
            var memberReport = await GenerateMemberStatisticsReportAsync(clubId, semester, generatedByUserId);
            var eventReport = await GenerateEventOutcomesReportAsync(clubId, semester, generatedByUserId);
            var activityReport = await GenerateActivityTrackingReportAsync(clubId, semester, generatedByUserId);

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

        public async Task<byte[]> ExportReportToPdfAsync(int reportId)
        {
            var report = await GetReportByIdAsync(reportId);
            if (report == null) return Array.Empty<byte>();

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
            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportReportToExcelAsync(int reportId)
        {
            var report = await GetReportByIdAsync(reportId);
            if (report == null) return Array.Empty<byte>();

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

            return package.GetAsByteArray();
        }

        private void FormatContentForPdf(Document document, Dictionary<string, object> content)
        {
            foreach (var item in content)
            {
                document.Add(new Paragraph(item.Key)
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
    }
}