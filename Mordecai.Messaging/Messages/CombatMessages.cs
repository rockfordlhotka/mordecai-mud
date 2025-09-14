namespace Mordecai.Messaging.Messages;

/// <summary>
/// Published when combat begins between entities
/// </summary>
public sealed record CombatStarted(
    Guid InitiatorId,
    string InitiatorName,
    Guid TargetId,
    string TargetName,
    int LocationRoomId
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published for each combat action/attack
/// </summary>
public sealed record CombatAction(
    Guid AttackerId,
    string AttackerName,
    Guid DefenderId,
    string DefenderName,
    int LocationRoomId,
    string ActionDescription,
    int Damage,
    bool IsHit,
    string SkillUsed
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published when combat ends
/// </summary>
public sealed record CombatEnded(
    int LocationRoomId,
    string EndReason,
    Guid? WinnerId = null,
    string? WinnerName = null
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published when a character's health changes significantly
/// </summary>
public sealed record HealthChanged(
    Guid CharacterId,
    string CharacterName,
    int LocationRoomId,
    int CurrentHealth,
    int MaxHealth,
    string? Cause = null
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}