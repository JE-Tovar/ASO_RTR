using System;

namespace ASO_RTR.Desktop.Models;

/// <summary>
/// Empleado de la planta: personal de control de calidad, limpieza, empacado y etiquetado.
/// Dato maestro; cruza cualquier periodo, no lleva referencia a temporada ni turno.
/// </summary>
public class Empleado : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Organización dueña de la fila; lo estampa AsoRtrDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }
    public string Cedula { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; } = DateTime.Today;
    public string Telefono { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public string EstadoTexto => Activo ? "Activo" : "Inactivo";

    public string Etiqueta => string.IsNullOrWhiteSpace(Cedula) ? NombreCompleto : $"{NombreCompleto} · {Cedula}";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public Empleado Clonar() => (Empleado)MemberwiseClone();
}
