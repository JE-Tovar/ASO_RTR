namespace ASO_RTR.Desktop.Models;

/// <summary>Cómo viene empacada una botella al salir de planta.</summary>
public enum PresentacionEmpaque
{
    Bulk,
    Caja
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

    public bool Activo { get; set; } = true;

    public string PresentacionTexto => Presentacion == PresentacionEmpaque.Caja ? "Por caja" : "A granel";

    public int BotellasPorNivel => Presentacion == PresentacionEmpaque.Caja
        ? UnidadesPorNivel * BotellasPorCaja
        : UnidadesPorNivel;

    public int BotellasPorPaleta => BotellasPorNivel * Niveles;

    public string EstadoTexto => Activo ? "Activo" : "Inactivo";

    public string Etiqueta => $"{Marca} {Nombre} · {Medida}";

    /// <summary>Copia superficial (solo hay tipos de valor y cadenas) para no mutar el original en la lista.</summary>
    public TipoBotella Clonar() => (TipoBotella)MemberwiseClone();
}
