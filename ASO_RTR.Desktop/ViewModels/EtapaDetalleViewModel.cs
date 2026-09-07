using System.Collections.Generic;
using System.Linq;
using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>Un renglón del desglose: el empleado, cuántas botellas hizo y qué merma generó.</summary>
public sealed class EmpleadoDetalleItem(string nombre, int cantidad, IReadOnlyList<MermaEtapa> mermas)
{
    public string Nombre { get; } = nombre;
    public int Cantidad { get; } = cantidad;

    public bool TieneMerma => mermas.Count > 0;

    /// <summary>Todas las líneas de merma de este empleado, juntas: "2 Botella 750ml, 3 Etiqueta".</summary>
    public string MermaTexto => string.Join(", ", mermas.Select(m => $"{m.Cantidad} {m.ArticuloNombre}"));
}

/// <summary>Un artículo que la etapa consume por botella, para mostrar en la ficha qué la define.</summary>
public sealed record ConsumoDetalleItem(string ArticuloNombre, decimal UnidadesPorBotella);

/// <summary>
/// Operaciones · Etapas: ficha de solo lectura con el desglose de botellas por empleado. Mismo
/// mecanismo que <see cref="TipoBotellaDetalleViewModel"/> (<c>MuestraCancelar</c>=false,
/// <c>TextoAccion</c>="Cerrar"): antes de completar la etapa, el desglose sale en 0 para todos —
/// no es un error, es que aún no se ha capturado.
/// </summary>
public sealed class EtapaDetalleViewModel : CrudEditorViewModelBase
{
    public EtapaDetalleViewModel(Etapa etapa)
    {
        ProcesoEtiqueta = etapa.ProcesoEtiqueta;
        TipoTexto = etapa.TipoTexto;
        EstadoTexto = etapa.EstadoTexto;
        CantidadProcesada = etapa.CantidadProcesada ?? 0;
        MermaTotal = etapa.MermaTotal ?? 0;
        MermaSalidaNumero = etapa.MermaSalidaNumero;
        ConsumoSalidaNumero = etapa.ConsumoSalidaNumero;

        Detalle = [.. etapa.Empleados.Select(e =>
            new EmpleadoDetalleItem(e.EmpleadoNombre, e.Cantidad,
                [.. etapa.Mermas.Where(m => m.EmpleadoId == e.EmpleadoId)]))];

        Consumos = [.. etapa.Consumos.Select(c => new ConsumoDetalleItem(c.ArticuloNombre, c.UnidadesPorBotella))];
    }

    public override string Titulo => $"Detalle de etapa · {TipoTexto}";

    public override string TextoAccion => "Cerrar";

    public override bool MuestraCancelar => false;

    public string ProcesoEtiqueta { get; }
    public string TipoTexto { get; }
    public string EstadoTexto { get; }
    public int CantidadProcesada { get; }
    public int MermaTotal { get; }
    public string MermaSalidaNumero { get; }
    public string ConsumoSalidaNumero { get; }

    public bool MuestraSalidaMerma => !string.IsNullOrEmpty(MermaSalidaNumero);
    public bool MuestraSalidaConsumo => !string.IsNullOrEmpty(ConsumoSalidaNumero);
    public bool MuestraConsumos => Consumos.Count > 0;

    public IReadOnlyList<EmpleadoDetalleItem> Detalle { get; }
    public IReadOnlyList<ConsumoDetalleItem> Consumos { get; }

    /// <summary>Ficha de solo lectura: no hay nada que validar antes de cerrar.</summary>
    protected override bool Validar(out string? error)
    {
        error = null;
        return true;
    }
}
