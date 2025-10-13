using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Mordecai.Web.Services;
using Xunit;

namespace Mordecai.Web.Tests;

public sealed class EquipmentServiceItemTests
{
    [Fact]
    public async Task CreateItemForCharacterAsync_ShouldCreateItemOwnedByCharacter()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Equipment_Create_{Guid.NewGuid()}")
            .Options;
        var factory = new TestDbContextFactory(options);
        var characterId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var cancellationToken = TestContext.Current.CancellationToken;

        await using (var context = await factory.CreateDbContextAsync(cancellationToken))
        {
            context.Characters.Add(new Character
            {
                Id = characterId,
                Name = "Tester",
                UserId = userId,
                CurrentFatigue = 5,
                CurrentVitality = 5,
                PendingFatigueDamage = 0,
                PendingVitalityDamage = 0
            });

            context.ItemTemplates.Add(new ItemTemplate
            {
                Id = 1,
                Name = "Test Sword",
                Description = "A sword for testing.",
                ItemType = ItemType.Weapon,
                ArmorSlot = ArmorSlot.MainHand,
                Weight = 1m,
                Volume = 1m,
                Value = 10,
                IsDroppable = true,
                CreatedBy = "tests",
                WeaponType = WeaponType.Sword,
                WeaponProperties = new WeaponTemplateProperties
                {
                    ItemTemplateId = 1,
                    DamageType = DamageType.Cutting,
                    DamageClass = DamageClass.Class1
                }
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        var service = new EquipmentService(factory, NullLogger<EquipmentService>.Instance);
        var result = await service.CreateItemForCharacterAsync(characterId, userId, 1);

        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal(characterId, result.Item!.OwnerCharacterId);

        await using var verificationContext = await factory.CreateDbContextAsync(cancellationToken);
        var persisted = await verificationContext.Items.Include(i => i.ItemTemplate).SingleAsync(cancellationToken);
        Assert.Equal(characterId, persisted.OwnerCharacterId);
        Assert.Null(persisted.CurrentRoomId);
        Assert.Equal(1, persisted.ItemTemplateId);
        Assert.Equal("Test Sword", persisted.ItemTemplate.Name);
    }

    [Fact]
    public async Task DropItemAsync_ShouldMoveItemToRoomAndUnequip()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Equipment_Drop_{Guid.NewGuid()}")
            .Options;
        var factory = new TestDbContextFactory(options);
        var characterId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var itemId = Guid.NewGuid();
        const int roomId = 42;
        var cancellationToken = TestContext.Current.CancellationToken;

        await using (var context = await factory.CreateDbContextAsync(cancellationToken))
        {
            context.Characters.Add(new Character
            {
                Id = characterId,
                Name = "Dropper",
                UserId = userId,
                CurrentFatigue = 5,
                CurrentVitality = 5,
                PendingFatigueDamage = 0,
                PendingVitalityDamage = 0
            });

            var template = new ItemTemplate
            {
                Id = 1,
                Name = "Test Club",
                Description = "A club for dropping.",
                ItemType = ItemType.Weapon,
                ArmorSlot = ArmorSlot.MainHand,
                Weight = 1m,
                Volume = 1m,
                Value = 5,
                IsDroppable = true,
                CreatedBy = "tests",
                WeaponType = WeaponType.Mace,
                WeaponProperties = new WeaponTemplateProperties
                {
                    ItemTemplateId = 1,
                    DamageType = DamageType.Bashing,
                    DamageClass = DamageClass.Class1
                }
            };

            context.ItemTemplates.Add(template);

            context.Items.Add(new Item
            {
                Id = itemId,
                ItemTemplateId = template.Id,
                ItemTemplate = template,
                OwnerCharacterId = characterId,
                IsEquipped = true,
                EquippedSlot = ArmorSlot.MainHand,
                StackSize = 1,
                CurrentRoomId = null,
                ContainerItemId = null,
                IsBound = false,
                PickedUpAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        var service = new EquipmentService(factory, NullLogger<EquipmentService>.Instance);
        var result = await service.DropItemAsync(characterId, userId, itemId, roomId);

        Assert.True(result.Success);
        Assert.NotNull(result.Item);

        await using var verificationContext = await factory.CreateDbContextAsync(cancellationToken);
        var persisted = await verificationContext.Items.SingleAsync(i => i.Id == itemId, cancellationToken);
        Assert.Null(persisted.OwnerCharacterId);
        Assert.Equal(roomId, persisted.CurrentRoomId);
        Assert.False(persisted.IsEquipped);
        Assert.Null(persisted.EquippedSlot);
    }

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
}
