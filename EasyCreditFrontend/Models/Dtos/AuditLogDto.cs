namespace EasyCreditFrontend.Models.Dtos;

public class AuditLogDto
{
    public int AuditId { get; set; }
    public int ApplicationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActionBy { get; set; } = string.Empty;
    public DateTime ActionAt { get; set; }
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? Notes { get; set; }
}
