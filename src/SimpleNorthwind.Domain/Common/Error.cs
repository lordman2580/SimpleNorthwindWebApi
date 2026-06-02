namespace SimpleNorthwind.Domain.Common;

/// <summary>業務錯誤分類，供 WebApi 對映 HTTP 狀態碼（見 06-稽核與共通技術規範#6）。</summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    Unauthorized = 2,
    NotFound = 3,
    Conflict = 4
}

/// <summary>領域 / 業務錯誤。系統例外仍以 throw 處理，不走 Error。</summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}
