using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO_RTR.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Inventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Articulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Unidad = table.Column<int>(type: "int", nullable: false),
                    Minimo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articulos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntradasInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: true),
                    ProveedorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompradoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FacturaProveedorId = table.Column<int>(type: "int", nullable: true),
                    FacturaProveedorNumero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    CreadoPorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntradasInventario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalidasInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Destino = table.Column<int>(type: "int", nullable: false),
                    DestinoDetalle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Motivo = table.Column<int>(type: "int", nullable: false),
                    RetiradoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AutorizadoPorId = table.Column<int>(type: "int", nullable: false),
                    AutorizadoPorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalidasInventario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntradaInventarioLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticuloId = table.Column<int>(type: "int", nullable: false),
                    ArticuloCodigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ArticuloNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UnidadTexto = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EntradaInventarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntradaInventarioLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntradaInventarioLinea_EntradasInventario_EntradaInventarioId",
                        column: x => x.EntradaInventarioId,
                        principalTable: "EntradasInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalidaInventarioLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticuloId = table.Column<int>(type: "int", nullable: false),
                    ArticuloCodigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ArticuloNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UnidadTexto = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalidaInventarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalidaInventarioLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalidaInventarioLinea_SalidasInventario_SalidaInventarioId",
                        column: x => x.SalidaInventarioId,
                        principalTable: "SalidasInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_OrganizacionId_Codigo",
                table: "Articulos",
                columns: new[] { "OrganizacionId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntradaInventarioLinea_EntradaInventarioId",
                table: "EntradaInventarioLinea",
                column: "EntradaInventarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EntradasInventario_FacturaProveedorId",
                table: "EntradasInventario",
                column: "FacturaProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_EntradasInventario_OrganizacionId_Numero",
                table: "EntradasInventario",
                columns: new[] { "OrganizacionId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalidaInventarioLinea_SalidaInventarioId",
                table: "SalidaInventarioLinea",
                column: "SalidaInventarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SalidasInventario_OrganizacionId_Numero",
                table: "SalidasInventario",
                columns: new[] { "OrganizacionId", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Articulos");

            migrationBuilder.DropTable(
                name: "EntradaInventarioLinea");

            migrationBuilder.DropTable(
                name: "SalidaInventarioLinea");

            migrationBuilder.DropTable(
                name: "EntradasInventario");

            migrationBuilder.DropTable(
                name: "SalidasInventario");
        }
    }
}
