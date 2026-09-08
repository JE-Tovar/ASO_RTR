using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO_RTR.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMateriaPrima : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DespachosProductoTerminado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutorizadoPorId = table.Column<int>(type: "int", nullable: false),
                    AutorizadoPorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FacturaDusaReferencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FacturaDusaFecha = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DespachosProductoTerminado", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecepcionesMateriaPrima",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumeroOrdenEntrega = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Gandolero = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Placa = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepcionesMateriaPrima", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DespachoProductoTerminadoLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoBotellaId = table.Column<int>(type: "int", nullable: false),
                    TipoBotellaEtiqueta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CantidadPaletas = table.Column<int>(type: "int", nullable: false),
                    BotellasPorPaletaSnapshot = table.Column<int>(type: "int", nullable: false),
                    DespachoProductoTerminadoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DespachoProductoTerminadoLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DespachoProductoTerminadoLinea_DespachosProductoTerminado_DespachoProductoTerminadoId",
                        column: x => x.DespachoProductoTerminadoId,
                        principalTable: "DespachosProductoTerminado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecepcionMateriaPrimaLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoBotellaId = table.Column<int>(type: "int", nullable: false),
                    TipoBotellaEtiqueta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CantidadPaletas = table.Column<int>(type: "int", nullable: false),
                    BotellasPorPaletaSnapshot = table.Column<int>(type: "int", nullable: false),
                    RecepcionMateriaPrimaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepcionMateriaPrimaLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecepcionMateriaPrimaLinea_RecepcionesMateriaPrima_RecepcionMateriaPrimaId",
                        column: x => x.RecepcionMateriaPrimaId,
                        principalTable: "RecepcionesMateriaPrima",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DespachoProductoTerminadoLinea_DespachoProductoTerminadoId",
                table: "DespachoProductoTerminadoLinea",
                column: "DespachoProductoTerminadoId");

            migrationBuilder.CreateIndex(
                name: "IX_DespachosProductoTerminado_OrganizacionId_Numero",
                table: "DespachosProductoTerminado",
                columns: new[] { "OrganizacionId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionesMateriaPrima_OrganizacionId_Numero",
                table: "RecepcionesMateriaPrima",
                columns: new[] { "OrganizacionId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionMateriaPrimaLinea_RecepcionMateriaPrimaId",
                table: "RecepcionMateriaPrimaLinea",
                column: "RecepcionMateriaPrimaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DespachoProductoTerminadoLinea");

            migrationBuilder.DropTable(
                name: "RecepcionMateriaPrimaLinea");

            migrationBuilder.DropTable(
                name: "DespachosProductoTerminado");

            migrationBuilder.DropTable(
                name: "RecepcionesMateriaPrima");
        }
    }
}
