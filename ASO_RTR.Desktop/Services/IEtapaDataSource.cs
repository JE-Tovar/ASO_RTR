using System.Collections.Generic;
using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.Services;

/// <summary>
/// Etapas de los procesos en planta. La implementa una fuente EF Core;
/// la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IEtapaDataSource : ICrudDataSource<Etapa, int>
{
    IEnumerable<Etapa> GetByProceso(int procesoId);
}
