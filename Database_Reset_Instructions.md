# Mordecai MUD - Database Reset and Test Script

## PowerShell Commands to Reset and Test

```powershell
# Navigate to the project directory
cd S:\src\mordecai-mud

# Remove any existing database file
Remove-Item -Path "Mordecai.Web\mordecai.db" -ErrorAction SilentlyContinue
Remove-Item -Path "mordecai.db" -ErrorAction SilentlyContinue

# Clean and rebuild the project
dotnet clean
dotnet build

# Run the application
dotnet run --project Mordecai.AppHost
```

## What Should Happen

1. **Database Creation**: The application should automatically:
   - Create a new SQLite database
   - Apply all migrations
   - Initialize the 7 base attribute skills

2. **Startup Logs**: You should see logs like:
   ```
   info: Program[0] Applying database migrations...
   info: Program[0] Database migrations applied successfully.
   info: Program[0] Initializing base attribute skills...
   info: Program[0] Base attribute skills initialized successfully.
   ```

3. **Application Ready**: The application should start successfully and be accessible via browser

## Testing the Skill System

Once the application starts:

1. **Register/Login**: Create a user account
2. **Character Creation**: 
   - Go to `/characters`
   - Create a new character
   - Observe the skill preview showing calculated ability scores
3. **Skills Viewer**:
   - Click "View Skills" on any character
   - Verify 7 base attribute skills are present
   - Check ability score calculations

## Troubleshooting

If you still get migration errors:

1. **Manual Database Deletion**:
   ```powershell
   # Find and delete any database files
   Get-ChildItem -Path . -Name "*.db" -Recurse | Remove-Item -Force
   ```

2. **Clear EF Cache**:
   ```powershell
   # Clear any cached EF metadata
   dotnet ef database drop --project Mordecai.Web --force
   ```

3. **Reset Migrations** (if needed):
   ```powershell
   # Remove the last migration and recreate it
   dotnet ef migrations remove --project Mordecai.Web
   dotnet ef migrations add AddSkillSystemAndHealthTracking --project Mordecai.Web
   ```

## Expected Database Tables

After successful startup, the database should contain:

- **SkillDefinitions**: 7 rows for base attribute skills
- **Characters**: User-created characters
- **CharacterSkills**: Skills for each character (7 per character)
- **Plus all existing tables**: Zones, Rooms, RoomTypes, etc.

## Verification Query

If you have a SQLite browser, you can verify the data:

```sql
-- Check skill definitions
SELECT * FROM SkillDefinitions WHERE IsStartingSkill = 1;

-- Check character skills for a specific character
SELECT cs.*, sd.Name, sd.RelatedAttribute 
FROM CharacterSkills cs 
JOIN SkillDefinitions sd ON cs.SkillDefinitionId = sd.Id 
WHERE cs.CharacterId = '<some-character-id>';
```