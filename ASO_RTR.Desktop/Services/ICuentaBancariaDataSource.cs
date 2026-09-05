using System.Collections.Generic;
using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.Services;

/// <summary>
/// Catálogo de cuentas del centro (banco, caja chica, divisas). La implementa una fuente EF
/// Core; la interfaz mantiene la UI y los ViewModels ajenos a la persistencia.
/// </summary>
public interface ICuentaBancariaDataSource : ICrudDataSource<CuentaBancaria, int>
{
    /// <summary>Las que se pueden elegir al registrar un movimiento; las cerradas no.</summary>
    IEnumerable<CuentaBancaria> GetActivas();
}
