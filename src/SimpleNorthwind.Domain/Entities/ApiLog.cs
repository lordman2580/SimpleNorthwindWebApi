namespace SimpleNorthwind.Domain.Entities;

public sealed class ApiLog
{
    public Guid Guid { get; set; }
    public int? UserId { get; set; }
    public string Actions { get; set; } = string.Empty;
    public string? ActionDetail { get; set; }
    public int? ResponseStatus { get; set; }
    public string? ResponseResult { get; set; }
    public DateTime SummaryDate { get; set; }
}
