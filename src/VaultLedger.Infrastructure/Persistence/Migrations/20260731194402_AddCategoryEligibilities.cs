using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryEligibilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category_eligibilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_eligibilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_category_eligibilities_budget_category_definitions_Category~",
                        column: x => x.CategoryId,
                        principalTable: "budget_category_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_category_eligibilities_users_GrantedByAdminUserId",
                        column: x => x.GrantedByAdminUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_category_eligibilities_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_category_eligibilities_CategoryId",
                table: "category_eligibilities",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_category_eligibilities_GrantedByAdminUserId",
                table: "category_eligibilities",
                column: "GrantedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_category_eligibilities_UserId_CategoryId",
                table: "category_eligibilities",
                columns: new[] { "UserId", "CategoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_eligibilities");
        }
    }
}
