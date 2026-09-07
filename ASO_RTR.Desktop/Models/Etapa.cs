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

    /// <summary>Suma de la merma de todos los empleados. Null hasta que se completa.</summary>
    public int? MermaTotal { get; set; }

    /// <summary>Número del boleto de salida de Inventario que generó esta merma. Vacío si no hubo.</summary>
    public string MermaSalidaNumero { get; set; } = string.Empty;

    /// <summary>Número del boleto de salida por el consumo automático de las procesadas. Vacío si no aplica.</summary>
    public string ConsumoSalidaNumero { get; set; } = string.Empty;

    public string Notas { get; set; } = string.Empty;

    /// <summary>Empleados involucrados en esta etapa — snapshot de texto, no catálogo referenciado.</summary>
    public List<EtapaEmpleado> Empleados { get; set; } = [];

    /// <summary>
    /// Qué artículos consume esta etapa por cada botella procesada. Se configura al crear/editar
    /// la etapa, no al completarla: es una propiedad del tipo de trabajo (Etiquetado gasta
    /// etiquetas), no algo que se vuelva a preguntar cada vez.
    /// </summary>
    public List<ConsumoEtapa> Consumos { get; set; } = [];

    /// <summary>
    /// Accidentes reportados al completar: de 0 a N líneas, cualquier combinación de empleado y
    /// artículo dañado — no una sola por empleado.
    /// </summary>
    public List<MermaEtapa> Mermas { get; set; } = [];

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
        copia.Consumos = Consumos.Select(c => c.Clonar()).ToList();
        copia.Mermas = Mermas.Select(m => m.Clonar()).ToList();
        return copia;
    }
}

/// <summary>Empleado involucrado en una etapa — snapshot de texto, no referencia ningún catálogo.</summary>
public class EtapaEmpleado
{
    public int EmpleadoId { get; set; }
    public string EmpleadoNombre { get; set; } = string.Empty;

    /// <summary>Botellas que hizo este empleado. En 0 hasta que se completa la etapa.</summary>
    public int Cantidad { get; set; }

    public EtapaEmpleado Clonar() => (EtapaEmpleado)MemberwiseClone();
}

/// <summary>
/// Un artículo que esta etapa consume por cada botella procesada. Se configura al crear o editar
/// la etapa — es una propiedad del tipo de trabajo, no algo que se repita cada vez que se completa.
/// </summary>
public class ConsumoEtapa
{
    public int ArticuloId { get; set; }
    public string ArticuloCodigo { get; set; } = string.Empty;  // snapshot
    public string ArticuloNombre { get; set; } = string.Empty;  // snapshot
    public string UnidadTexto { get; set; } = string.Empty;     // snapshot

    /// <summary>Proporción configurable: no siempre es 1 a 1 (ej. 1 caja cada 12 botellas).</summary>
    public decimal UnidadesPorBotella { get; set; }

    public ConsumoEtapa Clonar() => (ConsumoEtapa)MemberwiseClone();
}

/// <summary>
/// Una línea de merma: quién la generó, qué artículo se dañó y cuánto. Libre en cantidad por
/// empleado — de 0 a N líneas por persona, capturadas al completar la etapa.
/// </summary>
public class MermaEtapa
{
    public int EmpleadoId { get; set; }
    public string EmpleadoNombre { get; set; } = string.Empty;  // snapshot
    public int ArticuloId { get; set; }
    public string ArticuloNombre { get; set; } = string.Empty;  // snapshot
    public int Cantidad { get; set; }

    public MermaEtapa Clonar() => (MermaEtapa)MemberwiseClone();
}
