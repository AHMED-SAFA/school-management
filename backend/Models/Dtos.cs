namespace backend.Models;

// Request DTOs
public class CreateClassRequest
{
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
}

// Response DTOs
public class ClassResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int HeldSeats { get; set; }
    public int ConfirmedSeats { get; set; }
    public int AvailableSeats { get; set; }
}

public class CreateClassResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
}

public class HoldResponse
{
    public string HoldId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ConfirmResponse
{
    public string HoldId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class MyHoldResponse
{
    public string HoldId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}
