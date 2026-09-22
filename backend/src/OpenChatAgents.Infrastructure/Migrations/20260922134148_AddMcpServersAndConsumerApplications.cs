using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenChatAgents.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpServersAndConsumerApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consumer_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientSecretHash = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumer_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_consumer_applications_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "mcp_servers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AuthType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AuthHeaderName = table.Column<string>(type: "text", nullable: true),
                    Secret = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_servers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mcp_servers_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "agent_mcp_servers",
                columns: table => new
                {
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    McpServerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_mcp_servers", x => new { x.AgentId, x.McpServerId });
                    table.ForeignKey(
                        name: "FK_agent_mcp_servers_agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agent_mcp_servers_mcp_servers_McpServerId",
                        column: x => x.McpServerId,
                        principalTable: "mcp_servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_mcp_servers_McpServerId",
                table: "agent_mcp_servers",
                column: "McpServerId");

            migrationBuilder.CreateIndex(
                name: "IX_consumer_applications_ClientId",
                table: "consumer_applications",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consumer_applications_CreatedByUserId",
                table: "consumer_applications",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_mcp_servers_CreatedByUserId",
                table: "mcp_servers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_mcp_servers_Name",
                table: "mcp_servers",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_mcp_servers");

            migrationBuilder.DropTable(
                name: "consumer_applications");

            migrationBuilder.DropTable(
                name: "mcp_servers");
        }
    }
}
