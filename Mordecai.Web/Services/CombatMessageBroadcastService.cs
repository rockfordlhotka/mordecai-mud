using Microsoft.Extensions.Hosting;
using Mordecai.Messaging.Messages;
using Mordecai.Messaging.Services;

namespace Mordecai.Web.Services;

/// <summary>
/// Background service that listens for combat messages and propagates combat sounds to adjacent rooms
/// </summary>
public sealed class CombatMessageBroadcastService : BackgroundService
{
    private readonly IGameMessageSubscriberFactory _subscriberFactory;
    private readonly ISoundPropagationService _soundPropagationService;
    private readonly ILogger<CombatMessageBroadcastService> _logger;
    private IGameMessageSubscriber? _subscriber;

    public CombatMessageBroadcastService(
        IGameMessageSubscriberFactory subscriberFactory,
        ISoundPropagationService soundPropagationService,
        ILogger<CombatMessageBroadcastService> logger)
    {
        _subscriberFactory = subscriberFactory ?? throw new ArgumentNullException(nameof(subscriberFactory));
        _soundPropagationService = soundPropagationService ?? throw new ArgumentNullException(nameof(soundPropagationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Subscribe to all combat messages (use Guid.Empty for global subscription)
            _subscriber = _subscriberFactory.CreateSubscriber(Guid.Empty, null, null);
            _subscriber.MessageReceived += OnMessageReceived;

            await _subscriber.StartAsync().ConfigureAwait(false);

            _logger.LogInformation("Combat message broadcast service started");

            // Keep service running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Combat message broadcast service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Combat message broadcast service encountered an error");
            throw;
        }
    }

    private async Task OnMessageReceived(GameMessage message)
    {
        try
        {
            switch (message)
            {
                case CombatStarted combatStarted:
                    await HandleCombatStarted(combatStarted).ConfigureAwait(false);
                    break;

                case CombatAction combatAction:
                    await HandleCombatAction(combatAction).ConfigureAwait(false);
                    break;

                case CombatEnded combatEnded:
                    await HandleCombatEnded(combatEnded).ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling combat message: {MessageType}", message.GetType().Name);
        }
    }

    private async Task HandleCombatStarted(CombatStarted message)
    {
        var description = $"{message.InitiatorName} attacking {message.TargetName}";

        await _soundPropagationService.PropagateSound(
            message.LocationRoomId,
            message.SoundLevel,
            SoundType.Combat,
            description,
            message.InitiatorName,
            $"{message.InitiatorName} engages {message.TargetName} in combat!"
        ).ConfigureAwait(false);

        _logger.LogDebug(
            "Propagated combat start sound from room {RoomId}: {Initiator} vs {Target}",
            message.LocationRoomId,
            message.InitiatorName,
            message.TargetName);
    }

    private async Task HandleCombatAction(CombatAction message)
    {
        // Build description based on whether attack hit or missed
        var descriptor = message.IsHit
            ? GetHitDescriptor(message.Damage, message.SkillUsed)
            : GetMissDescriptor(message.SkillUsed);

        var description = $"{message.AttackerName} {descriptor}";

        await _soundPropagationService.PropagateSound(
            message.LocationRoomId,
            message.SoundLevel,
            SoundType.Combat,
            description,
            message.AttackerName,
            message.ActionDescription
        ).ConfigureAwait(false);

        _logger.LogDebug(
            "Propagated combat action sound from room {RoomId}: {Action} (Sound: {Level})",
            message.LocationRoomId,
            message.ActionDescription,
            message.SoundLevel);
    }

    private async Task HandleCombatEnded(CombatEnded message)
    {
        var description = message.WinnerName != null
            ? $"{message.WinnerName} victorious"
            : "combat ending";

        await _soundPropagationService.PropagateSound(
            message.LocationRoomId,
            SoundLevel.Normal,
            SoundType.Combat,
            description,
            message.WinnerName,
            $"The combat ends. {message.EndReason}"
        ).ConfigureAwait(false);

        _logger.LogDebug(
            "Propagated combat end sound from room {RoomId}: {Reason}",
            message.LocationRoomId,
            message.EndReason);
    }

    private static string GetHitDescriptor(int damage, string skillUsed)
    {
        // Describe the hit based on damage dealt
        return damage switch
        {
            <= 2 => "landing a glancing blow",
            <= 5 => "striking solidly",
            <= 10 => "hitting hard",
            <= 15 => "dealing a powerful strike",
            _ => "delivering a devastating blow"
        };
    }

    private static string GetMissDescriptor(string skillUsed)
    {
        return skillUsed.Contains("Unarmed", StringComparison.OrdinalIgnoreCase)
            ? "swinging wildly"
            : "attacking fiercely";
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_subscriber != null)
            {
                _subscriber.MessageReceived -= OnMessageReceived;
                await _subscriber.StopAsync().ConfigureAwait(false);
                _subscriber.Dispose();
                _subscriber = null;
            }

            _logger.LogInformation("Combat message broadcast service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping combat message broadcast service");
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public override void Dispose()
    {
        _subscriber?.Dispose();
        base.Dispose();
    }
}
