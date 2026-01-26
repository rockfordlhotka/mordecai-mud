# Testing Patterns

**Analysis Date:** 2026-01-26

## Test Framework

**Runner:**
- xUnit v3.0.0 (Microsoft.NET.Test.Sdk 17.14.1)
- Project: `Mordecai.Web.Tests`
- Framework: .NET 9.0

**Assertion Library:**
- xUnit assertions (built-in: `Assert.True()`, `Assert.False()`, `Assert.Equal()`, `Assert.InRange()`, `Assert.Contains()`, etc.)

**Run Commands:**
```bash
dotnet test                                  # Run all tests
dotnet test Mordecai.Web.Tests              # Run test project
dotnet test /p:CollectCoverage=true        # Run with coverage (requires coverlet)
```

**Test Platform:**
- Configured for xUnit v3 (not yet migrated to Microsoft.Testing.Platform per comments in `.csproj`)

## Test File Organization

**Location:**
- Dedicated test project: `Mordecai.Web.Tests/`
- Separate from source projects (not co-located)

**Naming:**
- Pattern: `[FeatureName]Tests.cs` (e.g., `CharacterCreationTests.cs`, `DoorInteractionServiceTests.cs`)
- Multiple test classes per file when related (e.g., `DiceServiceTests` and `CharacterCreationServiceTests` in same file)

**Directory Structure:**
```
Mordecai.Web.Tests/
├── [FeatureName]Tests.cs          # Test classes
├── Mordecai.Web.Tests.csproj      # Test project file
└── [No subdirectories - flat layout]
```

## Test Structure

**Suite Organization:**
```csharp
public sealed class DoorInteractionServiceTests
{
    [Fact]
    public async Task OpenAsync_ShouldOpenUnlockedDoor()
    {
        // Arrange

        // Act

        // Assert
    }
}
```

**Pattern: Arrange-Act-Assert (AAA)**
- **Arrange**: Set up test data, dependencies, mocks
- **Act**: Call the method being tested
- **Assert**: Verify results and side effects

Example from `CharacterCreationTests.cs`:
```csharp
[Fact]
public void Roll4dF_Should_Return_Value_Between_Minus4_And_Plus4()
{
    // Act & Assert - Test many rolls to ensure range is correct
    for (int i = 0; i < 1000; i++)
    {
        var result = _diceService.Roll4dF();
        Assert.InRange(result, -4, 4);
    }
}
```

**Test Method Naming:**
- `MethodName_Scenario_ExpectedResult` (e.g., `OpenAsync_ShouldOpenUnlockedDoor`)
- Descriptive; tests document expected behavior
- Action first, then outcome

**Fact vs Theory:**
- `[Fact]`: Single test case with fixed inputs
- `[Theory]`: Parameterized test with multiple inputs
- Example from `CharacterCreationTests.cs`:
  ```csharp
  [Theory]
  [InlineData("Human")]
  [InlineData("Elf")]
  [InlineData("Dwarf")]
  public void GenerateRandomAttributes_Should_Respect_Species_Bounds(string species)
  ```

**Async Test Methods:**
- Pattern: `async Task [MethodName]Async()`
- Always use `await` for async operations (database, context factory)
- Example: `public async Task OpenAsync_ShouldOpenUnlockedDoor()`

## Mocking

**Framework:** Custom test doubles (no mocking library)

**Patterns:**

**1. Stub Services (Fixed behavior):**
```csharp
private sealed class StubDiceService : IDiceService
{
    private readonly int _roll;

    public StubDiceService(int roll)
    {
        _roll = roll;
    }

    public int Roll4dF() => _roll;
    public int RollExploding4dF() => _roll;
    public int Roll4dFWithModifier(int modifier, int minValue = 1, int maxValue = 20) => _roll + modifier;
    public int RollMultiple4dF(int count) => _roll * count;
}
```
Used in `DoorInteractionServiceTests.cs` (lines 404-417)

**2. Recording Services (Track calls):**
```csharp
private sealed class RecordingSkillService : ISkillService
{
    public bool AddSkillUsageCalled { get; private set; }

    public Task<bool> AddSkillUsageAsync(Guid characterId, int skillDefinitionId,
        WebSkillUsageType usageType, int baseUsagePoints = 1, string? context = null, string? details = null)
    {
        AddSkillUsageCalled = true;
        return Task.FromResult(true);
    }

    // ... other methods return defaults
}
```
Used in `DoorInteractionServiceTests.cs` (lines 419-436) to verify skill operations were recorded

**3. Test Double Factory Pattern:**
```csharp
private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    {
        _options = options;
    }

    public ApplicationDbContext CreateDbContext() => new(_options);
    public ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new ApplicationDbContext(_options));
}
```
Used in `DoorInteractionServiceTests.cs` (lines 438-449)

**4. Stub Random Number Generator:**
```csharp
private sealed class StubRandomNumberGenerator : RandomNumberGenerator
{
    private readonly Queue<byte> _values;

    public StubRandomNumberGenerator(params byte[] values)
    {
        _values = new Queue<byte>(values);
    }

    public override void GetBytes(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            if (_values.Count == 0)
                throw new InvalidOperationException("Not enough random values provided for test.");
            data[i] = _values.Dequeue();
        }
    }
}
```
Used in `CharacterCreationTests.cs` (lines 71-92) for deterministic dice rolls

**What to Mock:**
- External services: `IDiceService`, `ISkillService`
- Database factory: `IDbContextFactory<ApplicationDbContext>`
- Random number generation: Custom `StubRandomNumberGenerator`
- Logger: `NullLogger<T>.Instance` (no-op logger)

**What NOT to Mock:**
- Entity models: Use real `Character`, `Room`, `Item` instances
- In-memory database context: Real EF Core context with in-memory database
- Game logic calculations: Test with actual formulas
- Value objects and domain models: Instantiate directly

## Fixtures and Factories

**Test Data:**
- **Entity builders**: Inline creation with lambda configuration
  ```csharp
  await SeedDoorScenarioAsync(factory, configureExit: exit =>
  {
      exit.DoorState = DoorState.Closed;
      exit.LockConfiguration = DoorLockType.None;
      exit.IsLocked = false;
  }, cancellationToken: cancellationToken);
  ```

- **Helper methods**: Parameterized seed methods for common scenarios
  ```csharp
  private static async Task<(Guid CharacterId, string UserId, int RoomId, int DestinationRoomId)>
      SeedDoorScenarioAsync(
          IDbContextFactory<ApplicationDbContext> factory,
          Action<RoomExit>? configureExit = null,
          bool seedPhysicalitySkill = false,
          Action<Character>? configureCharacter = null,
          bool createReciprocalExit = true,
          CancellationToken cancellationToken = default)
  ```
  See `DoorInteractionServiceTests.cs` (lines 264-394)

**Location:**
- Private static methods at end of test class
- Parameterized with Action lambdas for customization
- Example: `SeedDoorScenarioAsync()` in `DoorInteractionServiceTests.cs`

**Pattern for Complex Setups:**
```csharp
// In test method
var (characterId, userId, roomId, _) = await SeedDoorScenarioAsync(
    factory,
    configureExit: exit => { /* customize */ },
    configureCharacter: character => { /* customize */ },
    cancellationToken: cancellationToken);
```

## Database Testing

**In-Memory Database:**
- Uses `Microsoft.EntityFrameworkCore.InMemory` (v9.0.0)
- Each test gets unique database: `$"DoorOpen_{Guid.NewGuid()}"`
- Isolation: No test data leakage between tests

**Factory Pattern:**
```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase($"Equipment_Create_{Guid.NewGuid()}")
    .Options;
var factory = new TestDbContextFactory(options);
```

**Verification Pattern:**
```csharp
await using var verification = await factory.CreateDbContextAsync(cancellationToken);
var persisted = await verification.Items.Include(i => i.ItemTemplate)
    .SingleAsync(cancellationToken);
Assert.Equal(characterId, persisted.OwnerCharacterId);
```

**CancellationToken Usage:**
- Passed through all async operations: `await factory.CreateDbContextAsync(cancellationToken)`
- Uses `TestContext.Current.CancellationToken` for test cancellation support

## Coverage

**Requirements:** No coverage requirement enforced (no minimum target detected)

**View Coverage:**
- Would use: `dotnet test /p:CollectCoverage=true` (requires coverlet NuGet package)
- Not currently configured in project

**Coverage Gaps Observed:**
- Test focus: Core features (dice, character creation, doors, equipment, combat)
- Covered: Game mechanics formulas, integration scenarios, error paths
- Likely gaps: Admin CLI commands, background services (partially covered in `HealthTickBackgroundServiceTests.cs`)

## Test Types

**Unit Tests:**
- **Scope**: Single service/component in isolation
- **Approach**: Inject dependencies, mock external services, test single responsibility
- **Example**: `Roll4dF_Should_Return_Value_Between_Minus4_And_Plus4()` tests only `DiceService.Roll4dF()`
- **Setup**: Minimal, often just constructor

**Integration Tests:**
- **Scope**: Full feature flow with real database and multiple components
- **Approach**: In-memory database, real services, verify side effects
- **Example**: `OpenAsync_ShouldOpenUnlockedDoor()` tests entire door opening flow including database persistence
- **Setup**: Seed data via `SeedDoorScenarioAsync()` or inline entity creation
- **Assertion**: Verify database state changed correctly

**E2E Tests:**
- Not found in codebase (Blazor UI testing would be separate)
- Comment in `.csproj` indicates not yet configured

## Common Patterns

**Statistical/Probabilistic Testing:**
```csharp
for (int i = 0; i < 1000; i++)
{
    var result = _diceService.Roll4dF();
    Assert.InRange(result, -4, 4);
}
```
Run multiple iterations to verify randomness behavior (see `CharacterCreationTests.cs` lines 22-26)

**Async Testing:**
```csharp
[Fact]
public async Task OpenAsync_ShouldOpenUnlockedDoor()
{
    // ... Arrange ...
    var result = await service.OpenAsync(characterId, userId, roomId, "north", cancellationToken);
    // ... Assert ...
}
```

**Deterministic Dice Rolls in Tests:**
```csharp
var dice = new StubDiceService(0);  // Always return 0
var result = await service.AttemptBreakLockAsync(...);
Assert.Equal(10, result.AbilityScore);  // Ability 10 + 0 roll = 10
```

**Error Path Testing:**
```csharp
[Fact]
public async Task LockWithDeviceAsync_ShouldFail_WhenDeviceCodeDoesNotMatch()
{
    // ... setup with device code "golden-key" ...
    var result = await service.LockWithDeviceAsync(characterId, userId, roomId, "north", "iron-key", cancellationToken);
    Assert.False(result.Success);
    Assert.Contains("does not fit", result.Message, StringComparison.OrdinalIgnoreCase);
}
```

**State Verification via Query:**
```csharp
// After operation
await using var verification = await factory.CreateDbContextAsync(cancellationToken);
var exit = await verification.RoomExits.SingleAsync(e => e.FromRoomId == roomId, cancellationToken);
Assert.Equal(DoorState.Open, exit.DoorState);
Assert.False(exit.IsLocked);
```

**Result Record Assertions:**
```csharp
Assert.True(result.Success);
Assert.True(result.HasCheckDetails);
Assert.Equal(10, result.AbilityScore);
Assert.Equal(2, result.DiceRoll);
Assert.Equal(12, result.Total);
Assert.Equal(11, result.TargetValue);
```

## Test Characteristics

**Strengths Observed:**
- Clear naming makes test intent obvious
- Integration tests verify realistic scenarios (doors, equipment, combat)
- Deterministic test doubles allow predictable outcomes
- In-memory database prevents test pollution
- Comprehensive game mechanics testing (dice rolls, equipment bonuses, health ticks)

**Test Isolation:**
- Each test creates own in-memory database instance via unique name
- No shared state between tests
- Fresh context for verification queries

**Async Handling:**
- All database operations properly awaited
- `CancellationToken` threaded through async chains
- Test context provides cancellation support

---

*Testing analysis: 2026-01-26*
