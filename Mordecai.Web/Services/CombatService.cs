using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Game.Services;
using Mordecai.Messaging.Messages;
using Mordecai.Messaging.Services;
using Mordecai.Web.Data;
using System.Text.Json;

namespace Mordecai.Web.Services;

public sealed class CombatService : ICombatService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IGameMessagePublisher _messagePublisher;
    private readonly IDiceService _diceService;
    private readonly ILogger<CombatService> _logger;

    public CombatService(
        ApplicationDbContext dbContext,
        IGameMessagePublisher messagePublisher,
        IDiceService diceService,
        ILogger<CombatService> logger)
    {
        _dbContext = dbContext;
        _messagePublisher = messagePublisher;
        _diceService = diceService;
        _logger = logger;
    }

    public async Task<Guid?> InitiateCombatAsync(
        Guid attackerId,
        bool attackerIsPlayer,
        Guid targetId,
        bool targetIsPlayer,
        CancellationToken cancellationToken = default)
    {
        // Get attacker and target info (needed for all code paths)
        var (attackerName, attackerRoomId) = await GetParticipantInfoAsync(attackerId, attackerIsPlayer, cancellationToken);
        var (targetName, targetRoomId) = await GetParticipantInfoAsync(targetId, targetIsPlayer, cancellationToken);

        // Check if attacker is already in combat
        var existingSession = await GetActiveCombatSessionAsync(attackerId, attackerIsPlayer, cancellationToken);
        if (existingSession != null)
        {
            // Already in combat, can attack target if they're in the same session
            return existingSession.Id;
        }

        // Check if target is already in combat - join their session instead of creating new
        var targetSession = await GetActiveCombatSessionAsync(targetId, targetIsPlayer, cancellationToken);
        if (targetSession != null)
        {
            // Verify attacker is in same room as the combat
            if (attackerRoomId != targetSession.RoomId)
            {
                _logger.LogWarning("Cannot join combat - attacker in different room from combat");
                return null;
            }

            // Add attacker as new participant to target's existing session
            var joiningParticipant = new CombatParticipant
            {
                CombatSessionId = targetSession.Id,
                CharacterId = attackerIsPlayer ? attackerId : null,
                ActiveSpawnId = attackerIsPlayer ? null : (await _dbContext.ActiveSpawns
                    .Where(asp => asp.NpcId == attackerId && asp.IsActive)
                    .Select(asp => (int?)asp.Id)
                    .FirstOrDefaultAsync(cancellationToken)),
                ParticipantName = attackerName,
                IsActive = true,
                IsInParryMode = false,
                JoinedAt = DateTimeOffset.UtcNow
            };

            _dbContext.CombatParticipants.Add(joiningParticipant);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Attacker {Attacker} joined existing combat session {Session}",
                attackerName, targetSession.Id);

            return targetSession.Id;
        }

        if (attackerRoomId != targetRoomId)
        {
            _logger.LogWarning("Cannot initiate combat - participants in different rooms");
            return null;
        }

        // Create combat session
        var session = new CombatSession
        {
            RoomId = attackerRoomId,
            StartedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        _dbContext.CombatSessions.Add(session);

        // Add participants
        var attackerParticipant = new CombatParticipant
        {
            CombatSession = session,
            CharacterId = attackerIsPlayer ? attackerId : null,
            ActiveSpawnId = attackerIsPlayer ? null : (await _dbContext.ActiveSpawns
                .Where(asp => asp.NpcId == attackerId && asp.IsActive)
                .Select(asp => (int?)asp.Id)
                .FirstOrDefaultAsync(cancellationToken)),
            ParticipantName = attackerName,
            IsActive = true,
            IsInParryMode = false,
            JoinedAt = DateTimeOffset.UtcNow
        };

        var targetParticipant = new CombatParticipant
        {
            CombatSession = session,
            CharacterId = targetIsPlayer ? targetId : null,
            ActiveSpawnId = targetIsPlayer ? null : (await _dbContext.ActiveSpawns
                .Where(asp => asp.NpcId == targetId && asp.IsActive)
                .Select(asp => (int?)asp.Id)
                .FirstOrDefaultAsync(cancellationToken)),
            ParticipantName = targetName,
            IsActive = true,
            IsInParryMode = false,
            JoinedAt = DateTimeOffset.UtcNow
        };

        _dbContext.CombatParticipants.AddRange(attackerParticipant, targetParticipant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish combat started event
        await _messagePublisher.PublishAsync(new CombatStarted(
            attackerId,
            attackerName,
            targetId,
            targetName,
            attackerRoomId,
            SoundLevel.Loud
        ), cancellationToken);

        _logger.LogInformation("Combat initiated between {Attacker} and {Target} in room {Room}",
            attackerName, targetName, attackerRoomId);

        return session.Id;
    }

    public async Task<int?> PerformMeleeAttackAsync(
        Guid attackerId,
        bool attackerIsPlayer,
        Guid targetId,
        bool targetIsPlayer,
        bool isDualWield = false,
        bool isOffHand = false,
        CancellationToken cancellationToken = default)
    {
        // Ensure combat session exists
        var combatSessionId = await InitiateCombatAsync(attackerId, attackerIsPlayer, targetId, targetIsPlayer, cancellationToken);
        if (combatSessionId == null)
        {
            return null;
        }

        // Get attacker and target data
        var attackerData = await GetCombatantDataAsync(attackerId, attackerIsPlayer, cancellationToken);
        var targetData = await GetCombatantDataAsync(targetId, targetIsPlayer, cancellationToken);

        if (attackerData == null || targetData == null)
        {
            _logger.LogWarning("Cannot perform attack - missing combatant data");
            return null;
        }

        // Check fatigue cost
        int fatCost = isDualWield ? 2 : 1;
        if (attackerData.CurrentFatigue < fatCost)
        {
            _logger.LogDebug("Attacker {Attacker} has insufficient fatigue ({FAT}) for attack",
                attackerData.Name, attackerData.CurrentFatigue);
            return null;
        }

        // Get weapon skill and modifiers
        var weaponInfo = await GetWeaponSkillAsync(attackerData, isOffHand, cancellationToken);
        if (weaponInfo == null)
        {
            _logger.LogWarning("Attacker {Attacker} has no usable weapon", attackerData.Name);
            return null;
        }

        // Check if weapon is broken
        if (weaponInfo.IsBroken)
        {
            _logger.LogDebug("Attacker {Attacker} has a broken weapon: {Weapon}", attackerData.Name, weaponInfo.SkillName);
            await _messagePublisher.PublishAsync(new CombatAction(
                attackerId, attackerData.Name, targetId, targetData.Name,
                attackerData.RoomId, $"{attackerData.Name}'s {weaponInfo.SkillName} is broken and unusable!",
                0, false, weaponInfo.SkillName, SoundLevel.Quiet
            ), cancellationToken);
            return null;
        }

        // Apply off-hand penalty
        int attackSkill = weaponInfo.CurrentLevel + (isOffHand ? -2 : 0);

        // Apply weapon attack value modifier
        attackSkill += weaponInfo.AttackValueModifier;

        // Apply timed penalties
        attackSkill += await GetTotalTimedPenaltiesAsync(attackerId, attackerIsPlayer, cancellationToken);

        // Roll attack
        int attackRoll = _diceService.RollExploding4dF(); // 4dF+
        int attackValue = attackSkill + attackRoll;

        // Get defense
        int defenseSkill = await GetDefenseSkillAsync(targetData, targetId, targetIsPlayer, cancellationToken);
        int defenseRoll = _diceService.RollExploding4dF(); // 4dF+
        int targetValue = defenseSkill + defenseRoll;

        // Calculate success value
        int successValue = attackValue - targetValue;

        // Consume fatigue for attack
        await ApplyFatigueCostAsync(attackerId, attackerIsPlayer, fatCost, cancellationToken);

        // If defender used dodge, consume their fatigue
        if (targetData.CurrentFatigue > 0 && !await IsInParryModeAsync(targetId, targetIsPlayer, cancellationToken))
        {
            await ApplyFatigueCostAsync(targetId, targetIsPlayer, 1, cancellationToken);
        }

        // Check for failed attack penalties (SV <= -3)
        if (successValue <= -3)
        {
            await ApplyPhysicalityPenaltyAsync(attackerId, attackerIsPlayer, successValue, cancellationToken);
        }

        // If attack failed, log and return
        if (successValue < 0)
        {
            await LogCombatActionAsync(combatSessionId.Value, attackerId, attackerIsPlayer, targetId, targetIsPlayer,
                CombatActionType.MeleeAttack, attackValue, targetValue, successValue, null, null, null, null,
                $"{attackerData.Name} attacks {targetData.Name} but misses!", cancellationToken);

            await _messagePublisher.PublishAsync(new CombatAction(
                attackerId, attackerData.Name, targetId, targetData.Name,
                attackerData.RoomId, $"{attackerData.Name} attacks {targetData.Name} but misses!",
                0, false, weaponInfo.SkillName, SoundLevel.Normal
            ), cancellationToken);

            return successValue;
        }

        // Attack succeeded - roll physicality for damage bonus
        var physicalitySkill = await GetSkillAsync(attackerData, "Physicality", cancellationToken);
        int physicalityRoll = _diceService.RollExploding4dF();
        int physicalityResult = (physicalitySkill?.CurrentLevel ?? 10) + physicalityRoll;
        int resultValue = physicalityResult - 8;

        // Apply result value system to success value
        int modifiedSV = ApplyResultValueSystem(successValue, resultValue);

        // Apply weapon success value modifier
        modifiedSV += weaponInfo.SuccessValueModifier;

        // Apply timed penalty if physicality check failed badly
        if (resultValue <= -3)
        {
            await ApplyPhysicalityPenaltyAsync(attackerId, attackerIsPlayer, resultValue, cancellationToken);
        }

        // Determine hit location
        var hitLocation = RollHitLocation();

        // Get damage type from weapon
        var damageType = weaponInfo.DamageType;

        // Calculate damage after absorption
        int finalSV = await ApplyDefensesAsync(targetData, targetId, targetIsPlayer, modifiedSV, hitLocation, damageType, weaponInfo.DamageClass, cancellationToken);

        // Convert SV to damage
        var (fatDamage, vitDamage, wounds) = CalculateDamageFromSV(finalSV);

        // Apply pending damage
        await ApplyPendingDamageAsync(targetId, targetIsPlayer, fatDamage, vitDamage, wounds, cancellationToken);

        // Log combat action
        await LogCombatActionAsync(combatSessionId.Value, attackerId, attackerIsPlayer, targetId, targetIsPlayer,
            CombatActionType.MeleeAttack, attackValue, targetValue, successValue, fatDamage + vitDamage, fatDamage, vitDamage, wounds,
            $"{attackerData.Name} hits {targetData.Name} for {fatDamage + vitDamage} damage!", cancellationToken);

        await _messagePublisher.PublishAsync(new CombatAction(
            attackerId, attackerData.Name, targetId, targetData.Name,
            attackerData.RoomId, $"{attackerData.Name} hits {targetData.Name} dealing {fatDamage} FAT and {vitDamage} VIT damage!",
            fatDamage + vitDamage, true, weaponInfo.SkillName, SoundLevel.Normal
        ), cancellationToken);

        // Check if target died
        await CheckForDeathAsync(targetId, targetIsPlayer, targetData.Name, combatSessionId.Value, cancellationToken);

        return successValue;
    }

    public async Task<int?> PerformRangedAttackAsync(
        Guid attackerId,
        bool attackerIsPlayer,
        Guid targetId,
        bool targetIsPlayer,
        int range,
        CancellationToken cancellationToken = default)
    {
        // Ranged combat will be implemented in a future phase
        // This is a placeholder for the interface
        _logger.LogWarning("Ranged combat not yet implemented");
        return null;
    }

    public async Task<int> PerformKnockbackAsync(
        Guid attackerId,
        bool attackerIsPlayer,
        Guid targetId,
        bool targetIsPlayer,
        CancellationToken cancellationToken = default)
    {
        // Knockback will be implemented in a future phase
        _logger.LogWarning("Knockback not yet implemented");
        return 0;
    }

    public async Task SetParryModeAsync(
        Guid participantId,
        bool isPlayer,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var participant = await GetCombatParticipantAsync(participantId, isPlayer, cancellationToken);
        if (participant == null)
        {
            return;
        }

        participant.IsInParryMode = enabled;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Participant {Participant} parry mode: {Enabled}",
            participant.ParticipantName, enabled);
    }

    public async Task<bool> FleeFromCombatAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken = default)
    {
        var session = await GetActiveCombatSessionAsync(participantId, isPlayer, cancellationToken);
        if (session == null)
        {
            return false;
        }

        var participant = await GetCombatParticipantAsync(participantId, isPlayer, cancellationToken);
        if (participant == null)
        {
            return false;
        }

        // Mark participant as inactive
        participant.IsActive = false;
        participant.LeftAt = DateTimeOffset.UtcNow;
        participant.LeaveReason = "Fled";

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Participant {Participant} fled from combat", participant.ParticipantName);

        // Check if combat should end (only one active participant left)
        var activeParticipants = await _dbContext.CombatParticipants
            .Where(p => p.CombatSessionId == session.Id && p.IsActive)
            .CountAsync(cancellationToken);

        if (activeParticipants <= 1)
        {
            await EndCombatAsync(session.Id, "One participant fled", null, cancellationToken);
        }

        return true;
    }

    public async Task EndCombatAsync(
        Guid combatSessionId,
        string reason,
        Guid? winnerId = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.CombatSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == combatSessionId, cancellationToken);

        if (session == null)
        {
            return;
        }

        session.IsActive = false;
        session.EndedAt = DateTimeOffset.UtcNow;
        session.EndReason = reason;

        // Mark all participants as inactive
        foreach (var participant in session.Participants.Where(p => p.IsActive))
        {
            participant.IsActive = false;
            participant.LeftAt = DateTimeOffset.UtcNow;
            participant.LeaveReason = reason;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get winner name if provided
        string? winnerName = null;
        if (winnerId.HasValue)
        {
            var winner = session.Participants.FirstOrDefault(p =>
                p.CharacterId == winnerId || p.ActiveSpawn?.NpcId == winnerId);
            winnerName = winner?.ParticipantName;
        }

        await _messagePublisher.PublishAsync(new CombatEnded(
            session.RoomId,
            reason,
            winnerId,
            winnerName
        ), cancellationToken);

        _logger.LogInformation("Combat session {SessionId} ended: {Reason}", combatSessionId, reason);
    }

    public async Task<CombatSession?> GetActiveCombatSessionAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken = default)
    {
        if (isPlayer)
        {
            return await _dbContext.CombatSessions
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.IsActive &&
                    s.Participants.Any(p => p.CharacterId == participantId && p.IsActive),
                    cancellationToken);
        }
        else
        {
            // For NPCs, find the active spawn first
            var activeSpawn = await _dbContext.ActiveSpawns
                .Where(asp => asp.NpcId == participantId && asp.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeSpawn == null)
            {
                return null;
            }

            return await _dbContext.CombatSessions
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.IsActive &&
                    s.Participants.Any(p => p.ActiveSpawnId == activeSpawn.Id && p.IsActive),
                    cancellationToken);
        }
    }

    public async Task<bool> IsInCombatAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken = default)
    {
        var session = await GetActiveCombatSessionAsync(participantId, isPlayer, cancellationToken);
        return session != null;
    }

    public async Task<List<CombatParticipant>> GetCombatParticipantsAsync(
        Guid combatSessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CombatParticipants
            .Include(p => p.Character)
            .Include(p => p.ActiveSpawn)
            .Where(p => p.CombatSessionId == combatSessionId && p.IsActive)
            .ToListAsync(cancellationToken);
    }

    // Helper methods

    private async Task<(string Name, int RoomId)> GetParticipantInfoAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken)
    {
        if (isPlayer)
        {
            var character = await _dbContext.Characters
                .Where(c => c.Id == participantId)
                .Select(c => new { c.Name, c.CurrentRoomId })
                .FirstOrDefaultAsync(cancellationToken);

            return (character?.Name ?? "Unknown", character?.CurrentRoomId ?? 0);
        }
        else
        {
            var npc = await _dbContext.ActiveSpawns
                .Include(asp => asp.NpcTemplate)
                .Where(asp => asp.NpcId == participantId && asp.IsActive)
                .Select(asp => new { asp.NpcTemplate!.Name, asp.CurrentRoomId })
                .FirstOrDefaultAsync(cancellationToken);

            return (npc?.Name ?? "Unknown NPC", npc?.CurrentRoomId ?? 0);
        }
    }

    private async Task<CombatantData?> GetCombatantDataAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken)
    {
        if (isPlayer)
        {
            var character = await _dbContext.Characters
                .FirstOrDefaultAsync(c => c.Id == participantId, cancellationToken);

            if (character == null)
            {
                return null;
            }

            // For players, create skills from their base attributes
            // TODO: Load actual skill levels from Skills table when we have combat skills defined
            var skills = new List<SkillInfo>
            {
                new() { SkillName = "Physicality", CurrentLevel = character.Physicality },
                new() { SkillName = "Dodge", CurrentLevel = character.Dodge },
                new() { SkillName = "Drive", CurrentLevel = character.Drive },
                new() { SkillName = "Reasoning", CurrentLevel = character.Reasoning },
                new() { SkillName = "Awareness", CurrentLevel = character.Awareness },
                new() { SkillName = "Focus", CurrentLevel = character.Focus },
                new() { SkillName = "Bearing", CurrentLevel = character.Bearing }
            };

            return new CombatantData
            {
                Id = character.Id,
                Name = character.Name,
                RoomId = character.CurrentRoomId ?? 0,
                CurrentFatigue = character.CurrentFatigue,
                CurrentVitality = character.CurrentVitality,
                Skills = skills
            };
        }
        else
        {
            var npc = await _dbContext.ActiveSpawns
                .Include(asp => asp.NpcTemplate)
                .FirstOrDefaultAsync(asp => asp.NpcId == participantId && asp.IsActive, cancellationToken);

            if (npc == null)
            {
                return null;
            }

            // For NPCs, create skills from template attributes
            var skills = new List<SkillInfo>
            {
                new() { SkillName = "Physicality", CurrentLevel = npc.NpcTemplate!.Strength },
                new() { SkillName = "Dodge", CurrentLevel = npc.NpcTemplate!.Quickness },
                new() { SkillName = "Drive", CurrentLevel = npc.NpcTemplate!.Endurance },
                new() { SkillName = "Reasoning", CurrentLevel = npc.NpcTemplate!.Intelligence },
                new() { SkillName = "Awareness", CurrentLevel = npc.NpcTemplate!.Coordination },
                new() { SkillName = "Focus", CurrentLevel = npc.NpcTemplate!.Willpower },
                new() { SkillName = "Bearing", CurrentLevel = npc.NpcTemplate!.Charisma }
            };

            return new CombatantData
            {
                Id = npc.NpcId,
                Name = npc.NpcTemplate!.Name,
                RoomId = npc.CurrentRoomId ?? 0,
                CurrentFatigue = npc.CurrentFatigue,
                CurrentVitality = npc.CurrentVitality,
                Skills = skills
            };
        }
    }

    private async Task<WeaponInfo?> GetWeaponSkillAsync(
        CombatantData combatant,
        bool isOffHand,
        CancellationToken cancellationToken)
    {
        // Get equipped weapon from database
        var equippedSlot = isOffHand ? ArmorSlot.OffHand : ArmorSlot.MainHand;

        var equippedWeapon = await _dbContext.Items
            .Include(i => i.ItemTemplate)
                .ThenInclude(it => it.WeaponProperties)
            .Include(i => i.ItemTemplate)
                .ThenInclude(it => it.SkillBonuses)
            .FirstOrDefaultAsync(i =>
                (i.OwnerCharacterId == combatant.Id || (i.ContainerItem != null && i.ContainerItem.OwnerCharacterId == combatant.Id)) &&
                i.IsEquipped &&
                (i.EquippedSlot == equippedSlot || i.EquippedSlot == ArmorSlot.TwoHand),
                cancellationToken);

        if (equippedWeapon?.ItemTemplate?.WeaponProperties != null)
        {
            // Get weapon skill level (for now use Physicality, later integrate with skill system)
            var physicalitySkill = combatant.Skills.FirstOrDefault(s => s.SkillName == "Physicality");
            var baseSkillLevel = physicalitySkill?.CurrentLevel ?? 10;

            // Apply skill bonuses from the weapon itself
            var weaponSkillBonuses = equippedWeapon.ItemTemplate.SkillBonuses
                .Where(b => b.BonusType == "FlatBonus")
                .Sum(b => (int)b.BonusValue);

            return new WeaponInfo
            {
                SkillName = equippedWeapon.ItemTemplate.Name,
                CurrentLevel = baseSkillLevel + weaponSkillBonuses,
                AttackValueModifier = equippedWeapon.ItemTemplate.WeaponProperties.AttackValueModifier,
                SuccessValueModifier = equippedWeapon.ItemTemplate.WeaponProperties.BaseSuccessValueModifier,
                DamageType = equippedWeapon.ItemTemplate.WeaponProperties.DamageType,
                DamageClass = equippedWeapon.ItemTemplate.WeaponProperties.DamageClass,
                IsBroken = equippedWeapon.IsBroken
            };
        }

        // No weapon equipped - use unarmed combat
        var unarmedSkill = combatant.Skills.FirstOrDefault(s => s.SkillName == "Physicality");
        if (unarmedSkill == null)
        {
            return null;
        }

        return new WeaponInfo
        {
            SkillName = "Unarmed Combat",
            CurrentLevel = unarmedSkill.CurrentLevel,
            AttackValueModifier = 0,
            SuccessValueModifier = 0,
            DamageType = DamageType.Bashing,
            DamageClass = DamageClass.Class1,
            IsBroken = false
        };
    }

    private async Task<SkillInfo?> GetSkillAsync(
        CombatantData combatant,
        string skillName,
        CancellationToken cancellationToken)
    {
        return combatant.Skills.FirstOrDefault(s => s.SkillName == skillName);
    }

    private async Task<int> GetDefenseSkillAsync(
        CombatantData defender,
        Guid defenderId,
        bool defenderIsPlayer,
        CancellationToken cancellationToken)
    {
        int baseDefense;

        // Check if defender is in parry mode
        bool inParryMode = await IsInParryModeAsync(defenderId, defenderIsPlayer, cancellationToken);

        if (inParryMode)
        {
            // Use weapon skill for defense
            var weaponInfo = await GetWeaponSkillAsync(defender, false, cancellationToken);
            baseDefense = weaponInfo?.CurrentLevel ?? 10;
        }
        else
        {
            // Use dodge skill for defense
            var dodgeSkill = defender.Skills.FirstOrDefault(s => s.SkillName == "Dodge");
            baseDefense = dodgeSkill?.CurrentLevel ?? 10;

            // Apply equipment dodge modifiers (armor and weapons can affect dodge)
            var dodgeModifier = await GetEquipmentDodgeModifierAsync(defenderId, cancellationToken);
            baseDefense += dodgeModifier;
        }

        return baseDefense;
    }

    private async Task<int> GetEquipmentDodgeModifierAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var equippedItems = await _dbContext.Items
            .Include(i => i.ItemTemplate)
                .ThenInclude(it => it.WeaponProperties)
            .Include(i => i.ItemTemplate)
                .ThenInclude(it => it.ArmorProperties)
            .Where(i => i.OwnerCharacterId == characterId && i.IsEquipped)
            .ToListAsync(cancellationToken);

        int totalModifier = 0;

        foreach (var item in equippedItems)
        {
            if (item.IsBroken)
            {
                continue; // Broken equipment provides no bonuses
            }

            // Add weapon dodge modifier
            if (item.ItemTemplate.WeaponProperties != null)
            {
                totalModifier += item.ItemTemplate.WeaponProperties.DodgeModifier;
            }

            // Add armor dodge modifier
            if (item.ItemTemplate.ArmorProperties != null)
            {
                totalModifier += item.ItemTemplate.ArmorProperties.DodgeModifier;
            }
        }

        return totalModifier;
    }

    private async Task<bool> IsInParryModeAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken)
    {
        var participant = await GetCombatParticipantAsync(participantId, isPlayer, cancellationToken);
        return participant?.IsInParryMode ?? false;
    }

    private async Task<CombatParticipant?> GetCombatParticipantAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken)
    {
        if (isPlayer)
        {
            return await _dbContext.CombatParticipants
                .FirstOrDefaultAsync(p => p.CharacterId == participantId && p.IsActive, cancellationToken);
        }
        else
        {
            var activeSpawn = await _dbContext.ActiveSpawns
                .Where(asp => asp.NpcId == participantId && asp.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeSpawn == null)
            {
                return null;
            }

            return await _dbContext.CombatParticipants
                .FirstOrDefaultAsync(p => p.ActiveSpawnId == activeSpawn.Id && p.IsActive, cancellationToken);
        }
    }

    private async Task<int> GetTotalTimedPenaltiesAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken)
    {
        var participant = await GetCombatParticipantAsync(participantId, isPlayer, cancellationToken);
        if (participant == null || string.IsNullOrEmpty(participant.TimedPenaltiesJson))
        {
            return 0;
        }

        var penalties = JsonSerializer.Deserialize<List<TimedPenalty>>(participant.TimedPenaltiesJson) ?? new();
        var now = DateTimeOffset.UtcNow;

        // Remove expired penalties and sum active ones
        penalties = penalties.Where(p => p.ExpiresAt > now).ToList();

        // Update participant with cleaned penalties
        participant.TimedPenaltiesJson = penalties.Any()
            ? JsonSerializer.Serialize(penalties)
            : null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return penalties.Sum(p => p.PenaltyAmount);
    }

    private async Task ApplyPhysicalityPenaltyAsync(
        Guid participantId,
        bool isPlayer,
        int resultValue,
        CancellationToken cancellationToken)
    {
        if (resultValue > -3)
        {
            return;
        }

        var participant = await GetCombatParticipantAsync(participantId, isPlayer, cancellationToken);
        if (participant == null)
        {
            return;
        }

        // Determine penalty and duration from RVS table
        var (penalty, rounds) = resultValue switch
        {
            <= -9 => (-3, 3),
            <= -7 => (-2, 2),
            <= -5 => (-2, 1),
            <= -3 => (-1, 1),
            _ => (0, 0)
        };

        if (penalty == 0)
        {
            return;
        }

        var penalties = string.IsNullOrEmpty(participant.TimedPenaltiesJson)
            ? new List<TimedPenalty>()
            : JsonSerializer.Deserialize<List<TimedPenalty>>(participant.TimedPenaltiesJson) ?? new();

        penalties.Add(new TimedPenalty
        {
            PenaltyAmount = penalty,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(rounds * 3) // 1 round = 3 seconds
        });

        participant.TimedPenaltiesJson = JsonSerializer.Serialize(penalties);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Applied {Penalty} AV penalty to {Participant} for {Rounds} rounds",
            penalty, participant.ParticipantName, rounds);
    }

    private int ApplyResultValueSystem(int successValue, int resultValue)
    {
        // Apply RVS chart to modify success value based on physicality check
        int svModifier = resultValue switch
        {
            >= 12 => 4,
            >= 8 => 3,
            >= 4 => 2,
            >= 2 => 1,
            >= -2 => 0,
            _ => 0 // Negative RVs apply timed penalties, not SV modifiers
        };

        return successValue + svModifier;
    }

    private HitLocation RollHitLocation()
    {
        var roll = Random.Shared.Next(1, 13); // 1d12

        return roll switch
        {
            1 => Random.Shared.Next(1, 13) <= 6 ? HitLocation.Head : HitLocation.Torso,
            >= 2 and <= 6 => HitLocation.Torso,
            7 => HitLocation.LeftArm,
            8 => HitLocation.RightArm,
            >= 9 and <= 10 => HitLocation.LeftLeg,
            _ => HitLocation.RightLeg
        };
    }

    private async Task<int> ApplyDefensesAsync(
        CombatantData defender,
        Guid defenderId,
        bool defenderIsPlayer,
        int successValue,
        HitLocation hitLocation,
        DamageType damageType,
        DamageClass weaponClass,
        CancellationToken cancellationToken)
    {
        if (successValue < 0)
        {
            return successValue;
        }

        // Get all equipped armor pieces
        var equippedArmor = await _dbContext.Items
            .Include(i => i.ItemTemplate)
                .ThenInclude(it => it.ArmorProperties)
            .Where(i => i.OwnerCharacterId == defenderId && i.IsEquipped && i.ItemTemplate.ArmorProperties != null)
            .OrderBy(i => i.ItemTemplate.ArmorProperties!.LayerPriority)
            .ToListAsync(cancellationToken);

        if (!equippedArmor.Any())
        {
            return successValue; // No armor, no absorption
        }

        // Filter armor that covers the hit location
        var coveringArmor = equippedArmor
            .Where(armor => ArmorCoversLocation(armor, hitLocation))
            .ToList();

        if (!coveringArmor.Any())
        {
            return successValue; // No armor covering this location
        }

        int totalAbsorption = 0;

        // Calculate total absorption from all layers
        foreach (var armor in coveringArmor)
        {
            if (armor.IsBroken)
            {
                continue; // Broken armor provides no protection
            }

            var props = armor.ItemTemplate.ArmorProperties!;

            // Get absorption value based on damage type
            int absorption = damageType switch
            {
                DamageType.Bashing => props.BashingAbsorption,
                DamageType.Cutting => props.CuttingAbsorption,
                DamageType.Piercing => props.PiercingAbsorption,
                DamageType.Projectile => props.ProjectileAbsorption,
                DamageType.Energy => props.EnergyAbsorption,
                DamageType.Heat => props.HeatAbsorption,
                DamageType.Cold => props.ColdAbsorption,
                DamageType.Acid => props.AcidAbsorption,
                _ => 0
            };

            // Apply damage class scaling
            // Higher class weapons penetrate better against lower class armor
            int classModifier = (int)weaponClass - (int)props.DamageClass;
            if (classModifier > 0)
            {
                // Weapon class higher than armor class - reduce absorption
                absorption = Math.Max(0, absorption - classModifier);
            }

            totalAbsorption += absorption;
        }

        // Apply absorption to success value
        int finalSV = Math.Max(0, successValue - totalAbsorption);

        _logger.LogDebug("Armor absorption: {Absorption} reduced SV from {OriginalSV} to {FinalSV} for {Defender}",
            totalAbsorption, successValue, finalSV, defender.Name);

        return finalSV;
    }

    private bool ArmorCoversLocation(Item armor, HitLocation hitLocation)
    {
        var coverage = armor.ItemTemplate?.ArmorProperties?.HitLocationCoverage;
        if (string.IsNullOrWhiteSpace(coverage))
        {
            // If no coverage specified, assume it covers based on slot
            return armor.EquippedSlot switch
            {
                ArmorSlot.Head => hitLocation == HitLocation.Head,
                ArmorSlot.Chest => hitLocation == HitLocation.Torso,
                ArmorSlot.ArmLeft => hitLocation == HitLocation.LeftArm,
                ArmorSlot.ArmRight => hitLocation == HitLocation.RightArm,
                ArmorSlot.Legs => hitLocation == HitLocation.LeftLeg || hitLocation == HitLocation.RightLeg,
                _ => false
            };
        }

        // Parse coverage string (e.g., "Head,Torso,Arms")
        var coveredLocations = coverage.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToLowerInvariant())
            .ToHashSet();

        var locationName = hitLocation.ToString().ToLowerInvariant();
        if (coveredLocations.Contains(locationName))
        {
            return true;
        }

        // Check for common aliases
        return hitLocation switch
        {
            HitLocation.Torso => coveredLocations.Contains("torso") || coveredLocations.Contains("chest") || coveredLocations.Contains("body"),
            HitLocation.LeftArm or HitLocation.RightArm => coveredLocations.Contains("arms") || coveredLocations.Contains("arm"),
            HitLocation.LeftLeg or HitLocation.RightLeg => coveredLocations.Contains("legs") || coveredLocations.Contains("leg"),
            _ => false
        };
    }

    private (int FatDamage, int VitDamage, int Wounds) CalculateDamageFromSV(int successValue)
    {
        if (successValue < 0)
        {
            return (0, 0, 0);
        }

        // SV to damage dice conversion (simplified for Class 1)
        int damageRoll = successValue switch
        {
            0 => RollDice(1, 6) / 3,
            1 => RollDice(1, 6) / 2,
            2 => RollDice(1, 6),
            3 => RollDice(1, 8),
            4 => RollDice(1, 10),
            5 => RollDice(1, 12),
            6 => RollDice(1, 6) + RollDice(1, 8),
            7 => RollDice(2, 8),
            8 => RollDice(2, 10),
            9 => RollDice(2, 12),
            10 => RollDice(3, 10),
            11 => RollDice(3, 12),
            12 or 13 or 14 => RollDice(4, 10),
            >= 15 => RollDice(1, 6) * 10, // Class+1 damage (simplified)
            _ => 0
        };

        // Damage to health pools conversion
        return damageRoll switch
        {
            <= 4 => (damageRoll, 0, 0),
            5 => (5, 1, 0),
            6 => (6, 2, 0),
            7 => (7, 4, 1),
            8 => (8, 6, 1),
            9 => (9, 8, 1),
            10 => (10, 10, 2),
            11 => (11, 11, 2),
            12 => (12, 12, 2),
            13 => (13, 13, 2),
            14 => (14, 14, 2),
            15 => (15, 15, 3),
            >= 16 => (damageRoll, damageRoll, 3 + (damageRoll - 16) / 5)
        };
    }

    private int RollDice(int count, int sides)
    {
        int total = 0;
        for (int i = 0; i < count; i++)
        {
            total += Random.Shared.Next(1, sides + 1);
        }
        return total;
    }

    private async Task ApplyFatigueCostAsync(
        Guid participantId,
        bool isPlayer,
        int cost,
        CancellationToken cancellationToken)
    {
        if (isPlayer)
        {
            var character = await _dbContext.Characters
                .FirstOrDefaultAsync(c => c.Id == participantId, cancellationToken);

            if (character != null)
            {
                character.CurrentFatigue = Math.Max(0, character.CurrentFatigue - cost);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            var npc = await _dbContext.ActiveSpawns
                .FirstOrDefaultAsync(asp => asp.NpcId == participantId && asp.IsActive, cancellationToken);

            if (npc != null)
            {
                npc.CurrentFatigue = Math.Max(0, npc.CurrentFatigue - cost);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task ApplyPendingDamageAsync(
        Guid targetId,
        bool targetIsPlayer,
        int fatDamage,
        int vitDamage,
        int wounds,
        CancellationToken cancellationToken)
    {
        if (targetIsPlayer)
        {
            var character = await _dbContext.Characters
                .FirstOrDefaultAsync(c => c.Id == targetId, cancellationToken);

            if (character != null)
            {
                character.PendingFatigueDamage += fatDamage;
                character.PendingVitalityDamage += vitDamage;
                character.CurrentWounds += wounds;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            var npc = await _dbContext.ActiveSpawns
                .FirstOrDefaultAsync(asp => asp.NpcId == targetId && asp.IsActive, cancellationToken);

            if (npc != null)
            {
                npc.PendingFatigueDamage += fatDamage;
                npc.PendingVitalityDamage += vitDamage;
                npc.CurrentWounds += wounds;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task CheckForDeathAsync(
        Guid targetId,
        bool targetIsPlayer,
        string targetName,
        Guid combatSessionId,
        CancellationToken cancellationToken)
    {
        int currentVit = 0;

        if (targetIsPlayer)
        {
            var character = await _dbContext.Characters
                .FirstOrDefaultAsync(c => c.Id == targetId, cancellationToken);

            currentVit = character?.CurrentVitality ?? 0;
        }
        else
        {
            var npc = await _dbContext.ActiveSpawns
                .FirstOrDefaultAsync(asp => asp.NpcId == targetId && asp.IsActive, cancellationToken);

            currentVit = npc?.CurrentVitality ?? 0;

            if (currentVit <= 0 && npc != null)
            {
                // NPC died - despawn it
                npc.IsActive = false;
                npc.DeactivatedAt = DateTimeOffset.UtcNow;
                npc.DespawnReason = Game.Entities.DespawnReason.Death;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (currentVit <= 0)
        {
            _logger.LogInformation("{Target} has died in combat", targetName);
            await EndCombatAsync(combatSessionId, $"{targetName} died", null, cancellationToken);
        }
    }

    private async Task LogCombatActionAsync(
        Guid combatSessionId,
        Guid actorId,
        bool actorIsPlayer,
        Guid? targetId,
        bool? targetIsPlayer,
        CombatActionType actionType,
        int? attackRoll,
        int? defenseRoll,
        int? successValue,
        int? damageDealt,
        int? fatigueDamage,
        int? vitalityDamage,
        int? woundsInflicted,
        string? description,
        CancellationToken cancellationToken)
    {
        var actorParticipant = await GetCombatParticipantAsync(actorId, actorIsPlayer, cancellationToken);
        CombatParticipant? targetParticipant = null;

        if (targetId.HasValue && targetIsPlayer.HasValue)
        {
            targetParticipant = await GetCombatParticipantAsync(targetId.Value, targetIsPlayer.Value, cancellationToken);
        }

        var log = new CombatActionLog
        {
            CombatSessionId = combatSessionId,
            ActorParticipantId = actorParticipant?.Id ?? 0,
            TargetParticipantId = targetParticipant?.Id,
            ActionType = actionType,
            AttackRoll = attackRoll,
            DefenseRoll = defenseRoll,
            SuccessValue = successValue,
            DamageDealt = damageDealt,
            FatigueDamage = fatigueDamage,
            VitalityDamage = vitalityDamage,
            WoundsInflicted = woundsInflicted,
            Description = description
        };

        _dbContext.CombatActionLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private class CombatantData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public int CurrentFatigue { get; set; }
        public int CurrentVitality { get; set; }
        public List<SkillInfo> Skills { get; set; } = new();
    }

    private class SkillInfo
    {
        public string SkillName { get; set; } = string.Empty;
        public int CurrentLevel { get; set; }
    }

    private class WeaponInfo
    {
        public string SkillName { get; set; } = string.Empty;
        public int CurrentLevel { get; set; }
        public int AttackValueModifier { get; set; }
        public int SuccessValueModifier { get; set; }
        public DamageType DamageType { get; set; }
        public DamageClass DamageClass { get; set; }
        public bool IsBroken { get; set; }
    }
}
