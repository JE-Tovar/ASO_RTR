using System.Collections.Generic;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>Un círculo del panal, ya con su posición (esquina superior izquierda) calculada.</summary>
public sealed record CirculoPanal(double Left, double Top);

/// <summary>
/// Vista de solo lectura de cómo se acomodan botellas a granel en un nivel: un panal de abeja,
/// cada fila impar desplazada media posición para que cada botella encaje en el hueco de las dos
/// de abajo, en vez de quedar derecha a su vecina (lo que dejaría huecos y volvería la torre
/// inestable). A diferencia de <see cref="PatronEmpaqueEditorViewModel"/> (cajas), aquí no hay
/// nada que marcar por celda — una botella redonda no tiene orientación — así que un solo
/// ViewModel sirve igual para el editor y para "Ver detalle".
///
/// El tamaño se calcula "natural" (diámetro fijo); quien lo muestra lo envuelve en un
/// <c>Viewbox</c> con <c>StretchDirection="DownOnly"</c> para que un panal con muchas filas y
/// columnas se encoja solo y quepa en pantalla, sin necesidad de recalcular aquí ningún tamaño
/// según la cantidad de celdas.
/// </summary>
public sealed class PanalPreviewViewModel : ViewModelBase
{
    private const double Diametro = 20;
    private const double EspacioX = 20;

    /// <summary>Un poco menos que el diámetro: las filas se traslapan, como en un panal real.</summary>
    private const double EspacioY = 17;

    public IReadOnlyList<CirculoPanal> Circulos { get; private set; } = [];
    public double Ancho { get; private set; }
    public double Alto { get; private set; }

    public void Regenerar(int filas, int columnas)
    {
        var circulos = new List<CirculoPanal>();

        for (var f = 0; f < filas; f++)
        {
            var offsetX = f % 2 == 1 ? EspacioX / 2 : 0;
            for (var c = 0; c < columnas; c++)
                circulos.Add(new CirculoPanal(offsetX + c * EspacioX, f * EspacioY));
        }

        Circulos = circulos;
        Ancho = columnas <= 0 ? 0 : (columnas - 1) * EspacioX + EspacioX / 2 + Diametro;
        Alto = filas <= 0 ? 0 : (filas - 1) * EspacioY + Diametro;

        OnPropertyChanged(nameof(Circulos));
        OnPropertyChanged(nameof(Ancho));
        OnPropertyChanged(nameof(Alto));
    }
}
