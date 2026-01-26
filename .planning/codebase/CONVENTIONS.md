# Coding Conventions

**Analysis Date:** 2026-01-26

## Naming Patterns

**Files:**
- Classes and interfaces: PascalCase matching class name (e.g., `DiceService.cs`, `IEquipmentService.cs`)
- One class per file (exception: tightly coupled related types)
- Test files: `[ClassName]Tests.cs` (e.g., `CharacterCreationTests.cs`)
- Enum files: Plural or descriptive names (e.g., `WorldEntities.cs` for multiple entity types)

**Classes and Interfaces:**
- PascalCase: `CharacterCreationService`, `IDoorInteractionService`, `DiceService`
- Service classes: Suffix with `Service` (e.g., `EquipmentService`, `RoomService`)
- Service interfaces: Prefix with `I` (e.g., `IDiceService`, `IEquipmentService`)
- Result/Response types: Suffix with `Result` or descriptive name (e.g., `DoorActionResult`, `ItemCreationResult`)
- Enums: PascalCase (e.g., `DoorState`, `DoorLockType`, `ArmorSlot`)

**Methods:**
- PascalCase for all public and private methods
- Async methods: Suffix with `Async` (e.g., `GetRoomAsync`, `CreateItemForCharacterAsync`)
- Boolean methods: Prefix with `Try`, `Can`, `Is`, `Has` (e.g., `CanMoveToRoomAsync`, `HasDoor`)
- Query methods: Prefix with `Get` (e.g., `GetCharacterSkillsAsync`, `GetRoomExitsAsync`)
- Test methods: Pattern `[MethodName]_[Scenario]_[ExpectedResult]` (e.g., `OpenAsync_ShouldOpenUnlockedDoor`)

**Properties:**
- PascalCase: `CurrentHealth`, `MaxVitality`, `IsEquipped`, `CreatedBy`
- Boolean properties: Prefix with `Is` or `Has` (e.g., `IsActive`, `HasDoor`, `IsLocked`)

**Private Fields:**
- Underscore prefix with camelCase: `_diceService`, `_contextFactory`, `_logger`, `_skillService`
- Static readonly dictionaries/lookups: PascalCase (e.g., `CoverageSlotLookup`, `NameSlotHints`)

**Parameters and Local Variables:**
- camelCase: `characterId`, `roomId`, `cancellationToken`, `roomId`, `userId`

**Constants:**
- PascalCase or UPPER_SNAKE_CASE: `DefaultBreakTargetValue`, `MaxOccupancy`
- Game mechanics constants: Usually in entity properties with XML documentation

**Type Suffixes:**
- Message types: `[Concept]Event` or `[Concept]Message` (from Messaging project)
- Result types: `[Action]Result` (e.g., `DoorActionResult`, `EquipResult`)
- Service interfaces: `I[Service]` (e.g., `IEquipmentService`)

## Code Style

**Formatting:**
- Nullable reference types: Enabled project-wide (`<Nullable>enable</Nullable>`)
- Implicit usings: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- No explicit `.editorconfig` detected; follows .NET 9 conventions

**Linting:**
- No explicit linter configuration file detected
- Relies on built-in C# compiler warnings and nullable reference types

**Indentation & Braces:**
- Allman style braces (opening brace on new line) for class/method declarations
- Inline braces for short if/else (observed in pattern matching)
- 4 spaces per indentation level

## Import Organization

**Order:**
1. System namespaces (`using System;`, `using System.Collections;`)
2. Third-party namespaces (`using Microsoft.EntityFrameworkCore;`, `using Xunit;`)
3. Project namespaces (`using Mordecai.Game.Entities;`, `using Mordecai.Web.Services;`)
4. File-scoped namespace declaration: `namespace Mordecai.Web.Services;`

**Path Aliases:**
- Used for disambiguating imported types when multiple projects export same name
- Example: `using WebSkillCategory = Mordecai.Web.Data.SkillCategory;` (see `DoorInteractionServiceTests.cs` line 8-12)

## Error Handling

**Patterns:**
- **Result records for operation outcomes** (not exceptions): Services return typed result records
  - Example: `DoorActionResult` with `Success`, `Message`, and relevant data fields
  - Factory methods: `DoorActionResult.Failure()`, `DoorActionResult.SuccessResult()` (see `DoorInteractionService.cs` lines 26-29)

- **Try/fail pattern**: Callers check `result.Success` and read `result.Message`
  - Example: `if (!doorLookup.Success) { return doorLookup.Result; }` (see `DoorInteractionService.cs` line 61)

- **Validation at operation entry**: Input validation happens first, returns early with failure
  - Example: Null checks, missing entity checks before proceeding (see `EquipmentService.cs` lines 104-118)

- **No exception throwing in business logic**: Services use result records instead
  - Exceptions only for unexpected/programmer errors (e.g., `InvalidOperationException` in test stubs)

- **Graceful degradation**: Operations return failure results rather than throwing
  - Example: "Character not found." rather than exception (see `EquipmentService.cs` line 109)

## Logging

**Framework:** `ILogger<T>` via dependency injection (built-in .NET logging)

**Patterns:**
- Injected as `ILogger<[ServiceName]>` (e.g., `ILogger<DoorInteractionService>`)
- Used primarily for diagnostic/warning information
- Pattern: `_logger.LogWarning("message")` (see `RoomService.cs` line 93)
- Null logger in tests: `NullLogger<T>.Instance` (see `DoorInteractionServiceTests.cs` line 25)

**When to log:**
- Warnings for unimplemented methods or missing data
- Diagnostic information for debugging service behavior
- Not typically used for success paths

## Comments

**When to Comment:**
- Business logic requiring explanation of game mechanics
- Complex calculations (especially those referencing game design docs)
- Non-obvious conditional logic

**XML Documentation:**
- Used on public interfaces and service methods
- Three-slash comments (`///`) for public APIs
- Format: `<summary>`, `<remarks>` for game mechanics context
- Example from `DiceService.cs`:
  ```csharp
  /// <summary>
  /// Rolls 4dF (four Fudge dice) returning a value between -4 and +4
  /// </summary>
  int Roll4dF();
  ```

- Entity properties documented when behavior is non-obvious
- Example from `WorldEntities.cs` lines 42-44:
  ```csharp
  /// <summary>
  /// Difficulty level of the zone (affects spawns, skill requirements, etc.)
  /// </summary>
  public int DifficultyLevel { get; set; } = 1;
  ```

**Inline Comments:**
- Sparse; code should be self-documenting
- Used for non-obvious game mechanics or constraints
- Example from `DiceService.cs` lines 133-134:
  ```csharp
  // Fudge die has 6 faces: 2 blanks (0), 2 plus (+1), 2 minus (-1)
  byte[] randomBytes = new byte[1];
  ```

## Function Design

**Size:** Prefer small, focused methods (most examples 10-50 lines)

**Parameters:**
- Use dependency injection for services (`IDbContextFactory`, `IDiceService`, `ILogger`)
- Pass IDs (Guid, int, string) as parameters
- Optional parameters with defaults where sensible (e.g., `int minValue = 1, int maxValue = 20`)
- `CancellationToken` as final parameter: `CancellationToken cancellationToken = default`

**Return Values:**
- Async operations: `Task<T>` or `ValueTask<T>` for database factory
  - Example: `Task<DoorActionResult>`, `Task<string>`, `ValueTask<ApplicationDbContext>`
- Synchronous operations: Direct types or result records
  - Example: `int Roll4dF()`, `DoorActionResult.Failure(...)`
- Query operations return `IReadOnlyList<T>` (immutable view)
  - Example: `IReadOnlyList<RoomExitInfo>`, `IReadOnlyList<Item>`

## Module Design

**Exports:**
- Service interfaces in same file as implementation (see `DoorInteractionService.cs` lines 8-15)
- Result records defined in same file as service
- Helper records/types colocated (e.g., `RoomExitInfo` record in `RoomService.cs`)

**Barrel Files:**
- Not used; explicit imports preferred

**Class Organization Within File:**
1. Interface definition (if applicable)
2. Result/Response records (if applicable)
3. Main class/service implementation
4. Private helper classes/records (sealed inner classes for test doubles, e.g., `StubDiceService`)

**Data Access Patterns:**
- Injected: `IDbContextFactory<ApplicationDbContext>` (allows creating fresh context per operation)
- Usage pattern: `await using var context = await _contextFactory.CreateDbContextAsync(ct);`
- Prevents context reuse issues in async scenarios

## Records vs Classes

**Records used for:**
- Result/response types: `sealed record DoorActionResult(...)`
- DTOs and data transfer objects: `sealed record RoomExitInfo(...)`
- Immutable data containers with positional parameters

**Classes used for:**
- Service implementations
- Entity models
- Enums
- Complex state management

**Sealed modifier:**
- Applied to record result types to prevent inheritance: `sealed record DoorActionResult`
- Applied to test double classes: `sealed class StubDiceService`

## Type Safety Patterns

**Nullable Reference Types:**
- Project-wide enabled; actively used for nullability enforcement
- Properties explicitly nullable: `string? ExitMessage { get; set; }` (see `WorldEntities.cs` line 131)
- Properties non-nullable default: `public string Name { get; set; }` means required

**Pattern Matching:**
- Switch expressions preferred: `return roll switch { 0 or 1 => 0, 2 or 3 => 1, ... }` (see `DiceService.cs` lines 139-145)
- Used for enum conversions and value transformations

**Async/Await:**
- All I/O operations are async
- Database queries: Always `.Async` variants (`.FirstOrDefaultAsync()`, `.SaveChangesAsync()`)
- Consistent `CancellationToken` threading
- Test operations also async: `async Task TestMethodAsync()`

## Data Annotations

**On Entity Properties:**
- `[Key]` for primary keys: `public int Id { get; set; }`
- `[Required]` for non-nullable business data
- `[StringLength(n)]` for string bounds: `[StringLength(100)] public string Name`
- `[ForeignKey("PropertyName")]` for EF Core relationships
- Data annotations as primary validation mechanism

**EF Core Conventions:**
- Navigation properties: `public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();`
- Relationships inferred from navigation naming
- Timestamps: `public DateTimeOffset CreatedAt`, `public DateTimeOffset? LastModifiedAt`

---

*Convention analysis: 2026-01-26*
