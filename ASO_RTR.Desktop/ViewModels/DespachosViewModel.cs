using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using ASO_RTR.Desktop.Configuration;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Navigation;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>
/// Materia Prima · Despachos: el historial de lo que volvió a Dusa ya procesado y el registro de
/// lo nuevo.
///
/// Misma forma que Recepciones: las filas son documentos, así que no se editan ni se borran, se
/// anulan. Anular devuelve la custodia sola, porque el kardex no cuenta lo anulado.
/// </summary>
public sealed class DespachosViewModel : PantallaCrudViewModel<DespachoProductoTerminado, int>
{
    private const string FiltroTodos = "Todos";

    private readonly ITipoBotellaDataSource _tiposBotella;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly MateriaPrimaService _materiaPrima;
    private readonly DespachosService _servicio;

    private string _filtro = FiltroTodos;

    public DespachosViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearDespachosProductoTerminado(),
               new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private DespachosViewModel(Modulo modulo,
                               Submodulo submodulo,
                               IDespachoProductoTerminadoDataSource despachos,
                               IServicioDialogo dialogos,
                               ISesionActual sesion)
        : base(modulo, submodulo, despachos, dialogos, sesion)
    {
        _dialogos = dialogos;
        _sesionActual = sesion;
        _tiposBotella = DataSourceFactory.CrearTiposBotella();

        _materiaPrima = new MateriaPrimaService(DataSourceFactory.CrearRecepcionesMateriaPrima(), despachos);
        _servicio = new DespachosService(despachos, _materiaPrima, sesion);

        CambiarFiltroCommand = new RelayCommand<string>(filtro =>
        {
            _filtro = filtro;
            ItemsView.Refresh();
        });

        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } d && _servicio.PuedeAnular(d)
                  && _sesionActual.Puede(Permisos.Despachos.Anular));
    }

    public ICommand CambiarFiltroCommand { get; }
    public ICommand AnularCommand { get; }

    public string Resumen =>
        $"{Items.Count(d => d.Estado == EstadoDespacho.Registrada)} despachos · " +
        $"{_servicio.DelMes().Count} este mes";

    protected override string ModuloPermiso => "Despachos";

    protected override bool CoincideBusqueda(DespachoProductoTerminado item, string texto) =>
        item.Numero.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.AutorizadoPorNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Lineas.Any(l => l.TipoBotellaEtiqueta.Contains(texto, StringComparison.OrdinalIgnoreCase));

    protected override bool PasaFiltroExtra(DespachoProductoTerminado item) => _filtro switch
    {
        "Anulados" => item.Estado == EstadoDespacho.Anulada,
        _ => true
    };

    protected override bool PuedeEditar(DespachoProductoTerminado item) => false;

    protected override bool PuedeEliminar(DespachoProductoTerminado item) => false;

    protected override DespachoProductoTerminado CrearNuevo() => new()
    {
        Fecha = DateTime.Today,
        Estado = EstadoDespacho.Registrada
    };

    protected override CrudEditorViewModelBase<DespachoProductoTerminado> CrearEditor(DespachoProductoTerminado item) =>
        new DespachoEditorViewModel(item,
                                    [.. _tiposBotella.GetAll().Where(t => t.Activo).OrderBy(t => t.Etiqueta)],
                                    _materiaPrima.ExistenciasPorTipoBotella(),
                                    _sesionActual.UsuarioActual?.NombreCompleto ?? string.Empty,
                                    _servicio);

    /// <summary>
    /// La emisión pasa por el servicio de dominio: es él quien asigna el número del despacho,
    /// estampa quién autoriza y comprueba que haya custodia suficiente.
    /// </summary>
    protected override void Agregar()
    {
        var editor = CrearEditor(CrearNuevo());

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.Registrar(editor.ObtenerResultado(),
                                          _sesionActual.UsuarioActual?.Id ?? 0));
    }

    private void Anular()
    {
        if (SelectedItem is not { } despacho)
            return;

        var editor = new MotivoEditorViewModel(
            $"Anular despacho {despacho.Numero}",
            $"{despacho.TotalPaletas} paletas — autorizó {despacho.AutorizadoPorNombre}",
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.Anular(despacho, editor.Motivo));
    }

    private void Aplicar(Func<DespachoProductoTerminado> transicion)
    {
        try
        {
            SeleccionarTrasRecargar(transicion().Id);
        }
        catch (InvalidOperationException ex)
        {
            _dialogos.Informar("No se pudo completar la operación", ex.Message);
        }
    }
}

/// <summary>
/// El despacho. Lo que vuelve a Dusa, ya procesado.
///
/// "Autoriza" no es un campo: se muestra de solo lectura y lo estampa el servicio con el usuario
/// de la sesión, para que no se pueda escribir otro nombre en el papel.
/// </summary>
public sealed class DespachoEditorViewModel : CrudEditorViewModelBase<DespachoProductoTerminado>
{
    private readonly DespachoProductoTerminado _original;
    private readonly IReadOnlyDictionary<int, int> _existencias;
    private readonly DespachosService _servicio;

    public DespachoEditorViewModel(DespachoProductoTerminado original,
                                   IReadOnlyList<TipoBotella> tiposBotella,
                                   IReadOnlyDictionary<int, int> existencias,
                                   string autorizadoPorNombre,
                                   DespachosService servicio)
    {
        _original = original;
        _existencias = existencias;
        _servicio = servicio;

        TiposBotella = tiposBotella;
        AutorizadoPorNombre = autorizadoPorNombre;

        Fecha = original.Fecha == default ? DateTime.Today : original.Fecha;
        Observaciones = original.Observaciones;
        FacturaDusaReferencia = original.FacturaDusaReferencia ?? string.Empty;
        FacturaDusaFecha = original.FacturaDusaFecha;

        Lineas.CollectionChanged += AlCambiarLineas;

        AgregarLineaCommand = new RelayCommand(() => Lineas.Add(NuevaLinea()));

        QuitarLineaCommand = new RelayCommand<LineaDespachoEditorViewModel>(linea =>
        {
            if (linea is not null)
                Lineas.Remove(linea);
        });

        Lineas.Add(NuevaLinea());
    }

    public override string Titulo => "Registrar despacho";

    /// <summary>Amplio: lleva una grilla de líneas dentro.</summary>
    public override double AnchoEditor => Ancho.Amplio;

    public override string TextoAccion => "Registrar despacho";

    public IReadOnlyList<TipoBotella> TiposBotella { get; }

    /// <summary>Quién autoriza: el usuario de la sesión, de solo lectura.</summary>
    public string AutorizadoPorNombre { get; }

    public ObservableCollection<LineaDespachoEditorViewModel> Lineas { get; } = [];

    public ICommand AgregarLineaCommand { get; }
    public ICommand QuitarLineaCommand { get; }

    private DateTime _fecha = DateTime.Today;
    public DateTime Fecha
    {
        get => _fecha;
        set => SetProperty(ref _fecha, value);
    }

    private string _observaciones = string.Empty;
    public string Observaciones
    {
        get => _observaciones;
        set => SetProperty(ref _observaciones, value);
    }

    /// <summary>
    /// PROVISIONAL: constancia manual de que el servicio ya se facturó a Dusa por fuera de la
    /// aplicación. No genera ni valida nada — ver CLAUDE.md, no hay Cuentas por Cobrar todavía.
    /// </summary>
    private string _facturaDusaReferencia = string.Empty;
    public string FacturaDusaReferencia
    {
        get => _facturaDusaReferencia;
        set => SetProperty(ref _facturaDusaReferencia, value);
    }

    private DateTime? _facturaDusaFecha;
    public DateTime? FacturaDusaFecha
    {
        get => _facturaDusaFecha;
        set => SetProperty(ref _facturaDusaFecha, value);
    }

    public int TotalPaletas => Lineas.Sum(l => l.CantidadValor);

    protected override bool Validar(out string? error)
    {
        if (Lineas.Any(l => l.TipoBotellaSeleccionado is not null && !l.CantidadEsValida))
        {
            error = "Hay una cantidad de paletas que no es un número válido.";
            return false;
        }

        // La autoridad es el servicio, que vuelve a mirar la custodia real en el momento de
        // guardar: entre que se abrió el despacho y se emite, otro puesto pudo haber despachado.
        return _servicio.Validar(ObtenerResultado(), out error);
    }

    public override DespachoProductoTerminado ObtenerResultado()
    {
        var despacho = _original.Clonar();
        despacho.Fecha = Fecha.Date;
        despacho.Observaciones = Observaciones.Trim();
        despacho.FacturaDusaReferencia = string.IsNullOrWhiteSpace(FacturaDusaReferencia)
            ? null
            : FacturaDusaReferencia.Trim();
        despacho.FacturaDusaFecha = FacturaDusaFecha;

        despacho.Lineas = [.. Lineas
            .Where(l => l.TipoBotellaSeleccionado is not null)
            .Select(l => l.Construir())];

        return despacho;
    }

    private LineaDespachoEditorViewModel NuevaLinea()
    {
        var linea = new LineaDespachoEditorViewModel(TiposBotella, _existencias);
        linea.Cambio += AlCambiarLinea;
        return linea;
    }

    private void AlCambiarLineas(object? remitente, NotifyCollectionChangedEventArgs e)
    {
        foreach (var linea in e.OldItems?.OfType<LineaDespachoEditorViewModel>() ?? [])
            linea.Cambio -= AlCambiarLinea;

        AlCambiarLinea(this, EventArgs.Empty);
    }

    private void AlCambiarLinea(object? remitente, EventArgs e) => OnPropertyChanged(nameof(TotalPaletas));
}

/// <summary>
/// Un renglón del despacho mientras se escribe. Como el de las recepciones, pero con la custodia
/// disponible a la vista: avisa en el acto de que se está pidiendo de más, sin esperar a que el
/// servicio rechace el despacho entero.
/// </summary>
public sealed class LineaDespachoEditorViewModel : ViewModelBase
{
    public event EventHandler? Cambio;

    private readonly IReadOnlyDictionary<int, int> _existencias;

    public LineaDespachoEditorViewModel(IReadOnlyList<TipoBotella> tiposBotella,
                                        IReadOnlyDictionary<int, int> existencias)
    {
        TiposBotella = tiposBotella;
        _existencias = existencias;
    }

    public IReadOnlyList<TipoBotella> TiposBotella { get; }

    private TipoBotella? _tipoBotellaSeleccionado;
    public TipoBotella? TipoBotellaSeleccionado
    {
        get => _tipoBotellaSeleccionado;
        set { if (SetProperty(ref _tipoBotellaSeleccionado, value)) Recalcular(); }
    }

    private string _cantidadPaletas = string.Empty;
    public string CantidadPaletas
    {
        get => _cantidadPaletas;
        set { if (SetProperty(ref _cantidadPaletas, value)) Recalcular(); }
    }

    public bool CantidadEsValida =>
        !string.IsNullOrWhiteSpace(CantidadPaletas) && int.TryParse(CantidadPaletas, out var v) && v > 0;

    public int CantidadValor => int.TryParse(CantidadPaletas, out var valor) ? valor : 0;

    public int Disponible => TipoBotellaSeleccionado is { } tipo ? _existencias.GetValueOrDefault(tipo.Id) : 0;

    public string DisponibleTexto => TipoBotellaSeleccionado is null ? string.Empty : $"{Disponible} paletas";

    /// <summary>Se pinta en rojo en la grilla; la comprobación de verdad la hace el servicio.</summary>
    public bool SePasa => TipoBotellaSeleccionado is not null && CantidadValor > Disponible;

    public DespachoProductoTerminadoLinea Construir() => new()
    {
        TipoBotellaId = TipoBotellaSeleccionado!.Id,
        TipoBotellaEtiqueta = TipoBotellaSeleccionado.Etiqueta,
        CantidadPaletas = CantidadValor,
        BotellasPorPaletaSnapshot = TipoBotellaSeleccionado.BotellasPorPaleta
    };

    private void Recalcular()
    {
        OnPropertyChanged(nameof(Disponible));
        OnPropertyChanged(nameof(DisponibleTexto));
        OnPropertyChanged(nameof(SePasa));
        Cambio?.Invoke(this, EventArgs.Empty);
    }
}
