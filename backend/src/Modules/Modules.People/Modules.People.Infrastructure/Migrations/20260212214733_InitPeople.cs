using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.People.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitPeople : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttributeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    IsFilterable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidationRulesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IdentificationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonAttributeValues",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValueBool = table.Column<bool>(type: "bit", nullable: true),
                    ValueString = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValueNumber = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonAttributeValues", x => new { x.PersonId, x.AttributeDefinitionId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_IsActive",
                table: "AttributeDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_Key",
                table: "AttributeDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_FullName",
                table: "People",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_People_IdentificationNumber",
                table: "People",
                column: "IdentificationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_IsActive",
                table: "People",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PersonAttributeValues_AttributeDefinitionId_ValueBool",
                table: "PersonAttributeValues",
                columns: new[] { "AttributeDefinitionId", "ValueBool" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonAttributeValues_AttributeDefinitionId_ValueDate",
                table: "PersonAttributeValues",
                columns: new[] { "AttributeDefinitionId", "ValueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonAttributeValues_AttributeDefinitionId_ValueNumber",
                table: "PersonAttributeValues",
                columns: new[] { "AttributeDefinitionId", "ValueNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonAttributeValues_AttributeDefinitionId_ValueString",
                table: "PersonAttributeValues",
                columns: new[] { "AttributeDefinitionId", "ValueString" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttributeDefinitions");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "PersonAttributeValues");
        }
    }
}
