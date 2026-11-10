using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public enum HoldStatus
{
    HELD,
    CONFIRMED,
    EXPIRED,
    CANCELLED
}

[Table("holds")]
public class Hold
{
    [Key]
    [Column("id")]
    public string HoldId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("class_id")]
    public string ClassId { get; set; } = string.Empty;

    [Required]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    public HoldStatus Status { get; set; } = HoldStatus.HELD;

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ClassId")]
    public SchoolClass? SchoolClass { get; set; }
}
