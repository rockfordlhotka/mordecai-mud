using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using WebSkillDefinition = Mordecai.Web.Data.SkillDefinition;
using WebSkillUsageType = Mordecai.Web.Data.SkillUsageType;
using WebCharacterSkill = Mordecai.Web.Data.CharacterSkill;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for casting spells and managing spell effects.
/// Handles mana consumption, skill checks, effect application, and skill progression.
/// </summary>
public class SpellCastingService : ISpellCastingService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IManaService _manaService;
    private readonly ICharacterEffectService _characterEffectService;
    private readonly IRoomEffectService _roomEffectService;
    private readonly IDiceService _diceService;
    private readonly ISkillProgressionService _skillProgressionService;
    private readonly ILogger<SpellCastingService> _logger;

    // Spell skill type identifier
    private const string SpellSkillType = "Spell";

    // Default fatigue cost for casting spells
    private const int DefaultFatigueCost = 1;

    // Spell properties stored in CustomProperties JSON field
    private const string PropEffectType = "effectType";
    private const string PropBaseDamage = "baseDamage";
    private const string PropBaseHealing = "baseHealing";
    private const string PropEffectDefinitionId = "effectDefinitionId";
    private const string PropRoomEffectDefinitionId = "roomEffectDefinitionId";
    private const string PropDurationSeconds = "durationSeconds";
    private const string PropFatigueCost = "fatigueCost";

    public SpellCastingService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IManaService manaService,
        ICharacterEffectService characterEffectService,
        IRoomEffectService roomEffectService,
        IDiceService diceService,
        ISkillProgressionService skillProgressionService,
        ILogger<SpellCastingService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _manaService = manaService;
        _characterEffectService = characterEffectService;
        _roomEffectService = roomEffectService;
        _diceService = diceService;
        _skillProgressionService = skillProgressionService;
        _logger = logger;
    }

    public async Task<SpellCastResult> CastSpellAsync(
        SpellCastRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var result = new SpellCastResult();

        // 1. Validate caster exists and get character
        var caster = await context.Characters
            .FirstOrDefaultAsync(c => c.Id == request.CasterId, cancellationToken);

        if (caster == null)
        {
            result.Success = false;
            result.Message = "Caster not found.";
            return result;
        }

        // 2. Get spell skill definition
        var spellSkill = await context.SkillDefinitions
            .FirstOrDefaultAsync(sd => sd.Id == request.SpellSkillId && sd.SkillType == SpellSkillType, cancellationToken);

        if (spellSkill == null)
        {
            result.Success = false;
            result.Message = "Invalid spell skill.";
            return result;
        }

        // 3. Check if caster can cast the spell
        var (canCast, cannotCastReason) = await CanCastSpellAsync(request.CasterId, request.SpellSkillId, cancellationToken);
        if (!canCast)
        {
            result.Success = false;
            result.Message = cannotCastReason ?? "Cannot cast spell.";
            return result;
        }

        // 4. Get character's skill level
        var characterSkill = await context.CharacterSkills
            .Include(cs => cs.SkillDefinition)
            .FirstOrDefaultAsync(cs => cs.CharacterId == request.CasterId && cs.SkillDefinitionId == request.SpellSkillId, cancellationToken);

        int skillLevel = characterSkill?.Level ?? 0;

        // 5. Calculate ability score (attribute + skill - 5)
        int attributeValue = caster.GetAttributeValue(spellSkill.RelatedAttribute);
        int abilityScore = Math.Max(0, attributeValue + skillLevel - 5);
        result.AbilityScore = abilityScore;

        // 6. Roll dice (use exploding dice for spells as specified)
        int diceRoll = spellSkill.UsesExplodingDice ? _diceService.RollExploding4dF() : _diceService.Roll4dF();
        result.DiceRoll = diceRoll;
        result.IsCritical = diceRoll >= 4 && spellSkill.UsesExplodingDice;
        result.IsFumble = diceRoll <= -4 && spellSkill.UsesExplodingDice;

        // 7. Calculate success value
        int successValue = abilityScore + diceRoll;
        result.SuccessValue = successValue;

        // 8. Get spell info for costs and effects
        var spellInfo = await GetSpellInfoAsync(request.SpellSkillId, cancellationToken);
        if (spellInfo == null)
        {
            result.Success = false;
            result.Message = "Failed to load spell information.";
            return result;
        }

        // 9. Consume mana
        var school = spellInfo.School;
        var manaResult = await _manaService.ConsumeManaAsync(request.CasterId, school, spellInfo.ManaCost, cancellationToken);
        if (!manaResult.Success)
        {
            result.Success = false;
            result.Message = manaResult.Message;
            return result;
        }
        result.ManaConsumed = spellInfo.ManaCost;

        // 10. Consume fatigue
        int fatigueCost = spellInfo.FatigueCost;
        if (caster.CurrentFatigue >= fatigueCost)
        {
            caster.CurrentFatigue -= fatigueCost;
            result.FatigueConsumed = fatigueCost;
        }
        else
        {
            // Not enough fatigue - spell still succeeds but caster is exhausted
            result.FatigueConsumed = caster.CurrentFatigue;
            caster.CurrentFatigue = 0;
        }

        // 11. Update last used timestamp on character skill
        if (characterSkill != null)
        {
            characterSkill.LastUsedAt = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        // 12. Handle spell effect based on type
        bool spellSucceeded = successValue >= 0; // Basic success threshold

        if (result.IsFumble)
        {
            result.Success = false;
            result.Message = $"Your {spellInfo.Name} spell fizzles catastrophically! (Roll: {diceRoll})";
            // Log skill usage even on fumble (failed action)
            result.SkillProgression = await _skillProgressionService.LogUsageAsync(
                request.CasterId,
                request.SpellSkillId,
                WebSkillUsageType.RoutineUse,
                baseExperience: 1,
                actionSucceeded: false,
                context: "spell_cast",
                details: $"Fumbled {spellInfo.Name}",
                cancellationToken: cancellationToken);
            return result;
        }

        // Apply spell effects based on type
        switch (spellInfo.EffectType)
        {
            case SpellEffectType.TargetedDamage:
                await ApplyTargetedDamageAsync(request, result, spellInfo, successValue, caster, context, cancellationToken);
                break;

            case SpellEffectType.TargetedHealing:
                await ApplyTargetedHealingAsync(request, result, spellInfo, successValue, caster, context, cancellationToken);
                break;

            case SpellEffectType.SelfBuff:
                await ApplySelfBuffAsync(request, result, spellInfo, successValue, caster, cancellationToken);
                break;

            case SpellEffectType.TargetedBuff:
                await ApplyTargetedBuffAsync(request, result, spellInfo, successValue, caster, context, cancellationToken);
                break;

            case SpellEffectType.TargetedDebuff:
                await ApplyTargetedDebuffAsync(request, result, spellInfo, successValue, caster, context, cancellationToken);
                break;

            case SpellEffectType.AreaDamage:
                await ApplyAreaDamageAsync(request, result, spellInfo, successValue, caster, context, cancellationToken);
                break;

            case SpellEffectType.AreaHealing:
                await ApplyAreaHealingAsync(request, result, spellInfo, successValue, caster, context, cancellationToken);
                break;

            case SpellEffectType.RoomEffect:
                await ApplyRoomEffectAsync(request, result, spellInfo, successValue, caster, cancellationToken);
                break;

            default:
                result.Success = true;
                result.Message = $"You cast {spellInfo.Name} successfully.";
                break;
        }

        // 13. Log skill progression
        var usageType = result.IsCritical ? WebSkillUsageType.CriticalSuccess : WebSkillUsageType.RoutineUse;
        result.SkillProgression = await _skillProgressionService.LogUsageAsync(
            request.CasterId,
            request.SpellSkillId,
            usageType,
            baseExperience: 1,
            actionSucceeded: spellSucceeded,
            context: "spell_cast",
            details: $"Cast {spellInfo.Name} (SV: {successValue})",
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Character {CharacterId} cast {SpellName} with SV {SuccessValue} (AS: {AbilityScore} + Roll: {DiceRoll})",
            request.CasterId, spellInfo.Name, successValue, abilityScore, diceRoll);

        return result;
    }

    public async Task<(bool CanCast, string? Reason)> CanCastSpellAsync(
        Guid characterId,
        int spellSkillId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // 1. Check if character exists
        var character = await context.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character == null)
        {
            return (false, "Character not found.");
        }

        // 2. Check if spell skill exists and is a spell
        var spellSkill = await context.SkillDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(sd => sd.Id == spellSkillId && sd.SkillType == SpellSkillType, cancellationToken);

        if (spellSkill == null)
        {
            return (false, "Invalid spell skill.");
        }

        // 3. Check if character is silenced (cannot cast spells)
        var effectSummary = await _characterEffectService.GetEffectSummaryAsync(characterId, cancellationToken);
        if (!effectSummary.CanCastSpells)
        {
            return (false, "You are silenced and cannot cast spells.");
        }

        // 4. Check if character can act (not stunned)
        if (!effectSummary.CanAct)
        {
            return (false, "You cannot act right now.");
        }

        // 5. Check mana
        if (!string.IsNullOrEmpty(spellSkill.MagicSchool) && spellSkill.ManaCost.HasValue)
        {
            var school = ParseMagicSchool(spellSkill.MagicSchool);
            if (school.HasValue)
            {
                var hasEnoughMana = await _manaService.HasEnoughManaAsync(characterId, school.Value, spellSkill.ManaCost.Value, cancellationToken);
                if (!hasEnoughMana)
                {
                    return (false, $"Insufficient {spellSkill.MagicSchool} mana (need {spellSkill.ManaCost}).");
                }
            }
        }

        // 6. Check cooldown
        var cooldownRemaining = await GetSpellCooldownRemainingAsync(characterId, spellSkillId, cancellationToken);
        if (cooldownRemaining > 0)
        {
            return (false, $"Spell on cooldown ({cooldownRemaining:F1} seconds remaining).");
        }

        // 7. Check fatigue (optional - can cast at 0 but with penalties)
        if (character.CurrentFatigue <= 0)
        {
            // Allow casting but warn
            _logger.LogDebug("Character {CharacterId} casting spell while exhausted", characterId);
        }

        return (true, null);
    }

    public async Task<SpellInfo?> GetSpellInfoAsync(
        int spellSkillId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var skillDef = await context.SkillDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(sd => sd.Id == spellSkillId && sd.SkillType == SpellSkillType, cancellationToken);

        if (skillDef == null)
            return null;

        return CreateSpellInfoFromDefinition(skillDef);
    }

    public async Task<IReadOnlyList<(SpellInfo Spell, int Level)>> GetKnownSpellsAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var characterSpells = await context.CharacterSkills
            .AsNoTracking()
            .Include(cs => cs.SkillDefinition)
            .Where(cs => cs.CharacterId == characterId && cs.SkillDefinition.SkillType == SpellSkillType)
            .ToListAsync(cancellationToken);

        var result = new List<(SpellInfo Spell, int Level)>();
        foreach (var cs in characterSpells)
        {
            var spellInfo = CreateSpellInfoFromDefinition(cs.SkillDefinition);
            if (spellInfo != null)
            {
                result.Add((spellInfo, cs.Level));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<(SpellInfo Spell, int Level)>> GetKnownSpellsBySchoolAsync(
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken = default)
    {
        var allKnown = await GetKnownSpellsAsync(characterId, cancellationToken);
        return allKnown.Where(s => s.Spell.School == school).ToList();
    }

    public async Task<decimal> GetSpellCooldownRemainingAsync(
        Guid characterId,
        int spellSkillId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var skillDef = await context.SkillDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(sd => sd.Id == spellSkillId, cancellationToken);

        if (skillDef == null || skillDef.CooldownSeconds <= 0)
            return 0;

        var characterSkill = await context.CharacterSkills
            .AsNoTracking()
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == spellSkillId, cancellationToken);

        if (characterSkill?.LastUsedAt == null)
            return 0;

        var elapsed = (DateTimeOffset.UtcNow - characterSkill.LastUsedAt.Value).TotalSeconds;
        var remaining = (double)skillDef.CooldownSeconds - elapsed;

        return remaining > 0 ? (decimal)remaining : 0;
    }

    public async Task<bool> LearnSpellAsync(
        Guid characterId,
        int spellSkillId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Check if spell skill exists and is valid
        var spellSkill = await context.SkillDefinitions
            .FirstOrDefaultAsync(sd => sd.Id == spellSkillId && sd.SkillType == SpellSkillType, cancellationToken);

        if (spellSkill == null)
            return false;

        // Check if already known
        var existing = await context.CharacterSkills
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == spellSkillId, cancellationToken);

        if (existing != null)
            return false;

        // Create character skill at level 0
        var newSkill = new WebCharacterSkill
        {
            CharacterId = characterId,
            SkillDefinitionId = spellSkillId,
            Level = 0,
            Experience = 0,
            LearnedAt = DateTimeOffset.UtcNow
        };

        context.CharacterSkills.Add(newSkill);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Character {CharacterId} learned spell {SpellName}", characterId, spellSkill.Name);
        return true;
    }

    public async Task<IReadOnlyList<SpellInfo>> GetAllSpellsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var spellSkills = await context.SkillDefinitions
            .AsNoTracking()
            .Where(sd => sd.SkillType == SpellSkillType && sd.IsActive)
            .OrderBy(sd => sd.MagicSchool)
            .ThenBy(sd => sd.Name)
            .ToListAsync(cancellationToken);

        return spellSkills
            .Select(CreateSpellInfoFromDefinition)
            .Where(s => s != null)
            .Cast<SpellInfo>()
            .ToList();
    }

    public async Task<IReadOnlyList<SpellInfo>> GetSpellsBySchoolAsync(
        MagicSchool school,
        CancellationToken cancellationToken = default)
    {
        var allSpells = await GetAllSpellsAsync(cancellationToken);
        return allSpells.Where(s => s.School == school).ToList();
    }

    // ===================
    // Private Helpers
    // ===================

    private SpellInfo? CreateSpellInfoFromDefinition(WebSkillDefinition skillDef)
    {
        var school = ParseMagicSchool(skillDef.MagicSchool);
        if (!school.HasValue)
            return null;

        var spellInfo = new SpellInfo
        {
            SkillDefinitionId = skillDef.Id,
            Name = skillDef.Name,
            Description = skillDef.Description,
            School = school.Value,
            ManaCost = skillDef.ManaCost ?? 0,
            CooldownSeconds = skillDef.CooldownSeconds,
            UsesExplodingDice = skillDef.UsesExplodingDice,
            FatigueCost = DefaultFatigueCost
        };

        // Parse custom properties for spell-specific data
        if (!string.IsNullOrEmpty(skillDef.CustomProperties))
        {
            try
            {
                var props = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(skillDef.CustomProperties);
                if (props != null)
                {
                    if (props.TryGetValue(PropEffectType, out var effectTypeElem))
                    {
                        var effectTypeStr = effectTypeElem.GetString();
                        if (Enum.TryParse<SpellEffectType>(effectTypeStr, out var effectType))
                        {
                            spellInfo.EffectType = effectType;
                        }
                    }

                    if (props.TryGetValue(PropBaseDamage, out var baseDamageElem) && baseDamageElem.TryGetInt32(out var baseDamage))
                    {
                        spellInfo.BaseDamage = baseDamage;
                    }

                    if (props.TryGetValue(PropBaseHealing, out var baseHealingElem) && baseHealingElem.TryGetInt32(out var baseHealing))
                    {
                        spellInfo.BaseHealing = baseHealing;
                    }

                    if (props.TryGetValue(PropEffectDefinitionId, out var effectDefIdElem) && effectDefIdElem.TryGetInt32(out var effectDefId))
                    {
                        spellInfo.EffectDefinitionId = effectDefId;
                    }

                    if (props.TryGetValue(PropRoomEffectDefinitionId, out var roomEffectDefIdElem) && roomEffectDefIdElem.TryGetInt32(out var roomEffectDefId))
                    {
                        spellInfo.RoomEffectDefinitionId = roomEffectDefId;
                    }

                    if (props.TryGetValue(PropDurationSeconds, out var durationElem) && durationElem.TryGetInt32(out var duration))
                    {
                        spellInfo.DurationSeconds = duration;
                    }

                    if (props.TryGetValue(PropFatigueCost, out var fatigueCostElem) && fatigueCostElem.TryGetInt32(out var fatigueCost))
                    {
                        spellInfo.FatigueCost = fatigueCost;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse custom properties for spell {SpellId}", skillDef.Id);
            }
        }

        // Infer effect type from spell name if not explicitly set
        if (spellInfo.EffectType == default)
        {
            spellInfo.EffectType = InferEffectTypeFromName(skillDef.Name, skillDef.Description);
        }

        // Set default base damage/healing based on spell type
        if (spellInfo.EffectType is SpellEffectType.TargetedDamage or SpellEffectType.AreaDamage)
        {
            spellInfo.BaseDamage ??= spellInfo.ManaCost / 2 + 1; // Simple formula
        }
        else if (spellInfo.EffectType is SpellEffectType.TargetedHealing or SpellEffectType.AreaHealing)
        {
            spellInfo.BaseHealing ??= spellInfo.ManaCost / 2 + 1;
        }

        return spellInfo;
    }

    private static SpellEffectType InferEffectTypeFromName(string name, string description)
    {
        var lower = (name + " " + description).ToLowerInvariant();

        if (lower.Contains("heal") || lower.Contains("restore") || lower.Contains("mend"))
        {
            if (lower.Contains("mass") || lower.Contains("area") || lower.Contains("all"))
                return SpellEffectType.AreaHealing;
            return SpellEffectType.TargetedHealing;
        }

        if (lower.Contains("bolt") || lower.Contains("strike") || lower.Contains("burn") || lower.Contains("shock") || lower.Contains("damage"))
        {
            if (lower.Contains("fireball") || lower.Contains("mass") || lower.Contains("area") || lower.Contains("earthquake"))
                return SpellEffectType.AreaDamage;
            return SpellEffectType.TargetedDamage;
        }

        if (lower.Contains("invisible") || lower.Contains("shield") || lower.Contains("strength") || lower.Contains("haste"))
        {
            if (lower.Contains("self") || lower.Contains("caster"))
                return SpellEffectType.SelfBuff;
            return SpellEffectType.TargetedBuff;
        }

        if (lower.Contains("slow") || lower.Contains("weaken") || lower.Contains("curse") || lower.Contains("blind"))
        {
            return SpellEffectType.TargetedDebuff;
        }

        if (lower.Contains("wall") || lower.Contains("fog") || lower.Contains("entangle") || lower.Contains("zone") || lower.Contains("area effect"))
        {
            return SpellEffectType.RoomEffect;
        }

        // Default to targeted damage for offensive spells
        return SpellEffectType.TargetedDamage;
    }

    private static MagicSchool? ParseMagicSchool(string? schoolName)
    {
        if (string.IsNullOrEmpty(schoolName))
            return null;

        return schoolName.ToLowerInvariant() switch
        {
            "fire" => MagicSchool.Fire,
            "healing" => MagicSchool.Healing,
            "lightning" => MagicSchool.Lightning,
            "illusion" => MagicSchool.Illusion,
            _ => null
        };
    }

    private async Task ApplyTargetedDamageAsync(
        SpellCastRequest request,
        SpellCastResult result,
        SpellInfo spellInfo,
        int successValue,
        Character caster,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        // Validate target
        if (!request.TargetCharacterId.HasValue && !request.TargetNpcId.HasValue)
        {
            result.Success = false;
            result.Message = "No target specified for damage spell.";
            return;
        }

        Guid targetId;
        string targetName;
        bool isPlayer;

        if (request.TargetCharacterId.HasValue)
        {
            var target = await context.Characters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.TargetCharacterId.Value, cancellationToken);

            if (target == null)
            {
                result.Success = false;
                result.Message = "Target not found.";
                return;
            }

            targetId = target.Id;
            targetName = target.Name;
            isPlayer = true;

            // Calculate target's defense (Focus/WIL based for magic resistance)
            int targetDefense = Math.Max(0, target.Focus - 5);
            result.TargetDefenseValue = targetDefense;

            // Calculate damage
            int baseDamage = spellInfo.BaseDamage ?? 3;
            int damage = Math.Max(0, baseDamage + (successValue - targetDefense));
            result.EffectValue = damage;

            // Apply damage to target (as fatigue damage first, then vitality)
            if (damage > 0)
            {
                // Apply to pending damage pools (will be processed by background service)
                target = await context.Characters
                    .FirstAsync(c => c.Id == request.TargetCharacterId.Value, cancellationToken);

                target.PendingFatigueDamage += damage;
                await context.SaveChangesAsync(cancellationToken);

                result.AppliedEffects.Add(new SpellEffectApplication
                {
                    TargetId = targetId,
                    TargetName = targetName,
                    IsPlayer = isPlayer,
                    EffectType = "Damage",
                    EffectValue = damage
                });
            }

            result.Success = true;
            result.Message = damage > 0
                ? $"Your {spellInfo.Name} hits {targetName} for {damage} damage!"
                : $"Your {spellInfo.Name} fails to affect {targetName}.";
        }
        else
        {
            // NPC target - TODO: implement NPC damage when NPC service is available
            result.Success = true;
            result.Message = $"You cast {spellInfo.Name} at the target.";
        }
    }

    private async Task ApplyTargetedHealingAsync(
        SpellCastRequest request,
        SpellCastResult result,
        SpellInfo spellInfo,
        int successValue,
        Character caster,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        // Default target is self if not specified
        var targetId = request.TargetCharacterId ?? caster.Id;

        var target = await context.Characters
            .FirstOrDefaultAsync(c => c.Id == targetId, cancellationToken);

        if (target == null)
        {
            result.Success = false;
            result.Message = "Target not found.";
            return;
        }

        // Calculate healing
        int baseHealing = spellInfo.BaseHealing ?? 3;
        int healing = Math.Max(1, baseHealing + successValue);
        result.EffectValue = healing;

        // Apply healing (prioritize vitality, then fatigue)
        int actualHealing = 0;

        // First heal vitality if below max
        if (target.CurrentVitality < target.MaxVitality)
        {
            int vitalityHealing = Math.Min(healing, target.MaxVitality - target.CurrentVitality);
            target.CurrentVitality += vitalityHealing;
            actualHealing += vitalityHealing;
            healing -= vitalityHealing;
        }

        // Then heal fatigue with remaining healing
        if (healing > 0 && target.CurrentFatigue < target.MaxFatigue)
        {
            int fatigueHealing = Math.Min(healing, target.MaxFatigue - target.CurrentFatigue);
            target.CurrentFatigue += fatigueHealing;
            actualHealing += fatigueHealing;
        }

        await context.SaveChangesAsync(cancellationToken);

        result.AppliedEffects.Add(new SpellEffectApplication
        {
            TargetId = target.Id,
            TargetName = target.Name,
            IsPlayer = true,
            EffectType = "Healing",
            EffectValue = actualHealing
        });

        result.Success = true;
        var targetDesc = target.Id == caster.Id ? "yourself" : target.Name;
        result.Message = actualHealing > 0
            ? $"Your {spellInfo.Name} heals {targetDesc} for {actualHealing} health!"
            : $"{targetDesc} is already at full health.";
    }

    private async Task ApplySelfBuffAsync(
        SpellCastRequest request,
        SpellCastResult result,
        SpellInfo spellInfo,
        int successValue,
        Character caster,
        CancellationToken cancellationToken)
    {
        if (!spellInfo.EffectDefinitionId.HasValue)
        {
            result.Success = true;
            result.Message = $"You cast {spellInfo.Name} on yourself.";
            return;
        }

        // Calculate duration based on success value
        int baseDuration = spellInfo.DurationSeconds ?? 60;
        int duration = baseDuration + (successValue * 10); // +10 seconds per success level
        decimal intensity = request.IntensityOverride ?? 1.0m + (successValue * 0.1m);

        var effectResult = await _characterEffectService.ApplyEffectAsync(
            caster.Id,
            spellInfo.EffectDefinitionId.Value,
            sourceCharacterId: caster.Id,
            sourceSpellSkillId: spellInfo.SkillDefinitionId,
            durationSeconds: duration,
            intensity: intensity,
            cancellationToken: cancellationToken);

        if (effectResult.Success && effectResult.Effect != null)
        {
            result.AppliedEffects.Add(new SpellEffectApplication
            {
                TargetId = caster.Id,
                TargetName = caster.Name,
                IsPlayer = true,
                EffectType = "Buff",
                EffectValue = duration,
                AppliedEffect = effectResult.Effect
            });
        }

        result.Success = true;
        result.Message = $"You cast {spellInfo.Name} on yourself for {duration} seconds.";
    }

    private async Task ApplyTargetedBuffAsync(
        SpellCastRequest request,
        SpellCastResult result,
        SpellInfo spellInfo,
        int successValue,
        Character caster,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var targetId = request.TargetCharacterId ?? caster.Id;

        var target = await context.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == targetId, cancellationToken);

        if (target == null)
        {
            result.Success = false;
            result.Message = "Target not found.";
            return;
        }

        if (!spellInfo.EffectDefinitionId.HasValue)
        {
            result.Success = true;
            result.Message = $"You cast {spellInfo.Name} on {target.Name}.";
            return;
        }

        int baseDuration = spellInfo.DurationSeconds ?? 60;
        int duration = baseDuration + (successValue * 10);
        decimal intensity = request.IntensityOverride ?? 1.0m + (successValue * 0.1m);

        var effectResult = await _characterEffectService.ApplyEffectAsync(
            targetId,
            spellInfo.EffectDefinitionId.Value,
            sourceCharacterId: caster.Id,
            sourceSpellSkillId: spellInfo.SkillDefinitionId,
            durationSeconds: duration,
            intensity: intensity,
            cancellationToken: cancellationToken);

        if (effectResult.Success && effectResult.Effect != null)
        {
            result.AppliedEffects.Add(new SpellEffectApplication
            {
                TargetId = target.Id,
                TargetName = target.Name,
                IsPlayer = true,
                EffectType = "Buff",
                EffectValue = duration,
                AppliedEffect = effectResult.Effect
            });
        }

        result.Success = true;
        var targetDesc = target.Id == caster.Id ? "yourself" : target.Name;
        result.Message = $"You cast {spellInfo.Name} on {targetDesc} for {duration} seconds.";
    }

    private async Task ApplyTargetedDebuffAsync(
        SpellCastRequest request,
        SpellCastResult result,
        SpellInfo spellInfo,
        int successValue,
        Character caster,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        if (!request.TargetCharacterId.HasValue && !request.TargetNpcId.HasValue)
        {
            result.Success = false;
            result.Message = "No target specified for debuff spell.";
            return;
        }

        if (request.TargetCharacterId.HasValue)
        {
            var target = await context.Characters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.TargetCharacterId.Value, cancellationToken);

            if (target == null)
            {
                result.Success = false;
                result.Message = "Target not found.";
                return;
            }

            // Check resistance (Focus vs spell success)
            int targetResistance = Math.Max(0, target.Focus - 5);
            result.TargetDefenseValue = targetResistance;

            if (successValue <= targetResistance)
            {
                result.Success = true;
                result.Message = $"{target.Name} resists your {spellInfo.Name}!";
                return;
            }

            if (!spellInfo.EffectDefinitionId.HasValue)
            {
                result.Success = true;
                result.Message = $"You cast {spellInfo.Name} on {target.Name}.";
                return;
            }

            int baseDuration = spellInfo.DurationSeconds ?? 30;
            int duration = baseDuration + ((successValue - targetResistance) * 5);
            decimal intensity = request.IntensityOverride ?? 1.0m;

            var effectResult = await _characterEffectService.ApplyEffectAsync(
                request.TargetCharacterId.Value,
                spellInfo.EffectDefinitionId.Value,
                sourceCharacterId: caster.Id,
                sourceSpellSkillId: spellInfo.SkillDefinitionId,
                durationSeconds: duration,
                intensity: intensity,
                cancellationToken: cancellationToken);

            if (effectResult.Success && effectResult.Effect != null)
            {
                result.AppliedEffects.Add(new SpellEffectApplication
                {
                    TargetId = target.Id,
                    TargetName = target.Name,
                    IsPlayer = true,
                    EffectType = "Debuff",
                    EffectValue = duration,
                    ResistanceApplied = true,
                    ResistanceRoll = targetResistance,
                    AppliedEffect = effectResult.Effect
                });
            }

            result.Success = true;
            result.Message = $"Your {spellInfo.Name} afflicts {target.Name} for {duration} seconds!";
        }
        else
        {
            // NPC target - TODO
            result.Success = true;
            result.Message = $"You cast {spellInfo.Name} at the target.";
        }
    }

    private async Task ApplyAreaDamageAsync(
        SpellCastRequest request,
        SpellCastResult result,
        SpellInfo spellInfo,
        int successValue,
        Character caster,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        // Get all characters in the same room as the caster (except caster)
        if (!caster.CurrentRoomId.HasValue)
        {
            result.Success = false;
            result.Message = "You must be in a room to cast area spells.";
            return;
        }

        var targetsInRoom = await context.Characters
            .Where(c => c.CurrentRoomId == caster.CurrentRoomId && c.Id != caster.Id)
            .ToListAsync(cancellationToken);

        int baseDamage = spellInfo.BaseDamage ?? 3;
        int totalDamage = 0;

        foreach (var target in targetsInRoom)
        {
            int targetDefense = Math.Max(0, target.Focus - 5);
            int damage = Math.Max(0, baseDamage + (successValue - targetDefense));

            if (damage > 0)
            {
                target.PendingFatigueDamage += damage;
                totalDamage += damage;

                result.AppliedEffects.Add(new SpellEffectApplication
                {
                    TargetId = target.Id,
                    TargetName = target.Name,
                    IsPlayer = true,
                    EffectType = "Damage",
                    EffectValue = damage
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        result.EffectValue = totalDamage;
        result.Success = true;
        result.Message = targetsInRoom.Count > 0
            ? $"Your {spellInfo.Name} hits {targetsInRoom.Count} target(s) for a total of {totalDamage} damage!"
            : $"Your {spellInfo.Name} erupts, but there are no targets in range.";
    }

    private async Task ApplyAreaHealingAsync(
        SpellCastRequest request,
        SpellCastResult result,
        SpellInfo spellInfo,
        int successValue,
        Character caster,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        if (!caster.CurrentRoomId.HasValue)
        {
            result.Success = false;
            result.Message = "You must be in a room to cast area spells.";
            return;
        }

        // Include caster in healing
        var targetsInRoom = await context.Characters
            .Where(c => c.CurrentRoomId == caster.CurrentRoomId)
            .ToListAsync(cancellationToken);

        int baseHealing = spellInfo.BaseHealing ?? 3;
        int healing = Math.Max(1, baseHealing + successValue);
        int totalHealing = 0;

        foreach (var target in targetsInRoom)
        {
            int actualHealing = 0;

            if (target.CurrentVitality < target.MaxVitality)
            {
                int vitalityHealing = Math.Min(healing, target.MaxVitality - target.CurrentVitality);
                target.CurrentVitality += vitalityHealing;
                actualHealing += vitalityHealing;
            }

            if (actualHealing > 0)
            {
                totalHealing += actualHealing;
                result.AppliedEffects.Add(new SpellEffectApplication
                {
                    TargetId = target.Id,
                    TargetName = target.Name,
                    IsPlayer = true,
                    EffectType = "Healing",
                    EffectValue = actualHealing
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        result.EffectValue = totalHealing;
        result.Success = true;
        result.Message = totalHealing > 0
            ? $"Your {spellInfo.Name} heals {result.AppliedEffects.Count} target(s) for a total of {totalHealing} health!"
            : $"Everyone in the area is already at full health.";
    }

    private async Task ApplyRoomEffectAsync(
        SpellCastRequest request,
        SpellCastResult result,
        SpellInfo spellInfo,
        int successValue,
        Character caster,
        CancellationToken cancellationToken)
    {
        var roomId = request.TargetRoomId ?? caster.CurrentRoomId;
        if (!roomId.HasValue)
        {
            result.Success = false;
            result.Message = "No room target for room effect spell.";
            return;
        }

        if (!spellInfo.RoomEffectDefinitionId.HasValue)
        {
            result.Success = true;
            result.Message = $"You cast {spellInfo.Name} on the room.";
            return;
        }

        // Calculate duration and intensity based on success
        int baseDuration = spellInfo.DurationSeconds ?? 60;
        int duration = baseDuration + (successValue * 10);
        decimal intensity = request.IntensityOverride ?? 1.0m + (successValue * 0.1m);

        var roomEffect = await _roomEffectService.ApplyEffectAsync(
            roomId.Value,
            spellInfo.RoomEffectDefinitionId.Value,
            sourceType: "Spell",
            sourceId: spellInfo.SkillDefinitionId.ToString(),
            sourceName: spellInfo.Name,
            casterCharacterId: caster.Id,
            intensity: intensity,
            durationOverride: duration,
            cancellationToken: cancellationToken);

        result.CreatedRoomEffect = roomEffect;
        result.EffectValue = duration;
        result.Success = true;
        result.Message = $"Your {spellInfo.Name} creates a magical effect in the room for {duration} seconds!";
    }
}
