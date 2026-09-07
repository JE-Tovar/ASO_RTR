using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO_RTR.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConsumoYMermaMultipleAEtapa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArticuloMermaId",
                table: "EtapaEmpleado");

            migrationBuilder.DropColumn(
                name: "ArticuloMermaNombre",
                table: "EtapaEmpleado");

            migrationBuilder.DropColumn(
                name: "Merma",
                table: "EtapaEmpleado");

            migrationBuilder.AddColumn<string>(
                name: "ConsumoSalidaNumero",
                table: "Etapas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ConsumoEtapa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticuloId = table.Column<int>(type: "int", nullable: false),
                    ArticuloCodigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ArticuloNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UnidadTexto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnidadesPorBotella = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EtapaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumoEtapa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumoEtapa_Etapas_EtapaId",
                        column: x => x.EtapaId,
                        principalTable: "Etapas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MermaEtapa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ArticuloId = table.Column<int>(type: "int", nullable: false),
                    ArticuloNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    EtapaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MermaEtapa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MermaEtapa_Etapas_EtapaId",
                        column: x => x.EtapaId,
                        principalTable: "Etapas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoEtapa_EtapaId",
                table: "ConsumoEtapa",
                column: "EtapaId");

            migrationBuilder.CreateIndex(
                name: "IX_MermaEtapa_EtapaId",
                table: "MermaEtapa",
                column: "EtapaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumoEtapa");

            migrationBuilder.DropTable(
                name: "MermaEtapa");

            migrationBuilder.DropColumn(
                name: "ConsumoSalidaNumero",
                table: "Etapas");

            migrationBuilder.AddColumn<int>(
                name: "ArticuloMermaId",
                table: "EtapaEmpleado",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArticuloMermaNombre",
                table: "EtapaEmpleado",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Merma",
                table: "EtapaEmpleado",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
