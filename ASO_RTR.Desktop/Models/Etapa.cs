using System;
using System.Collections.Generic;
using System.Linq;

namespace ASO_RTR.Desktop.Models;

/// <summary>Tipos de etapa que puede tener un proceso. Se agregan libremente, no como pipeline fijo.</summary>
public enum TipoEtapa
{
    Recepcion,
    ControlCalidad,
    Limpieza,
    Empacado,
    Etiquetado,
    Despacho
}

/// <summary>
/// Estados de una etapa. "Rechazada" no es terminal: admite reproceso, ver
/// <see cref="Services.EtapaService.Reintentar"/>.
/// </summary>
public enum EstadoEtapa
{
    Pendiente,
    EnProceso,
    Completada,
    Rechazada
}

/// <summary>
/// Una etapa de un proceso (control de calidad, limpieza, empacado, etiquetado…), con su propia
/// máquina de estados y los empleados que la ejecutaron.
/// </summary>
public class Etapa : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Organización dueña de la fila; lo estampa AsoRtrDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public int ProcesoId { get; set; }
    public string ProcesoEtiqueta { get; set; } = string.Empty; // snapshot

    public TipoEtapa Tipo { get; set; }
    public EstadoEtapa Estado { get; set; }

    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? MotivoRechazo { get; set; }

    /// <summary>Botellas que esta etapa procesó con éxito. Null hasta que se completa.</summary>
    public int? CantidadProcesada { get; set; }

    public string Notas { get; set; } = string.Empty;

    /// <summary>Empleados involucrados en esta etapa — snapshot de texto, no catálogo referenciado.</summary>
    public List<EtapaEmpleado> Empleados { get; set; } = [];

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }

    public string TipoTexto => Tipo switch
    {
        TipoEtapa.Recepcion => "Recepción",
        TipoEtapa.ControlCalidad => "Control de calidad",
        TipoEtapa.Limpieza => "Limpieza",
        TipoEtapa.Empacado => "Empacado",
        TipoEtapa.Etiquetado => "Etiquetado",
        _ => "Despacho"
    };

    public string EstadoTexto => Estado switch
    {
        EstadoEtapa.Pendiente => "Pendiente",
        EstadoEtapa.EnProceso => "En proceso",
        EstadoEtapa.Completada => "Completada",
        _ => "Rechazada"
    };

    public string EmpleadosTexto => Empleados.Count == 0
        ? "Sin asignar"
        : string.Join(", ", Empleados.Select(e => e.EmpleadoNombre));

    /// <summary>
    /// Copia con los empleados duplicados de verdad: <c>MemberwiseClone</c> compartiría la misma
    /// lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public Etapa Clonar()
    {
        var copia = (Etapa)MemberwiseClone();
        copia.Empleados = Empleados.Select(e => e.Clonar()).ToList();
        return copia;
    }
}

/// <summary>Empleado involucrado en una etapa — snapshot de texto, no referencia ningún catálogo.</summary>
public class EtapaEmpleado
{
    public int EmpleadoId { get; set; }
    public string EmpleadoNombre { get; set; } = string.Empty;

    public EtapaEmpleado Clonar() => (EtapaEmpleado)MemberwiseClone();
}
