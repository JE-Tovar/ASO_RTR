using System.Collections.Generic;
using System.Linq;
using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.Services;

/// <summary>
/// Puente de solo lectura entre Operaciones y Materia Prima: cuántas botellas de la custodia de
/// Dusa ya están tomadas por procesos, y cuántas siguen disponibles para arrancar uno nuevo.
///
/// Va aparte de <see cref="MateriaPrimaService"/> a propósito: el sentido de la dependencia en el
/// resto de la app va del módulo más nuevo hacia el más viejo (<see cref="EtapaService"/> ya
/// depende de <see cref="SalidasInventarioService"/> de Inventario, igual que
/// <see cref="EntradasInventarioService"/> depende de <see cref="CuentasPorPagarService"/> de
/// Finanzas). Meterle a MateriaPrimaService el conocimiento de <see cref="Proceso"/>/
/// <see cref="Etapa"/> invertiría esa regla sin necesidad: una recepción no tiene por qué saber
/// que Operaciones existe.
///
/// Sin <see cref="ISesionActual"/>: es puro cálculo derivado, no escribe nada.
/// </summary>
public sealed class CustodiaProduccionService
{
    private readonly IProcesoDataSource _procesos;
    private readonly IEtapaDataSource _etapas;
    private readonly ITipoBotellaDataSource _tiposBotella;
    private readonly MateriaPrimaService _materiaPrima;

    public CustodiaProduccionService(IProcesoDataSource procesos,
                                     IEtapaDataSource etapas,
                                     ITipoBotellaDataSource tiposBotella,
                                     MateriaPrimaService materiaPrima)
    {
        _procesos = procesos;
        _etapas = etapas;
        _tiposBotella = tiposBotella;
        _materiaPrima = materiaPrima;
    }

    /// <summary>
    /// Botellas de este tipo que ya tomaron los procesos existentes: por cada proceso de ese
    /// tipo, la etapa Completada más antigua (la primera que se completó) aporta su
    /// <see cref="Etapa.CantidadProcesada"/> — las etapas siguientes del mismo proceso no cuentan
    /// de nuevo, son las mismas botellas avanzando, no material adicional.
    /// </summary>
    public int BotellasTomadasPorProcesos(int tipoBotellaId)
    {
        var procesosDelTipo = _procesos.GetAll()
            .Where(p => p.TipoBotellaId == tipoBotellaId)
            .Select(p => p.Id)
            .ToHashSet();

        if (procesosDelTipo.Count == 0)
            return 0;

        return _etapas.GetAll()
            .Where(e => procesosDelTipo.Contains(e.ProcesoId) && e.Estado == EstadoEtapa.Completada)
            .GroupBy(e => e.ProcesoId)
            .Sum(g => g.OrderBy(e => e.FechaFin).First().CantidadProcesada ?? 0);
    }

    /// <summary>
    /// Botellas de este tipo, en custodia, que todavía no tomó ningún proceso. Convierte la
    /// custodia (en paletas) a botellas con el patrón de paletizado vigente del tipo de botella.
    /// </summary>
    public int BotellasDisponibles(int tipoBotellaId)
    {
        if (_tiposBotella.GetById(tipoBotellaId) is not { } tipoBotella)
            return 0;

        var custodiaBotellas = _materiaPrima.ExistenciaPaletas(tipoBotellaId) * tipoBotella.BotellasPorPaleta;
        return custodiaBotellas - BotellasTomadasPorProcesos(tipoBotellaId);
    }
}
