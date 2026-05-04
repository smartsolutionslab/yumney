using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "AppUserProfiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "system");

            migrationBuilder.AddColumn<bool>(
                name: "TimerHapticFeedback",
                table: "AppUserProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TimerSoundAlerts",
                table: "AppUserProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "VoiceAutoReadInCookMode",
                table: "AppUserProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VoiceEnabled",
                table: "AppUserProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "VoiceSpeed",
                table: "AppUserProfiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "normal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Theme",
                table: "AppUserProfiles");

            migrationBuilder.DropColumn(
                name: "TimerHapticFeedback",
                table: "AppUserProfiles");

            migrationBuilder.DropColumn(
                name: "TimerSoundAlerts",
                table: "AppUserProfiles");

            migrationBuilder.DropColumn(
                name: "VoiceAutoReadInCookMode",
                table: "AppUserProfiles");

            migrationBuilder.DropColumn(
                name: "VoiceEnabled",
                table: "AppUserProfiles");

            migrationBuilder.DropColumn(
                name: "VoiceSpeed",
                table: "AppUserProfiles");
        }
    }
}
