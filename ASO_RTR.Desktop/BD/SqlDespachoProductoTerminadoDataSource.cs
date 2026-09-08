using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.BD;

/// <summary>El despacho se guarda entero con sus líneas.</summary>
public class SqlDespachoProductoTerminadoDataSource
    : SqlAgregadoDataSource<DespachoProductoTerminado, int>, IDespachoProductoTerminadoDataSource
{
    protected override IQueryable<DespachoProductoTerminado> Incluir(IQueryable<DespachoProductoTerminado> consulta)
        => consulta.Include(d => d.Lineas);

    /// <summary>Lo más reciente arriba: el historial se lee de vuelta desde hoy.</summary>
    protected override IQueryable<DespachoProductoTerminado> Ordenar(IQueryable<DespachoProductoTerminado> consulta)
        => consulta.OrderByDescending(d => d.Fecha).ThenByDescending(d => d.Id);

    protected override Expression<Func<DespachoProductoTerminado, bool>> PorId(int id) => d => d.Id == id;

    protected override IEnumerable<object> HijosDe(DespachoProductoTerminado raiz) => raiz.Lineas;

    protected override void CopiarHijos(DespachoProductoTerminado destino, DespachoProductoTerminado origen)
        => destino.Lineas = origen.Lineas;
}
