using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.BD;

/// <summary>
/// El tipo de botella se guarda entero con su patrón de paletizado (las celdas de orientación
/// por nivel). Tiene que ser un agregado y no un CRUD simple: <c>Id</c> de
/// <see cref="CeldaPatronEmpaque"/> es una propiedad sombra (no está en el CLR type), y un
/// <c>Update</c> ingenuo sobre un grafo desconectado no puede saber qué valor tenía — EF lo
/// rechaza con "shadow key property is unknown" en vez de adivinar.
/// </summary>
public class SqlTipoBotellaDataSource : SqlAgregadoDataSource<TipoBotella, int>, ITipoBotellaDataSource
{
    protected override IQueryable<TipoBotella> Incluir(IQueryable<TipoBotella> consulta)
        => consulta.Include(t => t.PatronEmpaque);

    protected override Expression<Func<TipoBotella, bool>> PorId(int id) => t => t.Id == id;

    protected override IEnumerable<object> HijosDe(TipoBotella raiz) => raiz.PatronEmpaque;

    protected override void CopiarHijos(TipoBotella destino, TipoBotella origen)
        => destino.PatronEmpaque = origen.PatronEmpaque;
}
