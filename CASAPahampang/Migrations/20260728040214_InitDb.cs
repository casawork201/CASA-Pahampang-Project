using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CASAPahampang.Migrations
{
    /// <inheritdoc />
    public partial class InitDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvatarOption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarOption", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUser = table.Column<bool>(type: "boolean", nullable: false),
                    AvatarUrl = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Team",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Logo = table.Column<byte[]>(type: "bytea", nullable: false),
                    Record = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Match",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameNumber = table.Column<int>(type: "integer", nullable: false),
                    Venue = table.Column<string>(type: "text", nullable: false),
                    Team1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Team1Score = table.Column<int>(type: "integer", nullable: false),
                    Team2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Team2Score = table.Column<int>(type: "integer", nullable: false),
                    IsLive = table.Column<bool>(type: "boolean", nullable: false),
                    StatusText = table.Column<string>(type: "text", nullable: false),
                    PeriodDetails = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Match", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Match_Team_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Match_Team_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Match_Team1Id",
                table: "Match",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Match_Team2Id",
                table: "Match",
                column: "Team2Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvatarOption");

            migrationBuilder.DropTable(
                name: "ChatMessage");

            migrationBuilder.DropTable(
                name: "Match");

            migrationBuilder.DropTable(
                name: "Team");
        }
    }
}
