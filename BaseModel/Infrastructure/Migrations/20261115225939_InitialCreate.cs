using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseModel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    text = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "person",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    firstName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    lastName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    schoolClass = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    firstName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    lastName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventoryItem",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    serialNumber = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    itemName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    categoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    isAvailable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventoryItem", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventoryItem_category_categoryId",
                        column: x => x.categoryId,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservation",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    personId = table.Column<int>(type: "INTEGER", nullable: false),
                    inventoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    loanDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    returnDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    acceptedById = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation", x => x.id);
                    table.ForeignKey(
                        name: "FK_reservation_inventoryItem_inventoryItemId",
                        column: x => x.inventoryItemId,
                        principalTable: "inventoryItem",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservation_person_personId",
                        column: x => x.personId,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservation_staff_acceptedById",
                        column: x => x.acceptedById,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventoryItem_categoryId",
                table: "inventoryItem",
                column: "categoryId");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryItem_serialNumber",
                table: "inventoryItem",
                column: "serialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reservation_acceptedById",
                table: "reservation",
                column: "acceptedById");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_inventoryItemId",
                table: "reservation",
                column: "inventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_openLoanPerItem",
                table: "reservation",
                column: "inventoryItemId",
                unique: true,
                filter: "\"returnDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_personId",
                table: "reservation",
                column: "personId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservation");

            migrationBuilder.DropTable(
                name: "inventoryItem");

            migrationBuilder.DropTable(
                name: "person");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "category");
        }
    }
}
