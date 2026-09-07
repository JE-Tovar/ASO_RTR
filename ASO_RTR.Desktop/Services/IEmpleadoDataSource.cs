using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.Services;

/// <summary>
/// Maestro de empleados. La implementa una fuente EF Core;
/// la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface IEmpleadoDataSource : ICrudDataSource<Empleado, int>
{
}
