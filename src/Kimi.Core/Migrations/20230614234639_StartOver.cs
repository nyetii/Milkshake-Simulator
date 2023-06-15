using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kimi.Core.Migrations
{
    /// <inheritdoc />
    public partial class StartOver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Instance",
                columns: table => new
                {
                    ContextId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Vips = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instance", x => x.ContextId);
                });

            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MilkshakeContextId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servers_Instance_MilkshakeContextId",
                        column: x => x.MilkshakeContextId,
                        principalTable: "Instance",
                        principalColumn: "ContextId");
                });

            migrationBuilder.CreateTable(
                name: "Source",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<int>(type: "int", nullable: false),
                    MilkshakeContextId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    Creator = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Source", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Source_Instance_MilkshakeContextId",
                        column: x => x.MilkshakeContextId,
                        principalTable: "Instance",
                        principalColumn: "ContextId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Template",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<int>(type: "int", nullable: false),
                    MilkshakeContextId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    Creator = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Template", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Template_Instance_MilkshakeContextId",
                        column: x => x.MilkshakeContextId,
                        principalTable: "Instance",
                        principalColumn: "ContextId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    X = table.Column<int>(type: "int", nullable: false),
                    Y = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Layer = table.Column<int>(type: "int", nullable: false),
                    Filter = table.Column<int>(type: "int", nullable: false),
                    IsText = table.Column<bool>(type: "bit", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Orientation = table.Column<int>(type: "int", nullable: false),
                    Font = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StrokeColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StrokeWidth = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateProperties_Template_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Template",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Servers_MilkshakeContextId",
                table: "Servers",
                column: "MilkshakeContextId");

            migrationBuilder.CreateIndex(
                name: "IX_Source_MilkshakeContextId",
                table: "Source",
                column: "MilkshakeContextId");

            migrationBuilder.CreateIndex(
                name: "IX_Template_MilkshakeContextId",
                table: "Template",
                column: "MilkshakeContextId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateProperties_TemplateId",
                table: "TemplateProperties",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Servers");

            migrationBuilder.DropTable(
                name: "Source");

            migrationBuilder.DropTable(
                name: "TemplateProperties");

            migrationBuilder.DropTable(
                name: "Template");

            migrationBuilder.DropTable(
                name: "Instance");
        }
    }
}
