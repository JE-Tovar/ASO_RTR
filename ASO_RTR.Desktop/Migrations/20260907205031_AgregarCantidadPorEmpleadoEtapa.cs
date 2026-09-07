using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO_RTR.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCantidadPorEmpleadoEtapa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Cantidad",
                table: "EtapaEmpleado",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "EtapaEmpleado");
        }
    }
}
