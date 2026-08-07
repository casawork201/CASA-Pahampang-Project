using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CASAPahampang.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchScheduleToMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatchSchedule",
                table: "Match",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchSchedule",
                table: "Match");
        }
    }
}
