using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.Services;

/// <summary>
/// Despachos de producto terminado con sus líneas. La implementa una fuente EF Core; la interfaz
/// mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IDespachoProductoTerminadoDataSource : ICrudDataSource<DespachoProductoTerminado, int>
{
}
