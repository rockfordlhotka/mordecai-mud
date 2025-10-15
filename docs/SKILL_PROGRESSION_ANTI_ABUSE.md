# Skill Progression Anti-Abuse Mechanics

## Overview

Mordecai MUD uses a usage-based skill progression system where skills improve through actual practice. While this creates engaging gameplay, it also creates potential for automation abuse. This document outlines strategies to prevent skill grinding and botting without burdening legitimate players.

## Core Design Principles

1. **Soft limits over hard caps** - Use diminishing returns rather than absolute cutoffs
2. **Transparent feedback** - Players should understand why progression is slowing
3. **Natural gameplay incentives** - Design systems that make legitimate play more rewarding than grinding
4. **Progressive escalation** - Detection and penalties should escalate with repeated abuse

## Anti-Abuse Strategies

### 1. Diminishing Returns Per Time Window

Implement soft diminishing returns based on hourly usage to discourage marathon grinding sessions while not punishing active legitimate play.

**Hourly Usage Thresholds:**

- **First 50 uses per hour**: Full value (1.0x multiplier)
- **Next 50 uses (51-100)**: Reduced value (0.5x multiplier)
- **Next 50 uses (101-150)**: Minimal value (0.1x multiplier)
- **Beyond 150 uses/hour**: No progression value (0.0x multiplier)

**Implementation Notes:**

- Track usage counts per skill per character with rolling 60-minute windows
- Reset counters hourly (not at fixed times, but 60 minutes from first use)
- Store usage timestamps to calculate rolling windows
- Clear feedback messages when entering reduced multiplier zones

**Player Feedback Messages:**

```text
"Your sword practice is becoming less effective. Consider resting or varying your training."
"You've been training intensively. Skill gains are significantly reduced."
"Your body and mind need rest. Skill progression temporarily suspended until you take a break."
```

**Benefits:**

- No hard wall that stops legitimate active play
- Natural discouragement of automated grinding
- Resets naturally with time, encouraging breaks
- Easy to tune based on observed player behavior

### 2. Context-Aware Usage Validation

Only count skill usage that occurs in meaningful gameplay contexts. Invalid or suspicious contexts receive reduced or zero progression.

**Invalid Contexts (0.0x multiplier):**

- Attacking the same target repeatedly with no variation (same NPC, same location, >20 times)
- Using skills in designated "safe rooms" or practice areas beyond initial training threshold
- Repetitive actions with identical timing patterns (±100ms variance over 20+ repetitions)
- Skill use while not engaged in actual content (attacking air, crafting nothing)
- Combat against "training dummy" NPCs beyond skill level 3

**Valid Contexts (normal multipliers):**

- Combat against different opponents (varied NPC types/levels)
- Crafting different items or using different material combinations
- Social skills used with different player targets
- Skills used in varied locations and situations
- Quest-related skill usage

**Reduced Context (0.3x multiplier):**

- Repeatedly crafting identical items (after first 10 of same recipe per day)
- Combat in obvious grinding locations (after extended duration)
- Skill usage during "idle" periods with no other player activity

**Implementation Notes:**

- Track recent targets, locations, and action patterns per character
- Maintain a "context variety" score that affects multipliers
- Use heuristics to identify mechanical vs organic gameplay patterns

### 3. Skill Use Cooldown Per Target/Context

Implement internal cooldowns on counting usage against the same target or in the same context to prevent rapid-fire spam grinding.

**Cooldown Rules:**

- **Same combat opponent**: Only count 1 weapon skill usage per 30 seconds against the same NPC
- **Same spell target**: Only count 1 spell usage per 20 seconds on same target
- **Same crafting recipe**: Only count 1 crafting usage per 60 seconds for identical items
- **Same social target**: Only count 1 social skill usage per 120 seconds per character

**Implementation Notes:**

- Track last-counted skill use timestamp per (skill, target) pair
- Cooldowns apply only to progression counting, not actual skill usage
- Players can still use skills freely, but progression only counts after cooldown
- Cooldowns reset when changing targets/contexts

**Player Feedback (optional):**

```text
"You've already learned from recent practice against this opponent."
"Repeated crafting of the same item yields diminishing learning value."
```

### 4. Fatigue System as Natural Limiter

Leverage the existing Fatigue (FAT) system as a built-in anti-grinding mechanic that naturally limits sustained skill usage.

**Current FAT Mechanics:**

- Every combat action costs 1 FAT (2 FAT for dual wielding)
- Characters recover 1 FAT per 3 seconds naturally
- Low FAT reduces combat effectiveness
- FAT = 0 prevents combat actions

**Enhancement for Anti-Abuse:**

- **Failed actions** (missed attacks, failed skill checks) don't count toward skill progression OR count at significantly reduced rate (0.2x multiplier)
- **Low FAT penalties** (< 25% max FAT): Skill progression reduced to 0.5x multiplier
- **Exhaustion state** (FAT = 0): No skill progression possible until FAT recovers above 25%

**Rationale:**

- Creates natural breaks in skill usage
- Prevents low-skill characters from grinding against impossible targets
- Encourages strategic FAT management
- Already implemented, just needs progression hooks

### 5. Pattern Detection and Flagging

Implement server-side detection for suspicious automation patterns with escalating responses.

**Suspicious Behavior Indicators:**

- Identical action timing (±100ms variance) over 20+ consecutive repetitions
- Command sequences that repeat in exact patterns
- Playing 18+ hours without substantial breaks (< 5 minutes per 3 hours)
- Multiple characters from same account grinding simultaneously in different sessions
- Skill usage during extended "idle" periods (no movement, chat, or varied commands)
- Inhuman response times to events (< 50ms reaction times consistently)

**Detection Scoring System:**

Each suspicious indicator contributes to an abuse score:

- **Score 0-10**: Normal play (1.0x multiplier)
- **Score 11-20**: Possible automation (0.5x multiplier + warning message)
- **Score 21-30**: Likely automation (0.1x multiplier + strong warning)
- **Score 31+**: Confirmed pattern (0.0x multiplier + admin notification + temporary skill progression freeze)

**Escalating Response:**

1. **First detection** (score 11-20):
   - Reduce skill gain to 0.5x
   - Warning message: "Your activity pattern appears unusual. Please ensure you're playing manually."
   - Log event for admin review

2. **Continued pattern** (score 21-30):
   - Reduce skill gain to 0.1x
   - Stronger warning: "Automated play is prohibited. Your skill progression is being heavily penalized."
   - Email/notification to player account
   - Escalated admin notification

3. **Persistent pattern** (score 31+):
   - Temporary skill progression freeze (24 hours)
   - Account flagged for admin investigation
   - Require CAPTCHA or manual verification to resume progression
   - Possible account suspension for repeat offenders

**Implementation Notes:**

- Background service tracks and scores behavior patterns
- Scores decay over time with normal play behavior
- False positive handling: Players can appeal with admin intervention
- Admin dashboard to review flagged accounts and patterns

### 6. Variable Skill Gain Based on Challenge Rating

Adjust skill progression multipliers based on the difficulty of the content relative to the character's current skill level.

**Challenge Rating Calculation:**

Compare opponent level, crafting difficulty, or skill check TV against character's current skill level:

```text
Challenge Differential = Target Difficulty - Character Skill Level
```

**Multiplier Based on Challenge:**

| Differential | Description | Multiplier | Rationale |
|--------------|-------------|------------|-----------|
| -10 or lower | Trivial | 0.1x | Too easy, minimal learning |
| -9 to -5 | Easy | 0.5x | Below skill level, some learning |
| -4 to +4 | Appropriate | 1.0x | Ideal challenge range |
| +5 to +9 | Difficult | 1.5x | Above skill level, maximum learning |
| +10 or higher | Overwhelming | 0.5x | Too hard, flailing ineffectively |

**Implementation Notes:**

- Calculate challenge rating for each skill usage event
- Store base difficulty ratings for NPCs, crafting recipes, skill checks
- Dynamic difficulty for player-vs-player interactions
- Clear feedback when fighting trivial/overwhelming opponents

**Player Feedback:**

```text
"This opponent is too weak to teach you much."
"Fighting this challenging opponent is excellent training!"
"This foe is beyond your current skill level. You're learning slowly through adversity."
```

**Benefits:**

- Naturally discourages grinding low-level content
- Encourages seeking appropriately challenging encounters
- Rewards players for taking on difficult content
- Self-regulating system that adapts to player progression

### 7. Daily "Fresh Learning" Bonus

Invert the typical daily cap approach by rewarding early daily usage with bonus multipliers, encouraging regular play sessions over marathon grinding.

**Daily Usage Thresholds:**

- **First 100 uses per day**: 1.5x multiplier ("Fresh Mind Bonus")
- **Next 100 uses (101-200)**: 1.0x multiplier (normal progression)
- **Beyond 200 uses per day**: 0.5x multiplier ("Mental Fatigue")

**Daily Reset:**

- Resets at server midnight (configurable timezone)
- Counter is per-skill, not global
- Encourages spreading practice across multiple skills

**Alternative: Weekly Fresh Learning**

For less frequent players:

- **First 500 uses per week**: 1.5x multiplier
- **Next 500 uses (501-1000)**: 1.0x multiplier
- **Beyond 1000 uses per week**: 0.5x multiplier

**Player Feedback:**

```text
"Your mind is fresh! Skill training is highly effective today."
"You're making steady progress with your training."
"You've trained extensively today. Mental fatigue is reducing learning effectiveness."
```

**Benefits:**

- Rewards consistent daily/weekly engagement
- Discourages binge grinding sessions
- Encourages balanced play across multiple days
- No hard cap prevents progression entirely

## Recommended Implementation Phases

### Phase 1: Core Protection (MVP)

Implement the most effective anti-abuse measures with minimal complexity:

1. **Diminishing Returns Per Hour** (Strategy #1)
   - Easiest to implement
   - Immediate impact on grinding behavior
   - Database: Add usage counter and timestamp per skill

2. **Target/Context Cooldowns** (Strategy #3)
   - Prevents rapid-fire spam attacks
   - Minimal storage overhead
   - Database: Track last-counted timestamp per (skill, target)

3. **FAT System Integration** (Strategy #4)
   - Leverages existing mechanics
   - No new systems needed
   - Code: Add progression hooks to FAT checks

**Estimated Implementation Time:** 1-2 weeks

### Phase 2: Enhanced Detection (Post-Launch)

Add more sophisticated detection and player incentives:

4. **Challenge-Based Multipliers** (Strategy #6)
   - Add difficulty ratings to NPCs and content
   - Calculate challenge differential
   - Reward appropriately challenging content

5. **Daily Fresh Learning Bonus** (Strategy #7)
   - Simple counter with daily reset
   - Positive reinforcement for regular play
   - Encourages consistent engagement

6. **Basic Pattern Detection** (Strategy #5 - simplified)
   - Track basic timing patterns
   - Simple abuse scoring (0-30 scale)
   - Warning messages only (no automation)

**Estimated Implementation Time:** 2-3 weeks

### Phase 3: Advanced Systems (Future Enhancement)

Implement sophisticated detection and admin tools:

7. **Context-Aware Validation** (Strategy #2)
   - Complex heuristics for pattern recognition
   - Context variety scoring
   - Location and target diversity tracking

8. **Advanced Bot Detection** (Strategy #5 - full)
   - Machine learning pattern detection
   - Automated flagging and escalation
   - Admin dashboard and investigation tools
   - CAPTCHA integration for flagged accounts

**Estimated Implementation Time:** 4-6 weeks

## Player Communication Strategy

**Transparency is Key:**

Players should understand why their progression is slowing and what they can do about it. Clear, helpful messages prevent frustration.

**In-Game Messages (Examples):**

```text
"Your sword practice is becoming less effective. Consider resting or finding stronger opponents."

"You've been training intensively. Skill gains are reduced until tomorrow."

"Fighting opponents near your skill level yields the best training results."

"Your mind and body need rest. Take a break and come back refreshed for optimal learning."

"This opponent is too weak to provide meaningful practice at your skill level."
```

**Help System Integration:**

Add `/help progression` command explaining:

- How skill progression works
- Why diminishing returns exist
- Tips for optimal skill development
- What behaviors are considered abuse

**Website/Forum Documentation:**

- Clear anti-cheat policy
- Explanation of progression systems
- FAQ addressing common concerns
- Appeals process for false positives

## Configuration Settings

**Admin-Tunable Parameters:**

```csharp
// Hourly diminishing returns
public int HourlyUsageThreshold1 { get; set; } = 50;  // First threshold
public decimal HourlyMultiplier1 { get; set; } = 1.0m;
public int HourlyUsageThreshold2 { get; set; } = 100; // Second threshold
public decimal HourlyMultiplier2 { get; set; } = 0.5m;
public int HourlyUsageThreshold3 { get; set; } = 150; // Third threshold
public decimal HourlyMultiplier3 { get; set; } = 0.1m;

// Daily fresh learning
public int DailyFreshUsageThreshold { get; set; } = 100;
public decimal DailyFreshMultiplier { get; set; } = 1.5m;
public int DailyFatigueThreshold { get; set; } = 200;
public decimal DailyFatigueMultiplier { get; set; } = 0.5m;

// Challenge rating
public decimal ChallengeTrivialMultiplier { get; set; } = 0.1m;  // -10 or lower
public decimal ChallengeEasyMultiplier { get; set; } = 0.5m;     // -9 to -5
public decimal ChallengeAppropriateMultiplier { get; set; } = 1.0m; // -4 to +4
public decimal ChallengeDifficultMultiplier { get; set; } = 1.5m;   // +5 to +9
public decimal ChallengeOverwhelmingMultiplier { get; set; } = 0.5m; // +10 or higher

// Target cooldowns (seconds)
public int CombatTargetCooldown { get; set; } = 30;
public int SpellTargetCooldown { get; set; } = 20;
public int CraftingRecipeCooldown { get; set; } = 60;
public int SocialTargetCooldown { get; set; } = 120;

// Pattern detection thresholds
public int PatternDetectionWarningScore { get; set; } = 11;
public int PatternDetectionPenaltyScore { get; set; } = 21;
public int PatternDetectionFreezeScore { get; set; } = 31;
```

## Database Schema Additions

### Skill Usage Tracking

```sql
-- Hourly usage tracking
CREATE TABLE SkillUsageHourlyTracking (
    CharacterId UNIQUEIDENTIFIER,
    SkillDefinitionId INT,
    WindowStartTime DATETIME2,
    UsageCount INT,
    PRIMARY KEY (CharacterId, SkillDefinitionId, WindowStartTime)
);

-- Daily usage tracking
CREATE TABLE SkillUsageDailyTracking (
    CharacterId UNIQUEIDENTIFIER,
    SkillDefinitionId INT,
    Date DATE,
    UsageCount INT,
    PRIMARY KEY (CharacterId, SkillDefinitionId, Date)
);

-- Target-specific cooldowns
CREATE TABLE SkillUsageTargetCooldowns (
    CharacterId UNIQUEIDENTIFIER,
    SkillDefinitionId INT,
    TargetId NVARCHAR(100), -- Could be NPC ID, recipe ID, player ID, etc.
    LastCountedAt DATETIME2,
    PRIMARY KEY (CharacterId, SkillDefinitionId, TargetId)
);

-- Pattern detection scoring
CREATE TABLE PlayerBehaviorScores (
    CharacterId UNIQUEIDENTIFIER PRIMARY KEY,
    AbuseScore INT,
    LastUpdated DATETIME2,
    LastWarningAt DATETIME2,
    ProgressionFrozenUntil DATETIME2
);
```

## Monitoring and Analytics

**Key Metrics to Track:**

1. **Progression Rate Distribution**
   - Average skill uses per hour across all players
   - Identify outliers (>3 standard deviations)
   - Track progression rates by skill type

2. **Multiplier Impact**
   - Percentage of skill uses at each multiplier level
   - Track how often players hit diminishing returns
   - Identify if thresholds are too strict or too lenient

3. **Pattern Detection Events**
   - Number of warnings issued per day
   - False positive rate (appeals upheld)
   - Accounts flagged for investigation

4. **Player Retention**
   - Do anti-abuse measures correlate with player churn?
   - Compare retention between normal and flagged players
   - Identify if legitimate players feel punished

**Admin Dashboard Views:**

- Real-time suspicious activity feed
- Player progression rate charts
- Flagged account review queue
- System tuning parameter adjustments
- Appeal tracking and resolution

## Appeals and False Positive Handling

**Player Appeal Process:**

1. Player submits appeal through in-game command or website
2. Appeal includes automatic data summary (recent activity, multipliers applied)
3. Admin reviews activity logs and pattern scores
4. Admin can:
   - Reset abuse score
   - Remove progression freeze
   - Adjust individual player thresholds
   - Issue warning or ban for confirmed abuse

**False Positive Mitigation:**

- Start with lenient thresholds and tune based on data
- Never permanently ban without human admin review
- Clear escalation path: warning → penalty → freeze → investigation
- Allow players to continue playing (just with reduced progression)
- Provide detailed activity logs to admins for review

## Testing Strategy

**Pre-Launch Testing:**

1. **Automated Bot Testing**
   - Create test bots with varying automation patterns
   - Verify detection triggers appropriately
   - Ensure legitimate fast play isn't flagged

2. **Edge Case Testing**
   - Very active legitimate players (8+ hours/day)
   - Players who focus on single skill intensively
   - Players with irregular playtimes (shift workers)

3. **Performance Testing**
   - Ensure tracking systems don't impact server performance
   - Database query optimization for hourly/daily lookups
   - Caching strategies for frequently accessed data

**Post-Launch Monitoring:**

- Weekly review of flagged accounts
- Monthly analysis of progression metrics
- Quarterly tuning of threshold parameters
- Community feedback collection on progression feel

## Future Enhancements

**Potential Advanced Features:**

1. **Machine Learning Bot Detection**
   - Train ML model on confirmed bot vs human patterns
   - Predictive flagging before abuse escalates
   - Adaptive thresholds based on player population

2. **Social Validation**
   - Guilds can vouch for members to reduce false positive risk
   - Player reputation affects abuse score decay rate
   - Community reporting of suspected bots

3. **Behavioral Biometrics**
   - Mouse movement patterns (for web client)
   - Typing cadence analysis
   - Command timing distribution analysis

4. **Dynamic Content Scaling**
   - NPCs/quests that adapt to detect grinding patterns
   - Diminishing loot/experience in heavily farmed areas
   - Bonus rewards for exploring new content

## Conclusion

The goal is to create a skill progression system that rewards legitimate play while making automation unprofitable and easily detectable. By combining multiple complementary strategies, we create a robust defense against abuse without frustrating honest players.

**Core Principles Achieved:**

✅ Soft limits over hard caps (diminishing returns, not walls)
✅ Transparent feedback (players understand why progression slows)
✅ Natural gameplay incentives (challenging content = better progression)
✅ Progressive escalation (warnings before penalties)

**Success Metrics:**

- <5% of players hitting hourly diminishing returns regularly
- <1% of accounts flagged for suspicious patterns
- Player retention remains high (anti-abuse doesn't drive away legitimate players)
- Minimal admin overhead for false positive appeals
- Clear reduction in obvious grinding behavior

This system protects game integrity while maintaining the engaging, practice-based progression that makes Mordecai MUD unique.
