using System;
using System.Collections.Generic;
using System.Linq;

namespace ASO_RTR.Desktop.Models;

/// <summary>Estados de un despacho. Se persiste como ORDINAL: miembros nuevos al final.</summary>
public enum EstadoDespacho
{
    Registrada,
    Anulada
}

/// <summary>
/// Documento de despacho de producto terminado: las paletas ya procesadas (control de calidad,
/// limpieza, empacado, etiquetado) que vuelven a Dusa.
///
/// No tiene documento aguas abajo — a diferencia de <see cref="RecepcionMateriaPrima"/>, un
/// despacho no genera nada en Finanzas: es maquila, y el destino siempre es Dusa, así que no
/// lleva campo de destino. Anularlo solo devuelve la custodia, porque el kardex no cuenta lo
/// anulado.
///
/// Es la raíz de un agregado (ver <see cref="Lineas"/>).
/// </summary>
public class DespachoProductoTerminado : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Organizacion duenna de la fila; lo estampa AsoRtrDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    /// <summary>Correlativo interno, "DES-000123". Lo asigna
    /// <c>DespachosService</c> al registrar, no el formulario.</summary>
    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string Observaciones { get; set; } = string.Empty;

    /// <summary>Lo que se despachó, tipo de botella por tipo de botella.</summary>
    public List<DespachoProductoTerminadoLinea> Lineas { get; set; } = [];

    public EstadoDespacho Estado { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    /// <summary>Quien tenía la sesión abierta al emitir el despacho. No es un campo del
    /// formulario: lo pone el servicio a partir de la sesión.</summary>
    public int AutorizadoPorId { get; set; }
    public string AutorizadoPorNombre { get; set; } = string.Empty;  // snapshot

    public int CreadoPorId { get; set; }
    public DateTime FechaCreacion { get; set; }

    /// <summary>
    /// PROVISIONAL: placeholder para dejar constancia de que el servicio de maquila ya se
    /// facturó a Dusa por fuera de la aplicación. No hay Cuentas por Cobrar en este scaffold (ver
    /// CLAUDE.md) — estos dos campos no generan ni validan nada, son solo una nota manual hasta
    /// que se construya un módulo de facturación real.
    /// </summary>
    public string? FacturaDusaReferencia { get; set; }
    public DateTime? FacturaDusaFecha { get; set; }

    /// <summary>Si este despacho resta de la custodia. Un despacho anulado deja de contar, que
    /// es lo que hace que anular devuelva la custodia sin tocar ninguna otra fila.</summary>
    public bool CuentaEnCustodia => Estado == EstadoDespacho.Registrada;

    public string EstadoTexto => Estado == EstadoDespacho.Registrada ? "Registrada" : "Anulada";

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy");

    public int CantidadLineas => Lineas.Count;

    public int TotalPaletas => Lineas.Sum(l => l.CantidadPaletas);

    /// <summary>
    /// Copia con las líneas duplicadas de verdad: <c>MemberwiseClone</c> compartiría la misma
    /// lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public DespachoProductoTerminado Clonar()
    {
        var copia = (DespachoProductoTerminado)MemberwiseClone();
        copia.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return copia;
    }
}

/// <summary>
/// Renglón de un despacho. Mismo criterio que <see cref="RecepcionMateriaPrimaLinea"/>: el
/// <c>TipoBotellaId</c> para la custodia, y la etiqueta y las botellas por paleta congeladas
/// como snapshot.
/// </summary>
public class DespachoProductoTerminadoLinea
{
    public int TipoBotellaId { get; set; }
    public string TipoBotellaEtiqueta { get; set; } = string.Empty;  // snapshot

    public int CantidadPaletas { get; set; }

    /// <summary>Snapshot de <see cref="TipoBotella.BotellasPorPaleta"/> al momento de despachar.</summary>
    public int BotellasPorPaletaSnapshot { get; set; }

    public int TotalBotellas => CantidadPaletas * BotellasPorPaletaSnapshot;

    public string CantidadPaletasTexto => $"{CantidadPaletas} paletas";

    public DespachoProductoTerminadoLinea Clonar() => (DespachoProductoTerminadoLinea)MemberwiseClone();
}
