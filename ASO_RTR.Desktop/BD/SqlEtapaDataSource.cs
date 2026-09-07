using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.BD;

/// <summary>La etapa se guarda entera con los empleados que la ejecutaron.</summary>
public class SqlEtapaDataSource : SqlAgregadoDataSource<Etapa, int>, IEtapaDataSource
{
    protected override IQueryable<Etapa> Incluir(IQueryable<Etapa> consulta)
        => consulta.Include(e => e.Empleados);

    protected override Expression<Func<Etapa, bool>> PorId(int id) => e => e.Id == id;

    protected override IEnumerable<object> HijosDe(Etapa raiz) => raiz.Empleados;

    protected override void CopiarHijos(Etapa destino, Etapa origen)
        => destino.Empleados = origen.Empleados;

    public IEnumerable<Etapa> GetByProceso(int procesoId)
        => Consultar(q => q.Where(e => e.ProcesoId == procesoId));
}
