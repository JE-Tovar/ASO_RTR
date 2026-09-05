using System.Linq;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.BD;

public class SqlPeticionCambioDataSource : SqlCrudDataSource<PeticionCambio, int>, IPeticionCambioDataSource
{
    /// <summary>La bandeja se lee de arriba abajo: lo ultimo solicitado va primero.</summary>
    protected override IQueryable<PeticionCambio> Ordenar(IQueryable<PeticionCambio> consulta)
        => consulta.OrderByDescending(p => p.SolicitadoEn);
}
