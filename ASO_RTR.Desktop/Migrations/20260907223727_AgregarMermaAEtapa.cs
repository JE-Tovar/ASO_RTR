using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO_RTR.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMermaAEtapa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MermaSalidaNumero",
                table: "Etapas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MermaTotal",
                table: "Etapas",
                type: "int",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MermaSalidaNumero",
                table: "Etapas");

            migrationBuilder.DropColumn(
                name: "MermaTotal",
                table: "Etapas");

            migrationBuilder.DropColumn(
                name: "ArticuloMermaId",
                table: "EtapaEmpleado");

            migrationBuilder.DropColumn(
                name: "ArticuloMermaNombre",
                table: "EtapaEmpleado");

            migrationBuilder.DropColumn(
                name: "Merma",
                table: "EtapaEmpleado");
        }
    }
}
