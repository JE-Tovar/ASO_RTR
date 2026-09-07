using System.Collections.Generic;
using System.Linq;
using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>Una celda de la matriz de paletizado, alternable horizontal/vertical con un clic.</summary>
public sealed class CeldaOrientacionViewModel : ViewModelBase
{
    public CeldaOrientacionViewModel(int fila, int columna, OrientacionCaja inicial)
    {
        Fila = fila;
        Columna = columna;
        _orientacion = inicial;
    }

    public int Fila { get; }
    public int Columna { get; }

    private OrientacionCaja _orientacion;
    public bool EsVertical
    {
        get => _orientacion == OrientacionCaja.Vertical;
        set
        {
            var nueva = value ? OrientacionCaja.Vertical : OrientacionCaja.Horizontal;
            if (_orientacion == nueva)
                return;

            _orientacion = nueva;
            OnPropertyChanged(nameof(EsVertical));
        }
    }

    public OrientacionCaja Orientacion => _orientacion;
}

/// <summary>Una fila de la matriz: sus celdas de izquierda a derecha.</summary>
public sealed record FilaPatronViewModel(IReadOnlyList<CeldaOrientacionViewModel> Celdas);

/// <summary>
/// Una matriz de Filas×Columnas celdas, cada una alternable horizontal/vertical. Se regenera
/// cuando cambian filas o columnas, conservando la orientación de las celdas que sigan
/// existiendo en la misma posición — para no perder lo ya marcado por ajustar el tamaño.
/// </summary>
public sealed class PatronEmpaqueEditorViewModel : ViewModelBase
{
    private List<CeldaOrientacionViewModel> _celdas = [];

    private IReadOnlyList<FilaPatronViewModel> _filas = [];
    public IReadOnlyList<FilaPatronViewModel> Filas
    {
        get => _filas;
        private set => SetProperty(ref _filas, value);
    }

    public IReadOnlyList<CeldaOrientacionViewModel> Celdas => _celdas;

    public void Regenerar(int filas, int columnas, IEnumerable<CeldaPatronEmpaque> existentes)
    {
        var previas = existentes.ToList();

        _celdas = [.. Enumerable.Range(1, filas).SelectMany(f => Enumerable.Range(1, columnas)
            .Select(c => new CeldaOrientacionViewModel(f, c,
                previas.FirstOrDefault(x => x.Fila == f && x.Columna == c)?.Orientacion
                    ?? OrientacionCaja.Horizontal)))];

        Filas = [.. Enumerable.Range(1, filas)
            .Select(f => new FilaPatronViewModel([.. _celdas.Where(x => x.Fila == f)]))];
    }
}
