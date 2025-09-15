using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mordecai.Game.Services;

/// <summary>
/// Represents different times of day for room descriptions and world events
/// </summary>
public enum TimeOfDay
{
    Night,      // 00:00 - 05:59
    Dawn,       // 06:00 - 07:59
    Morning,    // 08:00 - 11:59
    Midday,     // 12:00 - 13:59
    Afternoon,  // 14:00 - 17:59
    Evening,    // 18:00 - 21:59
    Dusk        // 22:00 - 23:59
}

/// <summary>
/// Service for managing game time and day/night cycles
/// </summary>
public interface IGameTimeService
{
    /// <summary>
    /// Gets the current game time (can be accelerated compared to real time)
    /// </summary>
    DateTime CurrentGameTime { get; }
    
    /// <summary>
    /// Gets the current time of day period
    /// </summary>
    TimeOfDay CurrentTimeOfDay { get; }
    
    /// <summary>
    /// Gets the next time of day that will occur
    /// </summary>
    TimeOfDay NextTimeOfDay { get; }
    
    /// <summary>
    /// Gets the time remaining until the next time of day period
    /// </summary>
    TimeSpan TimeUntilNext { get; }
    
    /// <summary>
    /// Determines if the current time is considered "outdoors daylight" for room descriptions
    /// </summary>
    bool IsDaylight { get; }
    
    /// <summary>
    /// Gets a descriptive string for the current time period
    /// </summary>
    string GetTimeDescription();
    
    /// <summary>
    /// Event fired when the time of day changes (useful for zone events, lighting changes, etc.)
    /// </summary>
    event Action<TimeOfDay, TimeOfDay> TimeOfDayChanged;
}

/// <summary>
/// Default implementation of game time service
/// </summary>
public sealed class GameTimeService : IGameTimeService, IDisposable
{
    private readonly ILogger<GameTimeService> _logger;
    private readonly Timer _timeUpdateTimer;
    private readonly object _lock = new object();
    
    // Game time acceleration factor (1.0 = real time, 2.0 = 2x faster, etc.)
    private readonly double _timeAcceleration;
    private readonly DateTime _gameStartTime;
    private readonly DateTime _realStartTime;
    
    private TimeOfDay _currentTimeOfDay;
    
    public GameTimeService(ILogger<GameTimeService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // Get time acceleration from configuration (default to 6x for faster day/night cycles)
        _timeAcceleration = configuration.GetValue<double>("Game:TimeAcceleration", 6.0);
        
        _realStartTime = DateTime.UtcNow;
        _gameStartTime = configuration.GetValue<DateTime>("Game:StartTime", 
            new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc)); // Default: 8 AM game time
        
        _currentTimeOfDay = GetTimeOfDayFromDateTime(CurrentGameTime);
        
        // Update time every 30 seconds (real time)
        _timeUpdateTimer = new Timer(CheckTimeOfDayChange, null, 
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        _logger.LogInformation("Game time service started. Acceleration: {Acceleration}x, Current time: {Time} ({TimeOfDay})", 
            _timeAcceleration, CurrentGameTime, _currentTimeOfDay);
    }

    public DateTime CurrentGameTime
    {
        get
        {
            lock (_lock)
            {
                var realElapsed = DateTime.UtcNow - _realStartTime;
                var gameElapsed = TimeSpan.FromTicks((long)(realElapsed.Ticks * _timeAcceleration));
                return _gameStartTime.Add(gameElapsed);
            }
        }
    }

    public TimeOfDay CurrentTimeOfDay
    {
        get
        {
            lock (_lock)
            {
                return _currentTimeOfDay;
            }
        }
    }

    public TimeOfDay NextTimeOfDay => GetNextTimeOfDay(_currentTimeOfDay);

    public TimeSpan TimeUntilNext
    {
        get
        {
            var current = CurrentGameTime;
            var currentHour = current.Hour;
            var targetHour = GetNextTimeOfDay(_currentTimeOfDay) switch
            {
                TimeOfDay.Dawn => 6,
                TimeOfDay.Morning => 8,
                TimeOfDay.Midday => 12,
                TimeOfDay.Afternoon => 14,
                TimeOfDay.Evening => 18,
                TimeOfDay.Dusk => 22,
                TimeOfDay.Night => 0,
                _ => 0
            };
            
            var hoursUntil = targetHour > currentHour ? targetHour - currentHour : (24 - currentHour) + targetHour;
            var minutesUntil = 60 - current.Minute;
            
            var totalMinutes = (hoursUntil * 60) + minutesUntil - 60; // Subtract current minutes
            if (totalMinutes <= 0) totalMinutes += 24 * 60; // Next day
            
            // Convert game time to real time
            var realMinutes = totalMinutes / _timeAcceleration;
            return TimeSpan.FromMinutes(realMinutes);
        }
    }

    public bool IsDaylight => _currentTimeOfDay is TimeOfDay.Dawn or TimeOfDay.Morning or TimeOfDay.Midday or TimeOfDay.Afternoon;

    public string GetTimeDescription()
    {
        var time = CurrentGameTime;
        return _currentTimeOfDay switch
        {
            TimeOfDay.Night => $"the dead of night ({time:HH:mm})",
            TimeOfDay.Dawn => $"the early dawn ({time:HH:mm})",
            TimeOfDay.Morning => $"mid-morning ({time:HH:mm})",
            TimeOfDay.Midday => $"the heat of midday ({time:HH:mm})",
            TimeOfDay.Afternoon => $"the late afternoon ({time:HH:mm})",
            TimeOfDay.Evening => $"the evening ({time:HH:mm})",
            TimeOfDay.Dusk => $"the gathering dusk ({time:HH:mm})",
            _ => $"an unknown time ({time:HH:mm})"
        };
    }

    public event Action<TimeOfDay, TimeOfDay>? TimeOfDayChanged;

    private static TimeOfDay GetTimeOfDayFromDateTime(DateTime dateTime)
    {
        return dateTime.Hour switch
        {
            >= 0 and < 6 => TimeOfDay.Night,
            >= 6 and < 8 => TimeOfDay.Dawn,
            >= 8 and < 12 => TimeOfDay.Morning,
            >= 12 and < 14 => TimeOfDay.Midday,
            >= 14 and < 18 => TimeOfDay.Afternoon,
            >= 18 and < 22 => TimeOfDay.Evening,
            >= 22 and < 24 => TimeOfDay.Dusk,
            _ => TimeOfDay.Night
        };
    }

    private static TimeOfDay GetNextTimeOfDay(TimeOfDay current)
    {
        return current switch
        {
            TimeOfDay.Night => TimeOfDay.Dawn,
            TimeOfDay.Dawn => TimeOfDay.Morning,
            TimeOfDay.Morning => TimeOfDay.Midday,
            TimeOfDay.Midday => TimeOfDay.Afternoon,
            TimeOfDay.Afternoon => TimeOfDay.Evening,
            TimeOfDay.Evening => TimeOfDay.Dusk,
            TimeOfDay.Dusk => TimeOfDay.Night,
            _ => TimeOfDay.Dawn
        };
    }

    private void CheckTimeOfDayChange(object? state)
    {
        try
        {
            var newTimeOfDay = GetTimeOfDayFromDateTime(CurrentGameTime);
            
            lock (_lock)
            {
                if (newTimeOfDay != _currentTimeOfDay)
                {
                    var oldTimeOfDay = _currentTimeOfDay;
                    _currentTimeOfDay = newTimeOfDay;
                    
                    _logger.LogInformation("Time of day changed from {OldTime} to {NewTime} at {GameTime}", 
                        oldTimeOfDay, newTimeOfDay, CurrentGameTime);
                    
                    TimeOfDayChanged?.Invoke(oldTimeOfDay, newTimeOfDay);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking time of day change");
        }
    }

    public void Dispose()
    {
        _timeUpdateTimer?.Dispose();
    }
}