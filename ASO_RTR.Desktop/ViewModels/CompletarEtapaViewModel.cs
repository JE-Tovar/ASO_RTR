using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>
/// Una fila fija del formulario de "Completar etapa": el empleado y cuántas botellas hizo. Es un
/// ViewModel y no el modelo <see cref="EtapaEmpleado"/> porque el total del pie tiene que
/// recalcularse según se teclea, y los modelos no avisan de sus cambios.
/// </summary>
public sealed class EmpleadoProcesadoViewModel : ViewModelBase
{
    /// <summary>Avisa al editor de que hay que recalcular el total.</summary>
    public event EventHandler? Cambio;

    public EmpleadoProcesadoViewModel(int empleadoId, string empleadoNombre)
    {
        EmpleadoId = empleadoId;
        EmpleadoNombre = empleadoNombre;
    }

    public int EmpleadoId { get; }
    public string EmpleadoNombre { get; }

    private string _cantidad = string.Empty;
    public string Cantidad
    {
        get => _cantidad;
        set { if (SetProperty(ref _cantidad, value)) Cambio?.Invoke(this, EventArgs.Empty); }
    }

    public bool CantidadEsValida => int.TryParse(Cantidad, out var v) && v >= 0;

    public int CantidadValor => int.TryParse(Cantidad, out var v) ? v : 0;
}

/// <summary>Una opción del combo de empleado en una línea de merma: solo los de esta etapa.</summary>
public sealed record EmpleadoOpcion(int EmpleadoId, string Nombre);

/// <summary>
/// Una línea libre de merma: quién, qué artículo y cuánto. De 0 a N por empleado — se
/// agrega/quita dinámicamente, igual que las líneas de <see cref="EntradaEditorViewModel"/>.
/// </summary>
public sealed class MermaLineaViewModel : ViewModelBase
{
    public event EventHandler? Cambio;

    public MermaLineaViewModel(IReadOnlyList<EmpleadoOpcion> empleados, IReadOnlyList<Articulo> articulos)
    {
        Empleados = empleados;
        Articulos = articulos;
    }

    public IReadOnlyList<EmpleadoOpcion> Empleados { get; }
    public IReadOnlyList<Articulo> Articulos { get; }

    private EmpleadoOpcion? _empleado;
    public EmpleadoOpcion? Empleado
    {
        get => _empleado;
        set { if (SetProperty(ref _empleado, value)) Cambio?.Invoke(this, EventArgs.Empty); }
    }

    private Articulo? _articulo;
    public Articulo? Articulo
    {
        get => _articulo;
        set { if (SetProperty(ref _articulo, value)) Cambio?.Invoke(this, EventArgs.Empty); }
    }

    private string _cantidad = string.Empty;
    public string Cantidad
    {
        get => _cantidad;
        set { if (SetProperty(ref _cantidad, value)) Cambio?.Invoke(this, EventArgs.Empty); }
    }

    public bool CantidadEsValida => int.TryParse(Cantidad, out var v) && v > 0;

    public int CantidadValor => int.TryParse(Cantidad, out var v) ? v : 0;

    /// <summary>Ni empleado, ni artículo, ni cantidad tocados: una fila en blanco, no se manda.</summary>
    public bool EsVacia => Empleado is null && Articulo is null && string.IsNullOrWhiteSpace(Cantidad);

    /// <summary>Completa y lista para convertirse en un <see cref="EtapaService.DatoMermaEmpleado"/>.</summary>
    public bool EsValida => Empleado is not null && Articulo is not null && CantidadEsValida;
}

/// <summary>
/// Completar una etapa: cuánto procesó cada empleado, más las líneas de merma que hubiera (libres,
/// no una por empleado). El consumo automático de materiales (<see cref="Etapa.Consumos"/>) no se
/// pide aquí — ya quedó configurado al crear/editar la etapa — pero se muestra una vista previa
/// de lo que se va a descontar, para que no sea una sorpresa silenciosa.
/// </summary>
public sealed class CompletarEtapaViewModel : CrudEditorViewModelBase
{
    private readonly Etapa _etapa;

    public CompletarEtapaViewModel(Etapa etapa, IReadOnlyList<Articulo> articulos)
    {
        _etapa = etapa;
        Descripcion = $"{etapa.ProcesoEtiqueta} — {etapa.TipoTexto}";

        EmpleadosOpciones = [.. etapa.Empleados.Select(e => new EmpleadoOpcion(e.EmpleadoId, e.EmpleadoNombre))];
        Articulos = articulos;

        Renglones = etapa.Empleados
            .Select(e => new EmpleadoProcesadoViewModel(e.EmpleadoId, e.EmpleadoNombre))
            .ToList();

        foreach (var renglon in Renglones)
            renglon.Cambio += AlCambiar;

        Mermas = [];
        Mermas.Add(NuevaMerma());

        AgregarMermaCommand = new RelayCommand(() => Mermas.Add(NuevaMerma()));
        QuitarMermaCommand = new RelayCommand<MermaLineaViewModel>(linea =>
        {
            if (linea is not null)
                Mermas.Remove(linea);
        });
    }

    public override string Titulo => "Completar etapa";

    /// <summary>Amplio: procesadas, merma libre y vista previa de consumo ya no caben en Estándar.</summary>
    public override double AnchoEditor => Ancho.Amplio;

    public override string TextoAccion => "Completar etapa";

    public string Descripcion { get; }

    public IReadOnlyList<EmpleadoOpcion> EmpleadosOpciones { get; }
    public IReadOnlyList<Articulo> Articulos { get; }

    public IReadOnlyList<EmpleadoProcesadoViewModel> Renglones { get; }

    public ObservableCollection<MermaLineaViewModel> Mermas { get; }

    public ICommand AgregarMermaCommand { get; }
    public ICommand QuitarMermaCommand { get; }

    public int TotalProcesado => Renglones.Sum(r => r.CantidadValor);
    public string TotalProcesadoTexto => TotalProcesado.ToString();

    public int TotalMerma => Mermas.Where(m => m.CantidadEsValida).Sum(m => m.CantidadValor);
    public string TotalMermaTexto => TotalMerma.ToString();

    /// <summary>Solo informativa: lo que se va a descontar solo, ya multiplicado por lo procesado.</summary>
    public bool MuestraConsumoPreview => _etapa.Consumos.Count > 0;

    public IReadOnlyList<string> ConsumoPreview =>
        [.. _etapa.Consumos.Select(c => $"{c.ArticuloNombre}: {c.UnidadesPorBotella * TotalProcesado:0.##} {c.UnidadTexto}".Trim())];

    public IReadOnlyList<EtapaService.DatoProcesadoEmpleado> ObtenerProcesados() =>
        [.. Renglones.Select(r => new EtapaService.DatoProcesadoEmpleado(r.EmpleadoId, r.CantidadValor))];

    public IReadOnlyList<EtapaService.DatoMermaEmpleado> ObtenerMermas() =>
        [.. Mermas.Where(m => m.EsValida)
            .Select(m => new EtapaService.DatoMermaEmpleado(m.Empleado!.EmpleadoId, m.Articulo!, m.CantidadValor))];

    protected override bool Validar(out string? error)
    {
        if (Renglones.Any(r => !r.CantidadEsValida))
        {
            error = "Indique cuántas botellas hizo cada empleado (número entero, 0 o más).";
            return false;
        }

        if (Mermas.Any(m => !m.EsVacia && !m.EsValida))
        {
            error = "Complete empleado, artículo y cantidad en cada línea de merma, o quítela.";
            return false;
        }

        error = null;
        return true;
    }

    private MermaLineaViewModel NuevaMerma()
    {
        var linea = new MermaLineaViewModel(EmpleadosOpciones, Articulos);
        linea.Cambio += AlCambiar;
        return linea;
    }

    private void AlCambiar(object? remitente, EventArgs e)
    {
        OnPropertyChanged(nameof(TotalProcesado));
        OnPropertyChanged(nameof(TotalProcesadoTexto));
        OnPropertyChanged(nameof(TotalMerma));
        OnPropertyChanged(nameof(TotalMermaTexto));
        OnPropertyChanged(nameof(ConsumoPreview));
    }
}
