using ClubManagementApp.Models;

namespace ClubManagementApp.Services
{
    public interface IReportService
    {
        Task<IEnumerable<Report>> GetAllReportsAsync();
        Task<Report?> GetReportByIdAsync(int reportId);
        Task<IEnumerable<Report>> GetReportsByClubAsync(int clubId);
        Task<IEnumerable<Report>> GetReportsByTypeAsync(ReportType type);
        Task<Report> CreateReportAsync(Report report);
        Task<Report> UpdateReportAsync(Report report);
        Task<bool> DeleteReportAsync(int reportId);
        Task<Report> GenerateMemberStatisticsReportAsync(int clubId, string semester, int generatedByUserId);
        Task<Report> GenerateEventOutcomesReportAsync(int clubId, string semester, int generatedByUserId);
        Task<Report> GenerateActivityTrackingReportAsync(int clubId, string semester, int generatedByUserId);
        Task<Report> GenerateSemesterSummaryReportAsync(int clubId, string semester, int generatedByUserId);
        Task<byte[]> ExportReportToPdfAsync(int reportId);
        Task<byte[]> ExportReportToExcelAsync(int reportId);
        Task<int> GetTotalReportsCountAsync();
    }
}
