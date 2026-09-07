using System.Collections.Generic;
using System.Linq;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.BD;

/// <summary>Catálogo plano, sin hijos: le basta el cuerpo común.</summary>
public class SqlArticuloDataSource : SqlCrudDataSource<Articulo, int>, IArticuloDataSource
{
    protected override IQueryable<Articulo> Ordenar(IQueryable<Articulo> consulta)
        => consulta.OrderBy(a => a.Codigo);

    public IEnumerable<Articulo> GetActivos()
        => Consultar(q => q.Where(a => a.Activo).OrderBy(a => a.Nombre));
}
