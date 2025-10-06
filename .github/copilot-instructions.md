# GitHub Copilot Project Instructions - Mordecai MUD

This repository builds a skill-based, real-time, text MUD using .NET 9 (Blazor Server + Aspire for dev orchestration) with SQLite (later PostgreSQL), RabbitMQ for async messaging, and OpenTelemetry for observability.

## High-Level Architecture
- Blazor Server web front-end (interactive + server-side rendering)
- Background services for world simulation, NPC processing, scheduled ticks
- Messaging via RabbitMQ (ALL cross-user, NPC, world events, chat)
- EF Core (code-first) with SQLite dev DB (migration path to PostgreSQL)
- Aspire AppHost orchestrates services locally (RabbitMQ container, etc.)
- OpenTelemetry for tracing/logging/metrics

## Documentation

Never create arbitrary documentation; always link to existing docs or RFCs.

- Mordecai Specification (docs/mordecai_specification.md)
- Quick reference (docs/quick_reference.md)
- Database design (docs/database_design.md)
- Kubernetes deployment (docs/kubernetes_deployment.md)

## Current Development Priorities (Early Phases)
1. World foundation: rooms, movement, descriptions, look/examine commands, local/room chat
2. Character system: creation, 7 core skills (Physicality, Dodge, Drive, Reasoning, Awareness, Focus, Bearing), practice-based skill progression
3. Combat foundation: melee, ranged, spell casting (each spell == its own skill), cooldowns, skill checks
4. Admin content tools (builder interface): create/edit rooms, zones, NPCs, items, quests (scaffold after core world + character basics)

If uncertain between adding a new feature vs reinforcing these priorities, prefer reinforcing the earlier-numbered priorities.

## Coding & Design Conventions
- Use C# 12 / .NET 9 features where they improve clarity (file-scoped namespaces, primary constructors cautiously, pattern matching, etc.)
- Prefer vertical slice additions: data model -> EF migration -> domain/service logic -> messaging integration -> UI endpoint/component.
- Keep public API surface minimal & intention-revealing.
- Avoid premature microservices; remain monolithic (modular internal layering) until proven scaling need.
- Favor internal event/message types over leaking EF entities to other layers.
- All game action resolution should be skill-based (NO direct raw attribute comparisons at runtime; attributes only influence/seed skills or apply modifiers).
- Keep domain logic testable (pure methods where feasible, side-effects isolated).
- Nullability: enable and honor nullable reference types.
- Use async/await all the way; avoid `.Result` / `.Wait()`.
- Guard clauses over deep nesting.
- Logging: structured (use ILogger<T>), avoid excessive noise; important state transitions & errors only.

## Entity Framework Guidance
- Code-first migrations; add new migration per schema change with descriptive name.
- Avoid loading broad object graphs unnecessarily (use projection + `AsNoTracking` for read models).
- Keep decimal/float precision explicit for progression values.
- Composite uniqueness: ensure `CharacterSkills` `(CharacterId, SkillDefinitionId)` stays unique.

## Messaging (RabbitMQ) Guidelines
- Define strongly-typed message contracts (POCO records) in `Mordecai.Messaging`.
- Use clear naming: `PlayerMoved`, `RoomDescriptionRequested`, `ChatMessagePosted`, etc.
- Messages should be immutable and serialization-friendly.
- Avoid putting EF entities or large payloads directly on the bus.

## Background Services
- Timer / tick loops should be cooperative, cancellation-token aware, and jittered (avoid lock-step thundering herd).
- World simulation actions emit messages; consumers update state.

## Combat & Skill System Principles
- Every action resolves: `ActionSkill vs DefenseSkill` -> outcome table / probability -> effect events.
- Skills increase by logged usage events (apply diminishing returns algorithm; TBD but keep hooks ready: e.g., `ISkillProgressionService`).
- Spells = distinct skills + entries in `SkillDefinitions` (type: SpellSkill) + optional mapping to `MagicSchools`.

## File / Project Organization (current intent)
- `Mordecai.Game`: Core domain models, services (movement, skills, combat skeleton)
- `Mordecai.Messaging`: Message contracts, possibly thin dispatch helpers
- `Mordecai.BackgroundServices`: Hosted services consuming/producing messages
- `Mordecai.Web`: Blazor Server UI & minimal adapters (includes admin content creation UI)
- `Mordecai.ServiceDefaults`: Cross-cutting extensions (OpenTelemetry, resiliency, etc.)
- `Mordecai.AppHost`: Aspire orchestration project

## When Adding Code (Checklist)
1. Does this change advance one of the active priority phases?
2. Are new domain concepts represented by a clear model + (if persisted) migration?
3. Is there a message/event that should be emitted or handled?
4. Are we avoiding direct EF entity exposure to UI components?
5. Are nullable + async best practices respected?
6. Have we written at least minimal unit tests for pure logic (if test project exists or after test project is added)?

## Security & Validation
- Validate user/game commands server-side (never trust client state).
- Rate-limit or debounce spammy actions (especially chat, movement).
- Sanitize text output (avoid injection into UI / ANSI concerns for future terminal theming).

## Observability
- Add tracing spans around high-level game actions (movement, combat resolution, skill advancement).
- Tag spans with `character.id`, `room.id`, `skill.id` when relevant.

## Performance Notes
- Use in-memory caching for frequently read static data: SkillDefinitions, Room metadata.
- Avoid chat or movement operations causing N+1 queries.
- Plan upgrade path for DB (SQLite -> PostgreSQL) by avoiding engine-specific SQL.

## Future (Do NOT prematurely implement)
- Advanced crafting pipelines
- Player housing systems
- Complex quest chains with branching logic engine
- External API integrations

Keep placeholders or TODO comments minimal; prefer a tracked issue instead.

## Style Preferences Summary
- Immutability where practical
- Records for simple data carriers; classes for behavior
- Extension methods for cross-cutting utility (but avoid overuse)
- Avoid static state (except clearly scoped caches with thread safety)

## Non-Goals (for now)
- WebAssembly client variant
- Full microservice split
- Real-time WebSocket custom protocol (SignalR suffices now)

## Example Message Contract Pattern
```csharp
namespace Mordecai.Messaging.Messages;

public sealed record PlayerMoved(
    Guid CharacterId,
    int FromRoomId,
    int ToRoomId,
    DateTimeOffset OccurredAt
);
```

## Example Skill Progression Hook Interface
```csharp
namespace Mordecai.Game.Skills;

public interface ISkillProgressionService
{
    Task LogUsageAsync(Guid characterId, int skillDefinitionId, SkillUsageType usageType, int baseExperience, CancellationToken ct = default);
}
```

If adding new patterns, ensure they align with these principles before expanding.

---
Last updated: 2025-01-23
