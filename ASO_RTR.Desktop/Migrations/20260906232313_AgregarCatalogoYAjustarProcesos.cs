using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO_RTR.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCatalogoYAjustarProcesos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoBotella",
                table: "Procesos");

            migrationBuilder.RenameColumn(
                name: "CantidadBotellas",
                table: "Procesos",
                newName: "TipoBotellaId");

            migrationBuilder.AddColumn<string>(
                name: "TipoBotellaEtiqueta",
                table: "Procesos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CantidadProcesada",
                table: "Etapas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TiposBotella",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Medida = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Presentacion = table.Column<int>(type: "int", nullable: false),
                    BotellasPorCaja = table.Column<int>(type: "int", nullable: false),
                    UnidadesPorNivel = table.Column<int>(type: "int", nullable: false),
                    Niveles = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposBotella", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TiposBotella");

            migrationBuilder.DropColumn(
                name: "TipoBotellaEtiqueta",
                table: "Procesos");

            migrationBuilder.DropColumn(
                name: "CantidadProcesada",
                table: "Etapas");

            migrationBuilder.RenameColumn(
                name: "TipoBotellaId",
                table: "Procesos",
                newName: "CantidadBotellas");

            migrationBuilder.AddColumn<string>(
                name: "TipoBotella",
                table: "Procesos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
