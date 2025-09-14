namespace Mordecai.Messaging.Messages;

/// <summary>
/// Published when a character gains skill experience
/// </summary>
public sealed record SkillExperienceGained(
    Guid CharacterId,
    string CharacterName,
    int SkillDefinitionId,
    string SkillName,
    int ExperienceGained,
    int NewLevel,
    bool LeveledUp
) : GameMessage
{
    public override IReadOnlyList<Guid>? TargetCharacterIds => [CharacterId]; // Only notify the character gaining XP
}

/// <summary>
/// Published when a skill is used (for logging and potential observation)
/// </summary>
public sealed record SkillUsed(
    Guid CharacterId,
    string CharacterName,
    int LocationRoomId,
    int SkillDefinitionId,
    string SkillName,
    string UsageDescription,
    bool Success,
    Guid? TargetId = null
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published when a character learns a new skill
/// </summary>
public sealed record SkillLearned(
    Guid CharacterId,
    string CharacterName,
    int SkillDefinitionId,
    string SkillName
) : GameMessage
{
    public override IReadOnlyList<Guid>? TargetCharacterIds => [CharacterId]; // Only notify the learning character
}