using System.ComponentModel.DataAnnotations;

namespace Mordecai.Game.Entities;

/// <summary>
/// Stores game-wide configuration settings
/// </summary>
public class GameConfiguration
{
    [Key]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;
}
