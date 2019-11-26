using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PostCore.MainApp.Migrations.Application
{
    public partial class Mail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Post",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    PersonFrom = table.Column<string>(nullable: true),
                    PersonTo = table.Column<string>(nullable: true),
                    AddressTo = table.Column<string>(nullable: true),
                    BranchId = table.Column<long>(nullable: true),
                    BranchStockAddress = table.Column<string>(nullable: true),
                    CarId = table.Column<long>(nullable: true),
                    SourceBranchId = table.Column<long>(nullable: true),
                    DestinationBranchId = table.Column<long>(nullable: true),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Post", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Post_Branch_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Post_Car_CarId",
                        column: x => x.CarId,
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Post_Branch_DestinationBranchId",
                        column: x => x.DestinationBranchId,
                        principalTable: "Branch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Post_Branch_SourceBranchId",
                        column: x => x.SourceBranchId,
                        principalTable: "Branch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Post_BranchId",
                table: "Post",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Post_CarId",
                table: "Post",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Post_DestinationBranchId",
                table: "Post",
                column: "DestinationBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Post_SourceBranchId",
                table: "Post",
                column: "SourceBranchId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Post");
        }
    }
}
