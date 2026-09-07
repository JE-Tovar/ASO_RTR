using System.Collections.Generic;
using System.Linq;
using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>
/// Catálogo · Tipos de Botella: ficha de solo lectura con el desglose de la paleta, nivel por
/// nivel. Todo lo que muestra ya está calculado en <see cref="TipoBotella"/>
/// (<see cref="TipoBotella.Niveles"/>, <see cref="TipoBotella.BotellasPorNivel"/>); este ViewModel
/// solo arma la lista repetida (un renglón por nivel) porque el modelo no guarda cantidades
/// distintas por nivel — la paleta es uniforme.
///
/// Primer consumidor real de <see cref="MuestraCancelar"/>=false / <see cref="TextoAccion"/>
/// ("Cerrar"): el mecanismo ya estaba en <see cref="CrudEditorViewModelBase"/> desde el scaffold,
/// pero ninguna pantalla lo había usado.
/// </summary>
public sealed class TipoBotellaDetalleViewModel : CrudEditorViewModelBase
{
    public TipoBotellaDetalleViewModel(TipoBotella tipoBotella)
    {
        Etiqueta = tipoBotella.Etiqueta;
        PresentacionTexto = tipoBotella.PresentacionTexto;
        CantidadNiveles = tipoBotella.Niveles;
        BotellasPorNivel = tipoBotella.BotellasPorNivel;
        BotellasPorPaleta = tipoBotella.BotellasPorPaleta;

        EsCaja = tipoBotella.Presentacion == PresentacionEmpaque.Caja;
        MuestraPatronPar = EsCaja && tipoBotella.Niveles > 1;
        MuestraPanal = !EsCaja;

        DetalleNiveles = Enumerable.Range(1, tipoBotella.Niveles)
            .Select(numero => new NivelDetalleItem(numero, tipoBotella.BotellasPorNivel, EsCaja))
            .ToList();

        PatronImpar = ArmarPatron(tipoBotella, tipoBotella.PatronParaNivel(1));
        PatronPar = MuestraPatronPar ? ArmarPatron(tipoBotella, tipoBotella.PatronParaNivel(2)) : [];

        Panal = new PanalPreviewViewModel();
        if (MuestraPanal && tipoBotella.FilasPorNivel > 0 && tipoBotella.ColumnasPorNivel > 0)
            Panal.Regenerar(tipoBotella.FilasPorNivel, tipoBotella.ColumnasPorNivel);
    }

    private static IReadOnlyList<IReadOnlyList<bool>> ArmarPatron(
        TipoBotella tipoBotella, IEnumerable<CeldaPatronEmpaque> celdas)
    {
        var lista = celdas.ToList();

        return [.. Enumerable.Range(1, tipoBotella.FilasPorNivel)
            .Select(fila => (IReadOnlyList<bool>)
            [
                .. Enumerable.Range(1, tipoBotella.ColumnasPorNivel)
                    .Select(columna => lista
                        .FirstOrDefault(c => c.Fila == fila && c.Columna == columna)?.Orientacion
                        == OrientacionCaja.Vertical)
            ])];
    }

    public override string Titulo => $"Detalle de paleta · {Etiqueta}";

    public override string TextoAccion => "Cerrar";

    public override bool MuestraCancelar => false;

    public string Etiqueta { get; }
    public string PresentacionTexto { get; }
    public int CantidadNiveles { get; }
    public int BotellasPorNivel { get; }
    public int BotellasPorPaleta { get; }

    public IReadOnlyList<NivelDetalleItem> DetalleNiveles { get; }

    public bool EsCaja { get; }
    public bool MuestraPatronPar { get; }
    public IReadOnlyList<IReadOnlyList<bool>> PatronImpar { get; }
    public IReadOnlyList<IReadOnlyList<bool>> PatronPar { get; }

    public bool MuestraPanal { get; }
    public PanalPreviewViewModel Panal { get; }

    /// <summary>Ficha de solo lectura: no hay nada que validar antes de cerrar.</summary>
    protected override bool Validar(out string? error)
    {
        error = null;
        return true;
    }
}

/// <summary>Un renglón del desglose: el nivel (camada) y cuántas botellas trae.</summary>
public sealed class NivelDetalleItem(int numero, int cantidad, bool esCaja)
{
    public string NumeroTexto => $"Nivel {numero}";
    public int Cantidad { get; } = cantidad;

    /// <summary>
    /// El patrón de paletizado solo existe para cajas (nivel impar/par); a granel no hay nada
    /// que alternar entre niveles, así que el renglón se queda solo con la cantidad.
    /// </summary>
    public string DetalleTexto => esCaja
        ? $"{Cantidad} botellas ({(numero % 2 == 1 ? "patrón impar" : "patrón par")})"
        : $"{Cantidad} botellas";
}
