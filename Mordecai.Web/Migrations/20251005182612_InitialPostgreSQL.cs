using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameConfigurations",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameConfigurations", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "RoomEffectDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EffectType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IconName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EffectColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    DetectionSkillId = table.Column<int>(type: "integer", nullable: true),
                    DetectionDifficulty = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DefaultDuration = table.Column<int>(type: "integer", nullable: false),
                    DefaultIntensity = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    IsStackable = table.Column<bool>(type: "boolean", nullable: false),
                    MaxStacks = table.Column<int>(type: "integer", nullable: false),
                    TickInterval = table.Column<int>(type: "integer", nullable: false),
                    RemovalMethods = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEffectDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AllowsCombat = table.Column<bool>(type: "boolean", nullable: false),
                    AllowsLogout = table.Column<bool>(type: "boolean", nullable: false),
                    HasSpecialCommands = table.Column<bool>(type: "boolean", nullable: false),
                    HealingRate = table.Column<decimal>(type: "numeric", nullable: false),
                    SkillLearningBonus = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxOccupancy = table.Column<int>(type: "integer", nullable: false),
                    IsIndoor = table.Column<bool>(type: "boolean", nullable: false),
                    EntryMessage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExitMessage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DefaultBaseCost = table.Column<int>(type: "integer", nullable: false),
                    DefaultMultiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    AllowsPassiveAdvancement = table.Column<bool>(type: "boolean", nullable: false),
                    AllowsTeaching = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DefaultBaseCost = table.Column<int>(type: "integer", nullable: false),
                    DefaultMultiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    AllowsPassiveAdvancement = table.Column<bool>(type: "boolean", nullable: false),
                    AllowsTeaching = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DifficultyLevel = table.Column<int>(type: "integer", nullable: false),
                    IsOutdoor = table.Column<bool>(type: "boolean", nullable: false),
                    WeatherType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomEffectImpacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomEffectDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    ImpactType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TargetSkillId = table.Column<int>(type: "integer", nullable: true),
                    TargetAttribute = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ImpactValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ImpactFormula = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPercentage = table.Column<bool>(type: "boolean", nullable: false),
                    DamageType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ResistanceSkillId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEffectImpacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomEffectImpacts_RoomEffectDefinitions_RoomEffectDefinitio~",
                        column: x => x.RoomEffectDefinitionId,
                        principalTable: "RoomEffectDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SkillType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseCost = table.Column<int>(type: "integer", nullable: false),
                    Multiplier = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    RelatedAttribute = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MagicSchool = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ManaCost = table.Column<int>(type: "integer", nullable: true),
                    CooldownSeconds = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    AllowsPassiveAdvancement = table.Column<bool>(type: "boolean", nullable: false),
                    AllowsTeaching = table.Column<bool>(type: "boolean", nullable: false),
                    UsesExplodingDice = table.Column<bool>(type: "boolean", nullable: false),
                    MaxPracticalLevel = table.Column<int>(type: "integer", nullable: false),
                    IsStartingSkill = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CustomProperties = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillDefinitions_SkillCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SkillDefinition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SkillType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseCost = table.Column<int>(type: "integer", nullable: false),
                    Multiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    RelatedAttribute = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MagicSchool = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ManaCost = table.Column<int>(type: "integer", nullable: true),
                    CooldownSeconds = table.Column<decimal>(type: "numeric", nullable: false),
                    AllowsPassiveAdvancement = table.Column<bool>(type: "boolean", nullable: false),
                    AllowsTeaching = table.Column<bool>(type: "boolean", nullable: false),
                    UsesExplodingDice = table.Column<bool>(type: "boolean", nullable: false),
                    MaxPracticalLevel = table.Column<int>(type: "integer", nullable: false),
                    IsStartingSkill = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CustomProperties = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillDefinition_SkillCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SkillCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ZoneId = table.Column<int>(type: "integer", nullable: false),
                    RoomTypeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    NightDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EntryDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NightEntryDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExitDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NightExitDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Z = table.Column<int>(type: "integer", nullable: false),
                    OverrideDayNightDescriptions = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CustomProperties = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rooms_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    LearnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSkills_SkillDefinitions_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillUsageLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    UsageType = table.Column<int>(type: "integer", nullable: false),
                    BaseUsagePoints = table.Column<int>(type: "integer", nullable: false),
                    UsageMultiplier = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    FinalUsagePoints = table.Column<int>(type: "integer", nullable: false),
                    SkillLevelBefore = table.Column<int>(type: "integer", nullable: false),
                    SkillLevelAfter = table.Column<int>(type: "integer", nullable: false),
                    DidAdvance = table.Column<bool>(type: "boolean", nullable: false),
                    Context = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillUsageLogs_SkillDefinitions_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Species = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastPlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Physicality = table.Column<int>(type: "integer", nullable: false),
                    Dodge = table.Column<int>(type: "integer", nullable: false),
                    Drive = table.Column<int>(type: "integer", nullable: false),
                    Reasoning = table.Column<int>(type: "integer", nullable: false),
                    Awareness = table.Column<int>(type: "integer", nullable: false),
                    Focus = table.Column<int>(type: "integer", nullable: false),
                    Bearing = table.Column<int>(type: "integer", nullable: false),
                    CurrentFatigue = table.Column<int>(type: "integer", nullable: false),
                    CurrentVitality = table.Column<int>(type: "integer", nullable: false),
                    PendingFatigueDamage = table.Column<int>(type: "integer", nullable: false),
                    PendingVitalityDamage = table.Column<int>(type: "integer", nullable: false),
                    CurrentRoomId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Rooms_CurrentRoomId",
                        column: x => x.CurrentRoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RoomEffects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    RoomEffectDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceId = table.Column<string>(type: "text", nullable: true),
                    SourceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CasterCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    StackCount = table.Column<int>(type: "integer", nullable: false),
                    Intensity = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTickTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CustomData = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomEffects_RoomEffectDefinitions_RoomEffectDefinitionId",
                        column: x => x.RoomEffectDefinitionId,
                        principalTable: "RoomEffectDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomEffects_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomExits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromRoomId = table.Column<int>(type: "integer", nullable: false),
                    ToRoomId = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExitDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NightExitDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    SkillRequired = table.Column<int>(type: "integer", nullable: true),
                    SkillLevelRequired = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomExits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomExits_Rooms_FromRoomId",
                        column: x => x.FromRoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomExits_Rooms_ToRoomId",
                        column: x => x.ToRoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSkill",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    TotalUsagePoints = table.Column<int>(type: "integer", nullable: false),
                    CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                    LearnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAdvancedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CanTeach = table.Column<bool>(type: "boolean", nullable: false),
                    CustomProperties = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSkill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSkill_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterSkill_SkillDefinition_SkillDefinitionId",
                        column: x => x.SkillDefinitionId,
                        principalTable: "SkillDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomEffectApplicationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomEffectId = table.Column<int>(type: "integer", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ImpactType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImpactValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ResistanceRoll = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    ResistanceSuccess = table.Column<bool>(type: "boolean", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEffectApplicationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomEffectApplicationLogs_RoomEffects_RoomEffectId",
                        column: x => x.RoomEffectId,
                        principalTable: "RoomEffects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CurrentRoomId",
                table: "Characters",
                column: "CurrentRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_Name",
                table: "Characters",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkill_CharacterId",
                table: "CharacterSkill",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkill_SkillDefinitionId",
                table: "CharacterSkill",
                column: "SkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_CharacterId",
                table: "CharacterSkills",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_CharacterId_SkillDefinitionId",
                table: "CharacterSkills",
                columns: new[] { "CharacterId", "SkillDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_LastUsedAt",
                table: "CharacterSkills",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_Level",
                table: "CharacterSkills",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_SkillDefinitionId",
                table: "CharacterSkills",
                column: "SkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_ApplicationType",
                table: "RoomEffectApplicationLogs",
                column: "ApplicationType");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_CharacterId",
                table: "RoomEffectApplicationLogs",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_RoomEffectId",
                table: "RoomEffectApplicationLogs",
                column: "RoomEffectId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectApplicationLogs_Timestamp",
                table: "RoomEffectApplicationLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_Category",
                table: "RoomEffectDefinitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_EffectType",
                table: "RoomEffectDefinitions",
                column: "EffectType");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_IsActive",
                table: "RoomEffectDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectDefinitions_Name",
                table: "RoomEffectDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectImpacts_ImpactType",
                table: "RoomEffectImpacts",
                column: "ImpactType");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectImpacts_RoomEffectDefinitionId",
                table: "RoomEffectImpacts",
                column: "RoomEffectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffectImpacts_TargetType",
                table: "RoomEffectImpacts",
                column: "TargetType");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_CasterCharacterId",
                table: "RoomEffects",
                column: "CasterCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_EndTime",
                table: "RoomEffects",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_IsActive",
                table: "RoomEffects",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_RoomEffectDefinitionId",
                table: "RoomEffects",
                column: "RoomEffectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEffects_RoomId",
                table: "RoomEffects",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomExits_FromRoomId_Direction",
                table: "RoomExits",
                columns: new[] { "FromRoomId", "Direction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomExits_IsActive",
                table: "RoomExits",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoomExits_ToRoomId",
                table: "RoomExits",
                column: "ToRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_IsActive",
                table: "Rooms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomTypeId",
                table: "Rooms",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_X_Y_Z",
                table: "Rooms",
                columns: new[] { "X", "Y", "Z" });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_ZoneId",
                table: "Rooms",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_IsActive",
                table: "RoomTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_Name",
                table: "RoomTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_DisplayOrder",
                table: "SkillCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_IsActive",
                table: "SkillCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_Name",
                table: "SkillCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinition_CategoryId",
                table: "SkillDefinition",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_CategoryId_SkillType",
                table: "SkillDefinitions",
                columns: new[] { "CategoryId", "SkillType" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_IsActive",
                table: "SkillDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_IsStartingSkill",
                table: "SkillDefinitions",
                column: "IsStartingSkill");

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_MagicSchool",
                table: "SkillDefinitions",
                column: "MagicSchool");

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefinitions_Name",
                table: "SkillDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageLogs_CharacterId",
                table: "SkillUsageLogs",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageLogs_CharacterId_UsedAt",
                table: "SkillUsageLogs",
                columns: new[] { "CharacterId", "UsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageLogs_SkillDefinitionId",
                table: "SkillUsageLogs",
                column: "SkillDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillUsageLogs_UsedAt",
                table: "SkillUsageLogs",
                column: "UsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Zones_IsActive",
                table: "Zones",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Zones_Name",
                table: "Zones",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CharacterSkill");

            migrationBuilder.DropTable(
                name: "CharacterSkills");

            migrationBuilder.DropTable(
                name: "GameConfigurations");

            migrationBuilder.DropTable(
                name: "RoomEffectApplicationLogs");

            migrationBuilder.DropTable(
                name: "RoomEffectImpacts");

            migrationBuilder.DropTable(
                name: "RoomExits");

            migrationBuilder.DropTable(
                name: "SkillUsageLogs");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "SkillDefinition");

            migrationBuilder.DropTable(
                name: "RoomEffects");

            migrationBuilder.DropTable(
                name: "SkillDefinitions");

            migrationBuilder.DropTable(
                name: "SkillCategory");

            migrationBuilder.DropTable(
                name: "RoomEffectDefinitions");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "SkillCategories");

            migrationBuilder.DropTable(
                name: "RoomTypes");

            migrationBuilder.DropTable(
                name: "Zones");
        }
    }
}
