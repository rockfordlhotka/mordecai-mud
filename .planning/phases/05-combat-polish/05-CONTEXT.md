# Phase 05: Combat Polish - Context

## Requirements

From REQUIREMENTS.md:
- **MECH-01**: Failed attacks with SV < -3 apply timed AV penalties to attacker per damage table
- **MULT-01**: CombatSession tracks all participants (players and NPCs) in same combat instance

## Analysis

### MECH-01: Timed AV Penalties - ALREADY IMPLEMENTED ✓

Found in `CombatService.ApplyPhysicalityPenaltyAsync()` (lines 788-835):

```csharp
// Determine penalty and duration from RVS table
var (penalty, rounds) = resultValue switch
{
    <= -9 => (-3, 3),  // -3 AV for 3 rounds
    <= -7 => (-2, 2),  // -2 AV for 2 rounds
    <= -5 => (-2, 1),  // -2 AV for 1 round
    <= -3 => (-1, 1),  // -1 AV for 1 round
    _ => (0, 0)
};
```

Called from:
- `PerformMeleeAttackAsync()` at line 236 when attack fails (SV ≤ -3)
- `PerformMeleeAttackAsync()` at line 270 when physicality check fails badly

Penalties stored in `CombatParticipant.TimedPenaltiesJson` and cleaned up each tick by `ProcessTimedPenaltiesAsync()`.

### MULT-01: Multi-Combatant Tracking - ALREADY IMPLEMENTED ✓

Found in `CombatEntities.cs`:
```csharp
public class CombatSession
{
    public virtual ICollection<CombatParticipant> Participants { get; set; } = new List<CombatParticipant>();
}
```

Join logic in `CombatService.InitiateCombatAsync()` (lines 49-82):
- When attacking a target already in combat, attacker joins that session
- New CombatParticipant created with same CombatSessionId

Tested by:
- `InitiateCombat_WhenTargetAlreadyInCombat_JoinsExistingSession`

## Conclusion

Both Phase 5 requirements were already implemented during earlier phases:
- MECH-01 was implemented as part of the combat resolution flow
- MULT-01 was implemented during Phase 2 (Combat Orchestration)

Phase 5 is effectively complete - just needs documentation update.
