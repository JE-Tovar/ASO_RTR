using System;
using System.Collections.Generic;
using System.Linq;

namespace ASO_RTR.Desktop.Models;

/// <summary>Estados de una recepción. Se persiste como ORDINAL: miembros nuevos al final.</summary>
public enum EstadoRecepcion
{
    Registrada,
    Anulada
}

/// <summary>
/// Documento de recepción de materia prima: las paletas de botellas que trae la gandola de Dusa,
/// según su orden de entrega.
///
/// Es maquila, no compra: Dusa sigue siendo dueña del material, así que registrar una recepción
/// NUNCA genera cuenta por pagar ni factura de proveedor — a diferencia de
/// <see cref="EntradaInventario"/>, no hay ningún documento aguas abajo que deshacer al anular.
///
/// Es la raíz de un agregado (ver <see cref="Lineas"/>).
/// </summary>
public class RecepcionMateriaPrima : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Organizacion duenna de la fila; lo estampa AsoRtrDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }

    /// <summary>Correlativo interno, "REC-000123". Lo asigna
    /// <c>RecepcionesService</c> al registrar, no el formulario.</summary>
    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    /// <summary>Número de la orden de entrega que trae el gandolero. No es el correlativo
    /// interno; es lo que evita cargar dos veces la misma entrega.</summary>
    public string NumeroOrdenEntrega { get; set; } = string.Empty;

    public string Gandolero { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;

    public string Observaciones { get; set; } = string.Empty;

    /// <summary>Lo que llegó, tipo de botella por tipo de botella.</summary>
    public List<RecepcionMateriaPrimaLinea> Lineas { get; set; } = [];

    public EstadoRecepcion Estado { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public int CreadoPorId { get; set; }
    public string CreadoPorNombre { get; set; } = string.Empty;  // snapshot
    public DateTime FechaCreacion { get; set; }

    /// <summary>Si esta recepción suma a la custodia. Una recepción anulada deja de contar, que
    /// es lo que hace que anular devuelva la custodia sin tocar ninguna otra fila.</summary>
    public bool CuentaEnCustodia => Estado == EstadoRecepcion.Registrada;

    public string EstadoTexto => Estado == EstadoRecepcion.Registrada ? "Registrada" : "Anulada";

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy");

    public int CantidadLineas => Lineas.Count;

    public int TotalPaletas => Lineas.Sum(l => l.CantidadPaletas);

    /// <summary>
    /// Copia con las líneas duplicadas de verdad: <c>MemberwiseClone</c> compartiría la misma
    /// lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public RecepcionMateriaPrima Clonar()
    {
        var copia = (RecepcionMateriaPrima)MemberwiseClone();
        copia.Lineas = Lineas.Select(l => l.Clonar()).ToList();
        return copia;
    }
}

/// <summary>
/// Renglón de una recepción. Guarda el <c>TipoBotellaId</c> para poder sumar la custodia, y
/// además la etiqueta y las botellas por paleta como snapshot, para que el documento siga
/// leyéndose igual aunque el tipo de botella cambie de patrón de paletizado después.
/// </summary>
public class RecepcionMateriaPrimaLinea
{
    public int TipoBotellaId { get; set; }
    public string TipoBotellaEtiqueta { get; set; } = string.Empty;  // snapshot

    public int CantidadPaletas { get; set; }

    /// <summary>Snapshot de <see cref="TipoBotella.BotellasPorPaleta"/> al momento de recibir.</summary>
    public int BotellasPorPaletaSnapshot { get; set; }

    public int TotalBotellas => CantidadPaletas * BotellasPorPaletaSnapshot;

    public string CantidadPaletasTexto => $"{CantidadPaletas} paletas";

    public RecepcionMateriaPrimaLinea Clonar() => (RecepcionMateriaPrimaLinea)MemberwiseClone();
}
