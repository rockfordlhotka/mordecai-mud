using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Seeds the database with common character effect definitions
/// </summary>
public class CharacterEffectSeedService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<CharacterEffectSeedService> _logger;

    public CharacterEffectSeedService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ILogger<CharacterEffectSeedService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Check if already seeded
        if (await context.CharacterEffectDefinitions.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Character effects already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding character effect definitions...");

        var effects = new List<CharacterEffectDefinition>();

        // =====================
        // WOUND EFFECT
        // =====================
        var wound = new CharacterEffectDefinition
        {
            Name = "Wound",
            Description = "A serious injury that impairs combat ability and slowly drains fatigue. Heals naturally over 4 hours.",
            EffectType = CharacterEffectType.Wound,
            DefaultDurationSeconds = 0, // Permanent until healed
            DefaultIntensity = 1.0m,
            IsStackable = true,
            MaxStacks = 10, // Max wounds per location
            TickIntervalSeconds = 6, // Every 6 seconds (2 rounds)
            IsVisible = true,
            IsVisibleToOthers = true,
            IconName = "wound",
            EffectColor = "#8B0000", // Dark red
            IsDispellable = true,
            IsSystemEffect = true,
            CreatedBy = "system"
        };
        wound.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.PeriodicFatigueDamage,
            ModifierValue = 1, // 1 FAT damage per tick per stack
            ScalesWithIntensity = false
        });
        // Note: -2 AV per wound is handled directly in CharacterEffectSummary
        effects.Add(wound);

        // =====================
        // BUFF EFFECTS
        // =====================
        
        // Strength Buff
        effects.Add(CreateAttributeBuff(
            "Strength",
            "Physicality",
            "Magically enhanced physical strength.",
            2, // +2 Physicality
            300, // 5 minutes
            "#FFD700" // Gold
        ));

        // Agility Buff
        effects.Add(CreateAttributeBuff(
            "Agility",
            "Dodge",
            "Magically enhanced agility and reflexes.",
            2, // +2 Dodge
            300,
            "#00CED1" // Dark Turquoise
        ));

        // Endurance Buff
        effects.Add(CreateAttributeBuff(
            "Endurance",
            "Drive",
            "Magically enhanced stamina and endurance.",
            2, // +2 Drive
            300,
            "#32CD32" // Lime Green
        ));

        // Protection (damage reduction)
        var protection = new CharacterEffectDefinition
        {
            Name = "Protection",
            Description = "A magical barrier that reduces incoming damage.",
            EffectType = CharacterEffectType.Buff,
            DefaultDurationSeconds = 300,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IconName = "shield",
            EffectColor = "#4169E1", // Royal Blue
            IsDispellable = true,
            CreatedBy = "system"
        };
        protection.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.ModifyDamageReceived,
            ModifierValue = -0.20m, // -20% damage taken
            IsPercentage = true,
            ScalesWithIntensity = true
        });
        effects.Add(protection);

        // Battle Focus (+AV)
        var battleFocus = new CharacterEffectDefinition
        {
            Name = "Battle Focus",
            Description = "Enhanced combat awareness grants a bonus to attack rolls.",
            EffectType = CharacterEffectType.Buff,
            DefaultDurationSeconds = 180, // 3 minutes
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IconName = "crosshairs",
            EffectColor = "#FF4500", // Orange Red
            IsDispellable = true,
            CreatedBy = "system"
        };
        battleFocus.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.ModifyAttackValue,
            ModifierValue = 2, // +2 AV
            ScalesWithIntensity = true
        });
        effects.Add(battleFocus);

        // Iron Skin (+SV)
        var ironSkin = new CharacterEffectDefinition
        {
            Name = "Iron Skin",
            Description = "Hardened skin provides a defensive bonus.",
            EffectType = CharacterEffectType.Buff,
            DefaultDurationSeconds = 180,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IconName = "shield-alt",
            EffectColor = "#708090", // Slate Gray
            IsDispellable = true,
            CreatedBy = "system"
        };
        ironSkin.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.ModifyDefenseValue,
            ModifierValue = 2, // +2 SV
            ScalesWithIntensity = true
        });
        effects.Add(ironSkin);

        // =====================
        // DEBUFF EFFECTS
        // =====================

        // Weakness
        effects.Add(CreateAttributeDebuff(
            "Weakness",
            "Physicality",
            "Magically drained of physical strength.",
            -2, // -2 Physicality
            180, // 3 minutes
            "#800080" // Purple
        ));

        // Slow
        var slow = new CharacterEffectDefinition
        {
            Name = "Slow",
            Description = "Magically slowed reflexes impair dodge ability.",
            EffectType = CharacterEffectType.Debuff,
            DefaultDurationSeconds = 120, // 2 minutes
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IconName = "hourglass",
            EffectColor = "#4B0082", // Indigo
            IsDispellable = true,
            CreatedBy = "system"
        };
        slow.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.ModifyAttribute,
            TargetAttribute = "Dodge",
            ModifierValue = -3,
            ScalesWithIntensity = true
        });
        effects.Add(slow);

        // Curse (reduced damage output)
        var curse = new CharacterEffectDefinition
        {
            Name = "Curse",
            Description = "A dark curse that weakens all attacks.",
            EffectType = CharacterEffectType.Debuff,
            DefaultDurationSeconds = 300,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IconName = "skull",
            EffectColor = "#2F4F4F", // Dark Slate Gray
            IsDispellable = true,
            CreatedBy = "system"
        };
        curse.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.ModifyDamageDealt,
            ModifierValue = -0.25m, // -25% damage dealt
            IsPercentage = true,
            ScalesWithIntensity = true
        });
        effects.Add(curse);

        // =====================
        // DAMAGE OVER TIME
        // =====================

        // Poison
        var poison = new CharacterEffectDefinition
        {
            Name = "Poison",
            Description = "Toxic venom courses through your veins, dealing periodic vitality damage.",
            EffectType = CharacterEffectType.DamageOverTime,
            DefaultDurationSeconds = 60, // 1 minute
            DefaultIntensity = 1.0m,
            IsStackable = true,
            MaxStacks = 5,
            TickIntervalSeconds = 6, // Every 6 seconds
            IsVisible = true,
            IconName = "biohazard",
            EffectColor = "#00FF00", // Green
            IsDispellable = true,
            CreatedBy = "system"
        };
        poison.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.PeriodicVitalityDamage,
            ModifierValue = 2, // 2 VIT damage per tick per stack
            DamageType = "Poison",
            ScalesWithIntensity = true
        });
        effects.Add(poison);

        // Burning
        var burning = new CharacterEffectDefinition
        {
            Name = "Burning",
            Description = "Engulfed in flames, taking periodic fire damage.",
            EffectType = CharacterEffectType.DamageOverTime,
            DefaultDurationSeconds = 30, // 30 seconds
            DefaultIntensity = 1.0m,
            IsStackable = true,
            MaxStacks = 3,
            TickIntervalSeconds = 3, // Every 3 seconds
            IsVisible = true,
            IsVisibleToOthers = true,
            IconName = "fire",
            EffectColor = "#FF4500", // Orange Red
            IsDispellable = true,
            CreatedBy = "system"
        };
        burning.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.PeriodicVitalityDamage,
            ModifierValue = 3, // 3 VIT damage per tick per stack
            DamageType = "Fire",
            ScalesWithIntensity = true
        });
        effects.Add(burning);

        // =====================
        // HEAL OVER TIME
        // =====================

        // Regeneration
        var regeneration = new CharacterEffectDefinition
        {
            Name = "Regeneration",
            Description = "Magical healing energy restores vitality over time.",
            EffectType = CharacterEffectType.HealOverTime,
            DefaultDurationSeconds = 60, // 1 minute
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 6, // Every 6 seconds
            IsVisible = true,
            IconName = "heart",
            EffectColor = "#FF69B4", // Hot Pink
            IsDispellable = false, // Can't dispel healing
            CreatedBy = "system"
        };
        regeneration.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.PeriodicVitalityHealing,
            ModifierValue = 3, // 3 VIT healing per tick
            ScalesWithIntensity = true
        });
        effects.Add(regeneration);

        // Rejuvenation (fatigue recovery)
        var rejuvenation = new CharacterEffectDefinition
        {
            Name = "Rejuvenation",
            Description = "Refreshing energy restores fatigue over time.",
            EffectType = CharacterEffectType.HealOverTime,
            DefaultDurationSeconds = 60,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 6,
            IsVisible = true,
            IconName = "bolt",
            EffectColor = "#00BFFF", // Deep Sky Blue
            IsDispellable = false,
            CreatedBy = "system"
        };
        rejuvenation.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.PeriodicFatigueHealing,
            ModifierValue = 2, // 2 FAT healing per tick
            ScalesWithIntensity = true
        });
        effects.Add(rejuvenation);

        // =====================
        // STATUS EFFECTS
        // =====================

        // Invisibility
        var invisibility = new CharacterEffectDefinition
        {
            Name = "Invisibility",
            Description = "Magically concealed from sight.",
            EffectType = CharacterEffectType.StatusEffect,
            DefaultDurationSeconds = 120, // 2 minutes
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IsVisibleToOthers = false, // Others can't see you're invisible
            IconName = "eye-slash",
            EffectColor = "#E6E6FA", // Lavender
            IsDispellable = true,
            CreatedBy = "system"
        };
        invisibility.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.Invisibility,
            ModifierValue = 1,
            ScalesWithIntensity = false
        });
        effects.Add(invisibility);

        // Stunned
        var stunned = new CharacterEffectDefinition
        {
            Name = "Stunned",
            Description = "Unable to take any actions.",
            EffectType = CharacterEffectType.StatusEffect,
            DefaultDurationSeconds = 6, // 1 round
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IconName = "dizzy",
            EffectColor = "#FFFF00", // Yellow
            IsDispellable = false, // Can't dispel stun
            CreatedBy = "system"
        };
        stunned.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.PreventActions,
            ModifierValue = 1,
            ScalesWithIntensity = false
        });
        effects.Add(stunned);

        // Rooted
        var rooted = new CharacterEffectDefinition
        {
            Name = "Rooted",
            Description = "Unable to move but can still fight.",
            EffectType = CharacterEffectType.StatusEffect,
            DefaultDurationSeconds = 15,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IconName = "anchor",
            EffectColor = "#8B4513", // Saddle Brown
            IsDispellable = true,
            CreatedBy = "system"
        };
        rooted.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.PreventMovement,
            ModifierValue = 1,
            ScalesWithIntensity = false
        });
        effects.Add(rooted);

        // Silenced
        var silenced = new CharacterEffectDefinition
        {
            Name = "Silenced",
            Description = "Unable to cast spells.",
            EffectType = CharacterEffectType.StatusEffect,
            DefaultDurationSeconds = 30,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IconName = "volume-mute",
            EffectColor = "#C0C0C0", // Silver
            IsDispellable = true,
            CreatedBy = "system"
        };
        silenced.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.PreventSpellcasting,
            ModifierValue = 1,
            ScalesWithIntensity = false
        });
        effects.Add(silenced);

        // Save all effects
        context.CharacterEffectDefinitions.AddRange(effects);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} character effect definitions", effects.Count);
    }

    private static CharacterEffectDefinition CreateAttributeBuff(
        string name, string attribute, string description, int modifier, int durationSeconds, string color)
    {
        var effect = new CharacterEffectDefinition
        {
            Name = name,
            Description = description,
            EffectType = CharacterEffectType.Buff,
            DefaultDurationSeconds = durationSeconds,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IconName = "arrow-up",
            EffectColor = color,
            IsDispellable = true,
            CreatedBy = "system"
        };
        effect.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.ModifyAttribute,
            TargetAttribute = attribute,
            ModifierValue = modifier,
            ScalesWithIntensity = true
        });
        return effect;
    }

    private static CharacterEffectDefinition CreateAttributeDebuff(
        string name, string attribute, string description, int modifier, int durationSeconds, string color)
    {
        var effect = new CharacterEffectDefinition
        {
            Name = name,
            Description = description,
            EffectType = CharacterEffectType.Debuff,
            DefaultDurationSeconds = durationSeconds,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IconName = "arrow-down",
            EffectColor = color,
            IsDispellable = true,
            CreatedBy = "system"
        };
        effect.Impacts.Add(new CharacterEffectImpact
        {
            ImpactType = CharacterEffectImpactType.ModifyAttribute,
            TargetAttribute = attribute,
            ModifierValue = modifier,
            ScalesWithIntensity = true
        });
        return effect;
    }
}
