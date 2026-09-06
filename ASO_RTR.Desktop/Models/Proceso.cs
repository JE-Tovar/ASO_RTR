using System;

namespace ASO_RTR.Desktop.Models;

/// <summary>
/// Un proceso en planta: la unidad de trabajo sobre un tipo de botella. No es un pipeline fijo
/// — las etapas que le corresponden se agregan una a una (ver <see cref="Etapa"/>), según lo que
/// pida el cliente para ese proceso.
/// </summary>
public class Proceso : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Organización dueña de la fila; lo estampa AsoRtrDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    public int TipoBotellaId { get; set; }
    public string TipoBotellaEtiqueta { get; set; } = string.Empty; // snapshot

    public DateTime FechaCreacion { get; set; } = DateTime.Today;
    public string Notas { get; set; } = string.Empty;

    // Sin CantidadBotellas: la cantidad la define cada etapa al completarse
    // (ver Etapa.CantidadProcesada), no el proceso al crearse.

    public string Etiqueta => $"Proceso Nº{Id} · {TipoBotellaEtiqueta}";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public Proceso Clonar() => (Proceso)MemberwiseClone();
}
