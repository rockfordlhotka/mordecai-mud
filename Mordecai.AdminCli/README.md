# Mordecai Admin CLI

A command-line administration tool for the Mordecai MUD game server.

## Installation & Setup

1. Build the project:
   ```bash
   dotnet build
   ```

2. **Option 1**: Run using dotnet (from project root):
   ```bash
   dotnet run --project "Mordecai.AdminCli" -- [command] [options]
   ```

3. **Option 2**: Run the compiled executable directly (from anywhere):
   ```bash
   # From the project root directory
   .\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe [command] [options]
   
   # Or copy the executable to your PATH for global access
   ```

The compiled executable automatically finds the database file regardless of where you run it from.

## Commands

### User Management

#### `list-users` - List all users in the system
```bash
# Using dotnet run
dotnet run --project "Mordecai.AdminCli" -- list-users

# Using compiled executable
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-users

# Show only admin users
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-users --admins

# Show detailed information
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-users --detailed
```

#### `make-admin` - Grant or revoke admin role for a user
```bash
# Grant admin role to a user
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe make-admin --user john@example.com

# Revoke admin role from a user
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe make-admin --user john@example.com --revoke

# Force operation without confirmation
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe make-admin -u admin@example.com --force
```

#### `set-password` - Set or reset a user's password
```bash
# Set password interactively (prompts for password)
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe set-password --user john@example.com

# Set a specific password
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe set-password --user john@example.com --password "NewPassword123!"

# Generate a secure random password
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe set-password --user john@example.com --generate

# Generate and show the password (for sharing with user)
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe set-password --user john@example.com --generate --show --force
```

### Zone Management

#### `list-zones` - List all zones in the game world
```bash
# List all zones
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-zones

# Show only active zones
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-zones --active

# Show detailed information including room counts
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-zones --detailed

# Filter by difficulty level
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-zones --difficulty 2
```

#### `create-zone` - Create a new zone in the game world
```bash
# Create a basic zone
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe create-zone --name "Whispering Woods" --description "A mysterious forest full of ancient secrets"

# Create an advanced zone with custom settings
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe create-zone --name "Dragon's Lair" --description "The fearsome lair of an ancient dragon" --difficulty 5 --indoor --weather "Hot" --creator "WorldBuilder"

# Create without confirmation prompt
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe create-zone -n "Peaceful Meadow" -d "A serene grassland" --force
```

#### `show-zone` - Show detailed information about a specific zone
```bash
# Show zone by ID
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe show-zone 1

# Show zone by name
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe show-zone "Whispering Woods"

# Show zone with room details
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe show-zone "Dragon's Lair" --rooms
```

### Legacy Aliases

For backwards compatibility, these shorter commands are also available:

```bash
# Short aliases (same functionality)
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list      # alias for list-users
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe admin     # alias for make-admin
```

## Password Management

The `set-password` command provides secure password management with multiple options:

### Password Options
- **Interactive Mode**: Prompts for password with confirmation (default)
- **Direct Password**: Use `--password` to specify the new password
- **Generated Password**: Use `--generate` to create a secure random password
- **Show Password**: Use `--show` with `--generate` to display the generated password

### Security Features
- ? **Password Validation**: Enforces ASP.NET Core Identity password policies
- ? **Confirmation Prompts**: Prevents accidental password changes
- ? **Secure Generation**: Creates 12-character passwords with mixed character types
- ? **Hidden Input**: Interactive mode hides password input for security
- ? **Force Override**: Use `--force` to skip confirmations for scripting

### Password Generation
Generated passwords include:
- Uppercase letters (A-Z)
- Lowercase letters (a-z)  
- Numbers (0-9)
- Special characters (!@#$%^&*)
- Total length: 12 characters
- Cryptographically shuffled for security

## Configuration

The tool uses `appsettings.json` for configuration, but **automatically finds the database file** regardless of where you run the executable from.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Mordecai.Web/mordecai.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

The tool intelligently searches for the database in these locations (in order):
1. `./Mordecai.Web/mordecai.db` (relative to current directory)
2. Relative to the executable location
3. The exact path specified in configuration
4. Creates a new database in the current directory as fallback

## Database Requirements

The tool automatically:
- Finds the existing database file regardless of execution location
- Creates the database if it doesn't exist
- Creates the Admin role if it doesn't exist
- Applies any pending migrations

## Quick Start Examples

### First-time setup
```bash
# Build the tool
dotnet build

# Grant yourself admin privileges
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe make-admin --user your-email@example.com

# Set your password (will prompt securely)
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe set-password --user your-email@example.com

# Create your first zone
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe create-zone --name "Tutorial Area" --description "A safe starting area for new players" --force
```

### Daily management
```bash
# Check user status
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-users --admins

# Reset a user's password with generated password
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe set-password --user user@example.com --generate --show --force

# Review world structure  
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-zones --detailed

# Add new content
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe create-zone --name "Darkwood Forest" --description "A dangerous forest filled with hostile creatures" --difficulty 3 --weather "Foggy"
```

## CLI Features

### Smart Database Location
- ? **Works from any directory** - no need to be in a specific folder
- ? **Automatic database discovery** - finds your existing database file
- ? **Fallback creation** - creates new database if none found
- ? **Robust path handling** - works with relative and absolute paths

### Zone Creation Options
- **--name|-n**: Zone name (required)
- **--description|-d**: Zone description (required) 
- **--difficulty**: Difficulty level 1-10 (default: 1)
- **--weather|-w**: Weather type (default: "Clear")
  - Valid types: Clear, Cloudy, Rainy, Stormy, Snowy, Foggy, Windy, Hot, Cold, Humid, Dry
- **--creator|-c**: Creator name (default: "CLI")
- **--indoor**: Mark as indoor zone (default: outdoor)
- **--inactive**: Mark as inactive (default: active)
- **--force|-f**: Skip confirmation prompts

### User Management Options
- **--user|-u**: Email or username (required for make-admin and set-password)
- **--revoke|-r**: Remove admin role instead of granting (make-admin)
- **--password|-p**: Specify new password directly (set-password)
- **--generate|-g**: Generate secure random password (set-password)
- **--show**: Display generated password in output (set-password)
- **--force|-f**: Skip confirmation prompts
- **--admins**: Filter to show only admin users (list-users)
- **--detailed**: Show additional user information (list-users)

### Display Features
- ? **Rich colored output** - difficulty levels and statuses are color-coded
- ? **Beautiful tables** - formatted tables with borders and proper alignment
- ? **Smart filtering** - filter by active status, difficulty level, admin status
- ? **Detailed views** - show room counts, creation details, user information
- ? **Comprehensive help** - built-in help system with examples

## Deployment Options

### Option 1: Development Use
Run directly from the build output:
```bash
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe [command]
```

### Option 2: Production Deployment
1. Build in Release mode:
   ```bash
   dotnet build -c Release
   ```
2. Copy the executable to a convenient location:
   ```bash
   copy "Mordecai.AdminCli\bin\Release\net9.0\Mordecai.AdminCli.exe" "C:\Tools\"
   ```
3. Run from anywhere:
   ```bash
   C:\Tools\Mordecai.AdminCli.exe list-users
   ```

### Option 3: Global Tool (Future)
```bash
# Package as a global tool (requires additional setup)
dotnet pack
dotnet tool install --global --add-source ./bin/Debug Mordecai.AdminCli
mordecai-admin list-users
```

## Integration with Web Admin

The CLI tool works seamlessly with the web admin interface:

- ? **Shared database** - CLI and web use the same data
- ? **Real-time sync** - changes appear immediately in both interfaces
- ? **Complementary workflows** - CLI for batch operations, web for detailed editing
- ? **User management** - grant admin access via CLI, manage content via web
- ? **Password management** - reset passwords via CLI, users can change them via web
- ? **Zone creation** - create zones via CLI, add rooms via web interface

## Common Administrative Workflows

### New User Setup
```bash
# 1. Check if user exists
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-users

# 2. If user registered, set their initial password
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe set-password --user newuser@example.com --generate --show --force

# 3. Grant admin role if needed
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe make-admin --user newuser@example.com --force
```

### Password Recovery
```bash
# Generate new password for user who forgot theirs
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe set-password --user user@example.com --generate --show --force

# Or set a temporary password they can change
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe set-password --user user@example.com --password "TempPassword123!" --force
```

### Bulk User Management
```bash
# List all users to see status
.\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe list-users --detailed

# Reset multiple passwords (scripting example)
# Note: Use with caution in production
for user in user1@example.com user2@example.com user3@example.com; do
    .\Mordecai.AdminCli\bin\Debug\net9.0\Mordecai.AdminCli.exe set-password --user "$user" --generate --show --force
done
```

## Troubleshooting

### Database Issues
- ? **Automatic resolution** - the tool finds your database automatically
- ? **Clear error messages** - helpful diagnostics if database issues occur
- ? **Fallback behavior** - creates new database if original not found

### Permission Issues
- Ensure you have read/write access to the directory containing `mordecai.db`
- Run as administrator if you get permission errors
- Check that the database file isn't locked by another process

### Command Issues
- Use `--help` to see all available commands and options
- Check that you're using the correct syntax for arguments
- Use `--force` to skip confirmation prompts in scripts

### Path Issues (Resolved)
- ? **No longer an issue** - the tool automatically finds the database
- ? **Works from any directory** - no need to worry about working directory
- ? **Robust path handling** - handles various deployment scenarios

## Security Notes

- The CLI tool has full admin access to the database
- Use `--force` flags carefully to avoid accidental operations  
- User management commands require exact email/username matches
- Zone names must be unique across the system
- All operations are logged for audit purposes
- **Password Management Security**:
  - Generated passwords meet strong security requirements
  - Use `--show` flag only when necessary and in secure environments
  - Interactive password entry is hidden from console output
  - Password validation enforces ASP.NET Core Identity policies
  - Consider secure password sharing methods (encrypted communication, password managers)