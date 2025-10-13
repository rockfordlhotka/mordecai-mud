using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

public interface IItemTemplateService
{
    Task<List<ItemTemplate>> GetItemTemplatesAsync(bool includeInactive = false);
    Task<ItemTemplate?> GetItemTemplateAsync(int id);
    Task<ItemTemplate> CreateItemTemplateAsync(ItemTemplate template);
    Task<ItemTemplate> UpdateItemTemplateAsync(ItemTemplate template);
}

public sealed class ItemTemplateService : IItemTemplateService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ItemTemplateService> _logger;

    public ItemTemplateService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<ItemTemplateService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<ItemTemplate>> GetItemTemplatesAsync(bool includeInactive = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.ItemTemplates
            .Include(it => it.WeaponProperties)
            .Include(it => it.ArmorProperties)
            .Include(it => it.SkillBonuses)
            .Include(it => it.AttributeModifiers)
            .OrderBy(it => it.ItemType)
            .ThenBy(it => it.DisplayOrder)
            .ThenBy(it => it.Name)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(it => it.IsActive);
        }

        return await query.ToListAsync();
    }

    public async Task<ItemTemplate?> GetItemTemplateAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.ItemTemplates
            .Include(it => it.WeaponProperties)
            .Include(it => it.ArmorProperties)
            .Include(it => it.SkillBonuses)
            .Include(it => it.AttributeModifiers)
            .FirstOrDefaultAsync(it => it.Id == id);
    }

    public async Task<ItemTemplate> CreateItemTemplateAsync(ItemTemplate template)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        NormalizeTemplate(template);

        await EnsureSkillReferencesAsync(context, template);

        _logger.LogDebug("Creating item template {Name} weapon skill {WeaponSkill} armor skill {ArmorSkill}",
            template.Name,
            template.WeaponProperties?.SkillDefinitionId,
            template.ArmorProperties?.SkillDefinitionId);

        context.ItemTemplates.Add(template);

        await context.SaveChangesAsync();
        _logger.LogInformation("Created item template {TemplateId} ({TemplateName}) of type {ItemType}",
            template.Id, template.Name, template.ItemType);

        return template;
    }

    public async Task<ItemTemplate> UpdateItemTemplateAsync(ItemTemplate template)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.ItemTemplates
            .Include(it => it.WeaponProperties)
            .Include(it => it.ArmorProperties)
            .FirstOrDefaultAsync(it => it.Id == template.Id);

        if (existing == null)
        {
            throw new InvalidOperationException($"Item template {template.Id} was not found.");
        }

        // Preserve immutable fields
        template.CreatedAt = existing.CreatedAt;
        template.CreatedBy = existing.CreatedBy;

        NormalizeTemplate(template);

        await EnsureSkillReferencesAsync(context, template);

        _logger.LogDebug("Updating item template {Id} weapon skill {WeaponSkill} armor skill {ArmorSkill}",
            template.Id,
            template.WeaponProperties?.SkillDefinitionId,
            template.ArmorProperties?.SkillDefinitionId);

        context.Entry(existing).CurrentValues.SetValues(template);

        UpdateWeaponProperties(context, existing, template);
        UpdateArmorProperties(context, existing, template);

        await context.SaveChangesAsync();
        _logger.LogInformation("Updated item template {TemplateId} ({TemplateName}) of type {ItemType}",
            template.Id, template.Name, template.ItemType);

        return existing;
    }

    private static void NormalizeTemplate(ItemTemplate template)
    {
        template.Name = template.Name.Trim();
        template.Description = template.Description.Trim();
        template.ShortDescription = string.IsNullOrWhiteSpace(template.ShortDescription)
            ? null
            : template.ShortDescription.Trim();
        template.CustomProperties = string.IsNullOrWhiteSpace(template.CustomProperties)
            ? null
            : template.CustomProperties.Trim();

        if (!template.HasDurability)
        {
            template.MaxDurability = null;
        }

        if (template.ItemType != ItemType.Weapon)
        {
            template.WeaponType = null;
            template.WeaponProperties = null;
        }

        if (template.ItemType != ItemType.Armor)
        {
            template.ArmorProperties = null;
        }

        if (template.ItemType is not ItemType.Weapon and not ItemType.Armor)
        {
            template.ArmorSlot = null;
        }
    }

    private static void UpdateWeaponProperties(
        ApplicationDbContext context,
        ItemTemplate existing,
        ItemTemplate inbound)
    {
        if (inbound.ItemType != ItemType.Weapon)
        {
            if (existing.WeaponProperties != null)
            {
                context.WeaponTemplateProperties.Remove(existing.WeaponProperties);
                existing.WeaponProperties = null;
            }
            return;
        }

        if (inbound.WeaponProperties == null)
        {
            if (existing.WeaponProperties != null)
            {
                context.WeaponTemplateProperties.Remove(existing.WeaponProperties);
                existing.WeaponProperties = null;
            }
            return;
        }

        inbound.WeaponProperties.ItemTemplateId = existing.Id;

        if (existing.WeaponProperties == null)
        {
            existing.WeaponProperties = inbound.WeaponProperties;
            context.WeaponTemplateProperties.Add(existing.WeaponProperties);
        }
        else
        {
            context.Entry(existing.WeaponProperties).CurrentValues.SetValues(inbound.WeaponProperties);
        }
    }

    private static void UpdateArmorProperties(
        ApplicationDbContext context,
        ItemTemplate existing,
        ItemTemplate inbound)
    {
        if (inbound.ItemType != ItemType.Armor)
        {
            if (existing.ArmorProperties != null)
            {
                context.ArmorTemplateProperties.Remove(existing.ArmorProperties);
                existing.ArmorProperties = null;
            }
            return;
        }

        if (inbound.ArmorProperties == null)
        {
            if (existing.ArmorProperties != null)
            {
                context.ArmorTemplateProperties.Remove(existing.ArmorProperties);
                existing.ArmorProperties = null;
            }
            return;
        }

        inbound.ArmorProperties.ItemTemplateId = existing.Id;

        if (existing.ArmorProperties == null)
        {
            existing.ArmorProperties = inbound.ArmorProperties;
            context.ArmorTemplateProperties.Add(existing.ArmorProperties);
        }
        else
        {
            context.Entry(existing.ArmorProperties).CurrentValues.SetValues(inbound.ArmorProperties);
        }
    }

    private static async Task EnsureSkillReferencesAsync(ApplicationDbContext context, ItemTemplate template)
    {
        if (template.WeaponProperties is not null)
        {
            var weaponSkillId = template.WeaponProperties.SkillDefinitionId;
            if (weaponSkillId is int id && id > 0)
            {
                var exists = await context.SkillDefinitions
                    .AsNoTracking()
                    .AnyAsync(sd => sd.Id == id);

                if (!exists)
                {
                    template.WeaponProperties.SkillDefinitionId = null;
                }
            }
            else
            {
                template.WeaponProperties.SkillDefinitionId = null;
            }
        }

        if (template.ArmorProperties is not null)
        {
            var armorSkillId = template.ArmorProperties.SkillDefinitionId;
            if (armorSkillId is int id && id > 0)
            {
                var exists = await context.SkillDefinitions
                    .AsNoTracking()
                    .AnyAsync(sd => sd.Id == id);

                if (!exists)
                {
                    template.ArmorProperties.SkillDefinitionId = null;
                }
            }
            else
            {
                template.ArmorProperties.SkillDefinitionId = null;
            }
        }
    }
}
