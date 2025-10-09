using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Background worker that periodically applies pending damage and healing to character health pools.
/// </summary>
public class HealthTickBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HealthTickBackgroundService> _logger;
    private const int ProcessingIntervalSeconds = 3;
    private const int FatigueRegenPerTick = 1;

    public HealthTickBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<HealthTickBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health tick background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingHealthAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying pending health changes");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(ProcessingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Health tick background service stopped.");
    }

    private async Task ProcessPendingHealthAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var characters = await context.Characters
            .Where(c => c.PendingFatigueDamage != 0
                        || c.PendingVitalityDamage != 0
                        || c.CurrentFatigue < ((c.Drive * 2) - 5 > 1 ? (c.Drive * 2) - 5 : 1))
            .ToListAsync(cancellationToken);

        if (characters.Count == 0)
        {
            return;
        }

        var updatedCount = 0;

        foreach (var character in characters)
        {
            var maxFatigue = Math.Max(1, (character.Drive * 2) - 5);

            if (character.CurrentFatigue < maxFatigue)
            {
                character.PendingFatigueDamage = SafeAdd(character.PendingFatigueDamage, -FatigueRegenPerTick);
                updatedCount++;
            }

            if (ApplyPendingPools(character))
            {
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Applied health tick updates for {CharacterCount} characters", updatedCount);
        }
    }

    private static bool ApplyPendingPools(Character character)
    {
        var updated = false;
        updated |= ProcessFatiguePool(character);
        updated |= ProcessVitalityPool(character);
        return updated;
    }

    private static bool ProcessFatiguePool(Character character)
    {
        if (character.PendingFatigueDamage == 0)
        {
            return false;
        }

        var pending = character.PendingFatigueDamage;
        var amount = Math.Max(1, (int)Math.Ceiling(Math.Abs(pending) / 2.0));

        if (pending > 0)
        {
            var applied = Math.Min(amount, character.CurrentFatigue);
            character.CurrentFatigue = Math.Max(0, character.CurrentFatigue - applied);
            character.PendingFatigueDamage = Math.Max(0, character.PendingFatigueDamage - amount);

            var overflow = amount - applied;
            if (overflow > 0)
            {
                character.PendingVitalityDamage += overflow;
            }

            return applied > 0 || overflow > 0;
        }
        else
        {
            var capacity = character.MaxFatigue - character.CurrentFatigue;
            if (capacity <= 0)
            {
                character.PendingVitalityDamage += character.PendingFatigueDamage;
                character.PendingFatigueDamage = 0;
                return true;
            }

            var applied = Math.Min(amount, capacity);
            character.CurrentFatigue = Math.Min(character.MaxFatigue, character.CurrentFatigue + applied);
            character.PendingFatigueDamage = Math.Min(0, character.PendingFatigueDamage + amount);

            var overflow = amount - applied;
            if (overflow > 0)
            {
                character.PendingVitalityDamage -= overflow;
            }

            return applied > 0 || overflow > 0;
        }
    }

    private static bool ProcessVitalityPool(Character character)
    {
        if (character.PendingVitalityDamage == 0)
        {
            return false;
        }

        var pending = character.PendingVitalityDamage;
        var amount = Math.Max(1, (int)Math.Ceiling(Math.Abs(pending) / 2.0));

        if (pending > 0)
        {
            var applied = Math.Min(amount, character.CurrentVitality);
            character.CurrentVitality = Math.Max(0, character.CurrentVitality - applied);
            character.PendingVitalityDamage = Math.Max(0, character.PendingVitalityDamage - amount);
            return applied > 0;
        }
        else
        {
            var capacity = character.MaxVitality - character.CurrentVitality;
            if (capacity <= 0)
            {
                character.PendingVitalityDamage = 0;
                return false;
            }

            var applied = Math.Min(amount, capacity);
            character.CurrentVitality = Math.Min(character.MaxVitality, character.CurrentVitality + applied);
            character.PendingVitalityDamage = Math.Min(0, character.PendingVitalityDamage + amount);
            return applied > 0;
        }
    }

    private static int SafeAdd(int current, int delta)
    {
        try
        {
            return checked(current + delta);
        }
        catch (OverflowException)
        {
            return delta > 0 ? int.MaxValue : int.MinValue;
        }
    }
}
