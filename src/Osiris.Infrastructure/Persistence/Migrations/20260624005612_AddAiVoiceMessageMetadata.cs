using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Osiris.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiVoiceMessageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "AiMessages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "text");

            migrationBuilder.AddColumn<int>(
                name: "InputAudioMilliseconds",
                table: "AiMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OutputAudioMilliseconds",
                table: "AiMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Channel",
                table: "AiMessages");

            migrationBuilder.DropColumn(
                name: "InputAudioMilliseconds",
                table: "AiMessages");

            migrationBuilder.DropColumn(
                name: "OutputAudioMilliseconds",
                table: "AiMessages");
        }
    }
}
