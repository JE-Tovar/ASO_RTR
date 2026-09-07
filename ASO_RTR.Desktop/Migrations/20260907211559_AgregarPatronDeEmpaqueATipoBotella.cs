using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO_RTR.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPatronDeEmpaqueATipoBotella : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColumnasPorNivel",
                table: "TiposBotella",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FilasPorNivel",
                table: "TiposBotella",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CeldaPatronEmpaque",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Patron = table.Column<int>(type: "int", nullable: false),
                    Fila = table.Column<int>(type: "int", nullable: false),
                    Columna = table.Column<int>(type: "int", nullable: false),
                    Orientacion = table.Column<int>(type: "int", nullable: false),
                    TipoBotellaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CeldaPatronEmpaque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CeldaPatronEmpaque_TiposBotella_TipoBotellaId",
                        column: x => x.TipoBotellaId,
                        principalTable: "TiposBotella",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CeldaPatronEmpaque_TipoBotellaId",
                table: "CeldaPatronEmpaque",
                column: "TipoBotellaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CeldaPatronEmpaque");

            migrationBuilder.DropColumn(
                name: "ColumnasPorNivel",
                table: "TiposBotella");

            migrationBuilder.DropColumn(
                name: "FilasPorNivel",
                table: "TiposBotella");
        }
    }
}
