using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenChatAgents.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillsSubAgentsBuiltInToolsAndChatImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageObjectKey",
                table: "messages",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "mcp_servers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<string>(
                name: "BuiltInKey",
                table: "mcp_servers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "mcp_servers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "mcp_servers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "agent_sub_agents",
                columns: table => new
                {
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubAgentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_sub_agents", x => new { x.AgentId, x.SubAgentId });
                    table.ForeignKey(
                        name: "FK_agent_sub_agents_agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agent_sub_agents_agents_SubAgentId",
                        column: x => x.SubAgentId,
                        principalTable: "agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skills_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "agent_skills",
                columns: table => new
                {
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_skills", x => new { x.AgentId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_agent_skills_agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agent_skills_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_skills_SkillId",
                table: "agent_skills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_sub_agents_SubAgentId",
                table: "agent_sub_agents",
                column: "SubAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_skills_CreatedByUserId",
                table: "skills",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_skills_Name",
                table: "skills",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_skills");

            migrationBuilder.DropTable(
                name: "agent_sub_agents");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "ImageObjectKey",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "BuiltInKey",
                table: "mcp_servers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "mcp_servers");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "mcp_servers");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "mcp_servers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
