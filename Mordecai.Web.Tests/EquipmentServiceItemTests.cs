using System.Linq;
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

    [Fact]
    public async Task PickUpItemAsync_ShouldAssignOwnershipAndClearRoom()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Equipment_Pick_{Guid.NewGuid()}")
            .Options;
        var factory = new TestDbContextFactory(options);
        var characterId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var itemId = Guid.NewGuid();
        const int roomId = 11;
        var cancellationToken = TestContext.Current.CancellationToken;

        await using (var context = await factory.CreateDbContextAsync(cancellationToken))
        {
            context.Characters.Add(new Character
            {
                Id = characterId,
                Name = "Picker",
                UserId = userId,
                CurrentFatigue = 5,
                CurrentVitality = 5,
                PendingFatigueDamage = 0,
                PendingVitalityDamage = 0
            });

            var template = new ItemTemplate
            {
                Id = 5,
                Name = "Ground Dagger",
                Description = "A dagger ready to be claimed.",
                ItemType = ItemType.Weapon,
                ArmorSlot = ArmorSlot.MainHand,
                Weight = 0.5m,
                Volume = 0.25m,
                Value = 15,
                IsDroppable = true,
                BindOnPickup = true,
                CreatedBy = "tests",
                WeaponType = WeaponType.Dagger,
                WeaponProperties = new WeaponTemplateProperties
                {
                    ItemTemplateId = 5,
                    DamageType = DamageType.Piercing,
                    DamageClass = DamageClass.Class1
                }
            };

            context.ItemTemplates.Add(template);

            context.Items.Add(new Item
            {
                Id = itemId,
                ItemTemplateId = template.Id,
                ItemTemplate = template,
                CurrentRoomId = roomId,
                StackSize = 1,
                IsBound = false,
                PickedUpAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        var service = new EquipmentService(factory, NullLogger<EquipmentService>.Instance);
        var result = await service.PickUpItemAsync(characterId, userId, itemId, roomId);

        Assert.True(result.Success);
        Assert.NotNull(result.Item);

        await using var verificationContext = await factory.CreateDbContextAsync(cancellationToken);
        var persisted = await verificationContext.Items.SingleAsync(i => i.Id == itemId, cancellationToken);
        Assert.Equal(characterId, persisted.OwnerCharacterId);
        Assert.Null(persisted.CurrentRoomId);
        Assert.False(persisted.IsEquipped);
        Assert.Null(persisted.EquippedSlot);
        Assert.True(persisted.IsBound);
        Assert.NotEqual(default, persisted.PickedUpAt);
        Assert.True(persisted.PickedUpAt >= DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task EquipAsync_ShouldAllowArmorWithChestSlot()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Equipment_EquipArmor_{Guid.NewGuid()}")
            .Options;
        var factory = new TestDbContextFactory(options);
        var characterId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var itemId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        await using (var context = await factory.CreateDbContextAsync(cancellationToken))
        {
            context.Characters.Add(new Character
            {
                Id = characterId,
                Name = "Armorer",
                UserId = userId,
                CurrentFatigue = 5,
                CurrentVitality = 5,
                PendingFatigueDamage = 0,
                PendingVitalityDamage = 0
            });

            var template = new ItemTemplate
            {
                Id = 9,
                Name = "Padded Vest",
                Description = "Simple padded armor for the torso.",
                ItemType = ItemType.Armor,
                ArmorSlot = ArmorSlot.Chest,
                Weight = 3m,
                Volume = 1.2m,
                Value = 12,
                IsDroppable = true,
                CreatedBy = "tests",
                ArmorProperties = new ArmorTemplateProperties
                {
                    ItemTemplateId = 9,
                    DamageClass = DamageClass.Class1,
                    BashingAbsorption = 1,
                    CuttingAbsorption = 1,
                    PiercingAbsorption = 1
                }
            };

            context.ItemTemplates.Add(template);

            context.Items.Add(new Item
            {
                Id = itemId,
                ItemTemplateId = template.Id,
                ItemTemplate = template,
                OwnerCharacterId = characterId,
                StackSize = 1
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        var service = new EquipmentService(factory, NullLogger<EquipmentService>.Instance);
        var result = await service.EquipAsync(characterId, userId, itemId);

        Assert.True(result.Success);
        Assert.NotNull(result.Item);

        await using var verificationContext = await factory.CreateDbContextAsync(cancellationToken);
        var persisted = await verificationContext.Items.SingleAsync(i => i.Id == itemId, cancellationToken);
        Assert.True(persisted.IsEquipped);
        Assert.Equal(ArmorSlot.Chest, persisted.EquippedSlot);
    }

    [Fact]
    public async Task EquipAsync_ShouldInferArmorSlotWhenMissing()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Equipment_InferSlot_{Guid.NewGuid()}")
            .Options;
        var factory = new TestDbContextFactory(options);
        var characterId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var itemId = Guid.NewGuid();
        const int templateId = 10;
        var cancellationToken = TestContext.Current.CancellationToken;

        await using (var context = await factory.CreateDbContextAsync(cancellationToken))
        {
            context.Characters.Add(new Character
            {
                Id = characterId,
                Name = "Infernal",
                UserId = userId,
                CurrentFatigue = 5,
                CurrentVitality = 5,
                PendingFatigueDamage = 0,
                PendingVitalityDamage = 0
            });

            var template = new ItemTemplate
            {
                Id = templateId,
                Name = "Linen Shirt",
                Description = "A simple linen shirt covering the chest.",
                ItemType = ItemType.Armor,
                ArmorSlot = null,
                Weight = 1.5m,
                Volume = 0.8m,
                IsDroppable = true,
                CreatedBy = "tests",
                ArmorProperties = new ArmorTemplateProperties
                {
                    ItemTemplateId = templateId,
                    DamageClass = DamageClass.Class1,
                    HitLocationCoverage = "Chest, Torso"
                }
            };

            context.ItemTemplates.Add(template);

            context.Items.Add(new Item
            {
                Id = itemId,
                ItemTemplateId = template.Id,
                ItemTemplate = template,
                OwnerCharacterId = characterId,
                StackSize = 1
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        var service = new EquipmentService(factory, NullLogger<EquipmentService>.Instance);
        var result = await service.EquipAsync(characterId, userId, itemId);

        Assert.True(result.Success);
        Assert.NotNull(result.Item);

        await using (var verificationContext = await factory.CreateDbContextAsync(cancellationToken))
        {
            var persistedItem = await verificationContext.Items.SingleAsync(i => i.Id == itemId, cancellationToken);
            Assert.True(persistedItem.IsEquipped);
            Assert.Equal(ArmorSlot.Chest, persistedItem.EquippedSlot);

            var template = await verificationContext.ItemTemplates.SingleAsync(t => t.Id == templateId, cancellationToken);
            Assert.Equal(ArmorSlot.Chest, template.ArmorSlot);
        }
    }

    [Fact]
    public async Task GetRoomItemsAsync_ShouldReturnOnlyFloorItems()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Equipment_Room_{Guid.NewGuid()}")
            .Options;
        var factory = new TestDbContextFactory(options);
        const int roomId = 7;
        var otherRoomId = 99;
        var cancellationToken = TestContext.Current.CancellationToken;

        await using (var context = await factory.CreateDbContextAsync(cancellationToken))
        {
            context.ItemTemplates.Add(new ItemTemplate
            {
                Id = 1,
                Name = "Floor Coin",
                Description = "A coin on the floor.",
                ItemType = ItemType.Treasure,
                Weight = 0.1m,
                Volume = 0.1m,
                Value = 1,
                IsDroppable = true,
                CreatedBy = "tests"
            });

            context.ItemTemplates.Add(new ItemTemplate
            {
                Id = 2,
                Name = "Pocket Gem",
                Description = "A gem in inventory.",
                ItemType = ItemType.Treasure,
                Weight = 0.1m,
                Volume = 0.1m,
                Value = 5,
                IsDroppable = true,
                CreatedBy = "tests"
            });

            context.ItemTemplates.Add(new ItemTemplate
            {
                Id = 3,
                Name = "Wooden Crate",
                Description = "A crate acting as a container.",
                ItemType = ItemType.Container,
                Weight = 5m,
                Volume = 3m,
                Value = 2,
                IsDroppable = true,
                CreatedBy = "tests"
            });

            var containerId = Guid.NewGuid();

            context.Items.AddRange(
                new Item
                {
                    Id = Guid.NewGuid(),
                    ItemTemplateId = 1,
                    CurrentRoomId = roomId,
                    StackSize = 1
                },
                new Item
                {
                    Id = Guid.NewGuid(),
                    ItemTemplateId = 2,
                    OwnerCharacterId = Guid.NewGuid(),
                    StackSize = 1
                },
                new Item
                {
                    Id = Guid.NewGuid(),
                    ItemTemplateId = 1,
                    CurrentRoomId = otherRoomId,
                    StackSize = 1
                },
                new Item
                {
                    Id = containerId,
                    ItemTemplateId = 3,
                    CurrentRoomId = roomId,
                    StackSize = 1
                },
                new Item
                {
                    Id = Guid.NewGuid(),
                    ItemTemplateId = 1,
                    ContainerItemId = containerId,
                    StackSize = 1
                });

            await context.SaveChangesAsync(cancellationToken);
        }

        var service = new EquipmentService(factory, NullLogger<EquipmentService>.Instance);
        var roomItems = await service.GetRoomItemsAsync(roomId);

        Assert.Equal(2, roomItems.Count);
        Assert.Equal(1, roomItems.Count(item => item.ItemTemplateId == 1));
        Assert.Contains(roomItems, item => item.ItemTemplateId == 3);
        Assert.All(roomItems, item =>
        {
            Assert.Equal(roomId, item.CurrentRoomId);
            Assert.False(item.OwnerCharacterId.HasValue);
            Assert.False(item.ContainerItemId.HasValue);
        });
        Assert.DoesNotContain(roomItems, item => item.ItemTemplateId == 2);
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
