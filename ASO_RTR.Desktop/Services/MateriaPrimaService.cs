using System.Collections.Generic;
using System.Linq;
using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.Services;

/// <summary>
/// Custodia de materia prima: cuántas paletas de cada tipo de botella tiene RTR en planta,
/// propiedad de Dusa.
///
/// Mismo criterio que <see cref="InventarioService"/>: la custodia no se guarda, se calcula.
/// Lo que llegó (<see cref="RecepcionMateriaPrima"/>) menos lo que se despachó
/// (<see cref="DespachoProductoTerminado"/>), sin contar documentos anulados.
/// </summary>
public sealed class MateriaPrimaService
{
    private readonly IRecepcionMateriaPrimaDataSource _recepciones;
    private readonly IDespachoProductoTerminadoDataSource _despachos;

    public MateriaPrimaService(IRecepcionMateriaPrimaDataSource recepciones,
                               IDespachoProductoTerminadoDataSource despachos)
    {
        _recepciones = recepciones;
        _despachos = despachos;
    }

    /// <summary>
    /// Paletas en custodia de cada tipo de botella que se haya movido alguna vez: lo que entró
    /// menos lo que salió, sin contar documentos anulados.
    ///
    /// Recorre las dos tablas UNA vez cada una y suma en memoria sobre un diccionario, igual que
    /// <see cref="InventarioService.ExistenciasPorArticulo"/>.
    /// </summary>
    public IReadOnlyDictionary<int, int> ExistenciasPorTipoBotella()
    {
        var saldos = new Dictionary<int, int>();

        foreach (var recepcion in _recepciones.GetAll().Where(r => r.CuentaEnCustodia))
            foreach (var linea in recepcion.Lineas)
                saldos[linea.TipoBotellaId] = saldos.GetValueOrDefault(linea.TipoBotellaId) + linea.CantidadPaletas;

        foreach (var despacho in _despachos.GetAll().Where(d => d.CuentaEnCustodia))
            foreach (var linea in despacho.Lineas)
                saldos[linea.TipoBotellaId] = saldos.GetValueOrDefault(linea.TipoBotellaId) - linea.CantidadPaletas;

        return saldos;
    }

    public int ExistenciaPaletas(int tipoBotellaId) => ExistenciasPorTipoBotella().GetValueOrDefault(tipoBotellaId);

    // --- Resúmenes para el panel del módulo ---

    public int TotalPaletasEnCustodia() => ExistenciasPorTipoBotella().Values.Sum();
}
