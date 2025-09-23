# Mordecai MUD - Skill System Implementation Test Instructions

## Database Migration Test

1. **Start the application**:
   ```bash
   dotnet run --project Mordecai.AppHost
   ```

2. **The migration should apply automatically** when the application starts due to the code in `Program.cs`:
   ```csharp
   context.Database.Migrate();
   ```

## Manual Testing Steps

### 1. Character Creation Test
1. Navigate to: `https://localhost:7001` (or whatever port is shown)
2. Register/Login with a test account
3. Go to **Characters** page
4. Create a new character:
   - Try different species (Human, Elf, Dwarf, Halfling, Orc)
   - Notice how species modifiers affect attributes
   - Observe the "Starting Attribute Skills" preview showing calculated ability scores
   - Complete character creation

### 2. Skills Viewer Test
1. After creating a character, click **"View Skills"** on the character card
2. You should see:
   - **7 base attribute skills** automatically created
   - Each skill showing **Level 0** initially
   - **Ability Scores** calculated as: `Attribute - 5 + Level`
   - **Experience bars** (empty since newly created)
   - Skills grouped by type ("AttributeSkill")

### 3. Species Comparison Test
Create one character of each species and compare their skills:

#### Expected Ability Scores for Average Attributes (10):
- **Human (no modifiers)**: All skills AS = 5 (10 - 5 + 0)
- **Elf (+1 INT, -1 STR)**: Reasoning AS = 6, Physicality AS = 4, others = 5
- **Dwarf (+1 STR, -1 DEX)**: Physicality AS = 6, Dodge AS = 4, others = 5
- **Halfling (+1 DEX, +1 ITT, -2 STR)**: Dodge AS = 6, Awareness AS = 6, Physicality AS = 3, others = 5
- **Orc (+2 STR, +1 END, -1 INT, -1 PHY)**: Physicality AS = 7, Drive AS = 6, Reasoning AS = 4, Bearing AS = 4, others = 5

### 4. Health System Test
1. Check character cards show correct health values:
   - **Fatigue (FAT)**: (Drive × 2) - 5
   - **Vitality (VIT)**: (Physicality + Drive) - 5
2. In the game interface (`/play/{characterId}`), health bars should display correctly

### 5. Database Verification Test
If you have a SQLite browser, check the database contains:
- **SkillDefinitions** table with 7 base attribute skills
- **CharacterSkills** table with entries for each created character
- **Characters** table with health tracking fields

## Expected Skill Definitions
The system should create these 7 skills automatically:

| Skill Name | Type | Related Attribute | Description |
|------------|------|-------------------|-------------|
| Physicality | AttributeSkill | Physicality | Physical strength and power for melee combat, carrying capacity, and physical feats |
| Dodge | AttributeSkill | Dodge | Agility and evasion ability for avoiding attacks and quick movements |
| Drive | AttributeSkill | Drive | Endurance and stamina for sustained activities and health |
| Reasoning | AttributeSkill | Reasoning | Intelligence and logical thinking for problem-solving and learning |
| Awareness | AttributeSkill | Awareness | Intuition and perception for detecting danger and hidden things |
| Focus | AttributeSkill | Focus | Willpower and mental concentration for spellcasting and mental resistance |
| Bearing | AttributeSkill | Bearing | Physical beauty and social presence for leadership and persuasion |

## Troubleshooting

### Migration Issues
If you get migration errors:
1. Delete the SQLite database file (usually in the project root)
2. Restart the application to rebuild the database

### No Skills Appearing
1. Check the application logs for errors during skill initialization
2. Verify the `InitializeBaseAttributeSkillsAsync()` method is being called in `Program.cs`

### Compilation Errors
1. Ensure all NuGet packages are restored: `dotnet restore`
2. Clean and rebuild: `dotnet clean && dotnet build`

## Future Testing
Once the basic system is working, you can test:
1. Skill usage tracking (when implemented)
2. Experience gaining (when implemented)
3. Level advancement (when implemented)
4. Combat integration with ability scores (when implemented)

## Implementation Notes
- All characters automatically get the 7 base attribute skills
- Ability Score formula: `AS = Attribute - 5 + Level`
- Starting characters have Level 0 in all skills
- Health is calculated from attributes and tracked with pending damage pools
- System is designed for future expansion to weapon skills, spell skills, etc.