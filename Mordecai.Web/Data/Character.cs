using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mordecai.Web.Data;

public class Character
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty; // FK to AspNetUsers

    [StringLength(30)]
    public string Species { get; set; } = "Human"; // Simple placeholder

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastPlayedAt { get; set; }
}
