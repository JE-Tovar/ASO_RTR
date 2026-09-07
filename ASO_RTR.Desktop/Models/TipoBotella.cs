using System.Collections.Generic;
using System.Linq;

namespace ASO_RTR.Desktop.Models;

/// <summary>Cómo viene empacada una botella al salir de planta.</summary>
public enum PresentacionEmpaque
{
    Bulk,
    Caja
}

/// <summary>Qué niveles (camadas) de la paleta usan un patrón de empaque: 1,3,5… o 2,4,6…</summary>
public enum PatronNivel
{
    Impar,
    Par
}

/// <summary>Cómo se acuesta una caja dentro de su celda de la paleta.</summary>
public enum OrientacionCaja
{
    Horizontal,
    Vertical
}

/// <summary>
/// Una celda del patrón de paletizado: en qué posición (fila, columna) de qué patrón
/// (nivel impar o par) va una caja, y cómo se orienta.
/// </summary>
public class CeldaPatronEmpaque
{
    public PatronNivel Patron { get; set; }
    public int Fila { get; set; }
    public int Columna { get; set; }
    public OrientacionCaja Orientacion { get; set; }

    public CeldaPatronEmpaque Clonar() => (CeldaPatronEmpaque)MemberwiseClone();
}

/// <summary>
/// Un tipo de botella que maneja la empresa: su medida, su empaque y cómo se arma en paleta.
/// Dato maestro de producto; lo elige cada <see cref="Proceso"/> en vez de escribirlo de nuevo.
/// </summary>
public class TipoBotella : IEntidad<int>, IDeOrganizacion
{
    /// <summary>Organización dueña de la fila; lo estampa AsoRtrDbContext.SaveChanges.</summary>
    public int OrganizacionId { get; set; }

    public int Id { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Medida { get; set; } = string.Empty;
    public PresentacionEmpaque Presentacion { get; set; }

    /// <summary>Solo aplica cuando <see cref="Presentacion"/> es Caja.</summary>
    public int BotellasPorCaja { get; set; }

    /// <summary>Cajas por nivel si la presentación es Caja; botellas por nivel si es Bulk.</summary>
    public int UnidadesPorNivel { get; set; }

    /// <summary>Niveles (camadas) que tiene la paleta.</summary>
    public int Niveles { get; set; }

    /// <summary>Filas y columnas de la matriz de cajas por nivel. Solo aplica si es Caja.</summary>
    public int FilasPorNivel { get; set; }
    public int ColumnasPorNivel { get; set; }

    /// <summary>
    /// Orientación de cada caja, para los dos patrones que se alternan por nivel (para que la
    /// torre trabe y no se desarme en el transporte). Solo aplica si es Caja.
    /// </summary>
    public List<CeldaPatronEmpaque> PatronEmpaque { get; set; } = [];

    public bool Activo { get; set; } = true;

    public string PresentacionTexto => Presentacion == PresentacionEmpaque.Caja ? "Por caja" : "A granel";

    public int BotellasPorNivel => Presentacion == PresentacionEmpaque.Caja
        ? UnidadesPorNivel * BotellasPorCaja
        : UnidadesPorNivel;

    public int BotellasPorPaleta => BotellasPorNivel * Niveles;

    public string EstadoTexto => Activo ? "Activo" : "Inactivo";

    public string Etiqueta => $"{Marca} {Nombre} · {Medida}";

    /// <summary>Las celdas del patrón que le tocan a un nivel dado, según sea impar o par.</summary>
    public IEnumerable<CeldaPatronEmpaque> PatronParaNivel(int nivel) =>
        PatronEmpaque.Where(c => c.Patron == (nivel % 2 == 1 ? PatronNivel.Impar : PatronNivel.Par));

    /// <summary>
    /// Copia con el patrón de empaque duplicado de verdad: <c>MemberwiseClone</c> compartiría la
    /// misma lista entre el original y la copia, y editar la copia mutaría lo que está en pantalla.
    /// </summary>
    public TipoBotella Clonar()
    {
        var copia = (TipoBotella)MemberwiseClone();
        copia.PatronEmpaque = PatronEmpaque.Select(c => c.Clonar()).ToList();
        return copia;
    }
}
