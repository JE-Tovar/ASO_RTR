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
/// Materia Prima · Recepciones: el historial de lo que trajo la gandola de Dusa y el registro de
/// lo nuevo.
///
/// Las filas son documentos: no se editan ni se borran, se anulan. Por eso la pantalla apaga
/// Editar y Eliminar y solo deja "Registrar recepción" y "Anular".
/// </summary>
public sealed class RecepcionesViewModel : PantallaCrudViewModel<RecepcionMateriaPrima, int>
{
    private const string FiltroTodas = "Todas";

    private readonly ITipoBotellaDataSource _tiposBotella;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly MateriaPrimaService _materiaPrima;
    private readonly RecepcionesService _servicio;

    private string _filtro = FiltroTodas;

    public RecepcionesViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearRecepcionesMateriaPrima(),
               new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private RecepcionesViewModel(Modulo modulo,
                                 Submodulo submodulo,
                                 IRecepcionMateriaPrimaDataSource recepciones,
                                 IServicioDialogo dialogos,
                                 ISesionActual sesion)
        : base(modulo, submodulo, recepciones, dialogos, sesion)
    {
        _dialogos = dialogos;
        _sesionActual = sesion;
        _tiposBotella = DataSourceFactory.CrearTiposBotella();

        _materiaPrima = new MateriaPrimaService(recepciones, DataSourceFactory.CrearDespachosProductoTerminado());
        _servicio = new RecepcionesService(recepciones, _materiaPrima, sesion);

        CambiarFiltroCommand = new RelayCommand<string>(filtro =>
        {
            _filtro = filtro;
            ItemsView.Refresh();
        });

        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } r && _servicio.PuedeAnular(r)
                  && _sesionActual.Puede(Permisos.Recepciones.Anular));
    }

    public ICommand CambiarFiltroCommand { get; }
    public ICommand AnularCommand { get; }

    public string Resumen =>
        $"{Items.Count(r => r.Estado == EstadoRecepcion.Registrada)} recepciones · " +
        $"{_materiaPrima.TotalPaletasEnCustodia()} paletas en custodia";

    protected override string ModuloPermiso => "Recepciones";

    protected override bool CoincideBusqueda(RecepcionMateriaPrima item, string texto) =>
        item.Numero.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.NumeroOrdenEntrega.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Gandolero.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Placa.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Lineas.Any(l => l.TipoBotellaEtiqueta.Contains(texto, StringComparison.OrdinalIgnoreCase));

    protected override bool PasaFiltroExtra(RecepcionMateriaPrima item) => _filtro switch
    {
        "Anuladas" => item.Estado == EstadoRecepcion.Anulada,
        _ => true
    };

    /// <summary>Un documento no se corrige ni se borra: se anula, y eso deja constancia.</summary>
    protected override bool PuedeEditar(RecepcionMateriaPrima item) => false;

    protected override bool PuedeEliminar(RecepcionMateriaPrima item) => false;

    protected override RecepcionMateriaPrima CrearNuevo() => new()
    {
        Fecha = DateTime.Today,
        Estado = EstadoRecepcion.Registrada
    };

    protected override CrudEditorViewModelBase<RecepcionMateriaPrima> CrearEditor(RecepcionMateriaPrima item) =>
        new RecepcionEditorViewModel(item,
                                     [.. _tiposBotella.GetAll().Where(t => t.Activo).OrderBy(t => t.Etiqueta)],
                                     _servicio);

    /// <summary>
    /// El alta pasa por el servicio de dominio y no por la fuente de datos: registrar la
    /// recepción exige el correlativo y las validaciones de negocio, que el CRUD genérico no
    /// conoce.
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
        if (SelectedItem is not { } recepcion)
            return;

        var editor = new MotivoEditorViewModel(
            $"Anular recepción {recepcion.Numero}",
            $"Orden de entrega {recepcion.NumeroOrdenEntrega} — {recepcion.TotalPaletas} paletas",
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.Anular(recepcion, editor.Motivo));
    }

    private void Aplicar(Func<RecepcionMateriaPrima> transicion)
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

/// <summary>El modal de "Registrar recepción": la orden de entrega y las paletas que trajo.</summary>
public sealed class RecepcionEditorViewModel : CrudEditorViewModelBase<RecepcionMateriaPrima>
{
    private readonly RecepcionMateriaPrima _original;
    private readonly RecepcionesService _servicio;

    public RecepcionEditorViewModel(RecepcionMateriaPrima original,
                                    IReadOnlyList<TipoBotella> tiposBotella,
                                    RecepcionesService servicio)
    {
        _original = original;
        _servicio = servicio;

        TiposBotella = tiposBotella;

        Fecha = original.Fecha == default ? DateTime.Today : original.Fecha;
        NumeroOrdenEntrega = original.NumeroOrdenEntrega;
        Gandolero = original.Gandolero;
        Placa = original.Placa;
        Observaciones = original.Observaciones;

        Lineas.CollectionChanged += AlCambiarLineas;

        AgregarLineaCommand = new RelayCommand(() => Lineas.Add(NuevaLinea()));

        QuitarLineaCommand = new RelayCommand<LineaRecepcionEditorViewModel>(linea =>
        {
            if (linea is not null)
                Lineas.Remove(linea);
        });

        Lineas.Add(NuevaLinea());
    }

    public override string Titulo => "Registrar recepción";

    /// <summary>Amplio: lleva una grilla de líneas dentro.</summary>
    public override double AnchoEditor => Ancho.Amplio;

    public override string TextoAccion => "Registrar recepción";

    public IReadOnlyList<TipoBotella> TiposBotella { get; }

    public ObservableCollection<LineaRecepcionEditorViewModel> Lineas { get; } = [];

    public ICommand AgregarLineaCommand { get; }
    public ICommand QuitarLineaCommand { get; }

    private DateTime _fecha = DateTime.Today;
    public DateTime Fecha
    {
        get => _fecha;
        set => SetProperty(ref _fecha, value);
    }

    private string _numeroOrdenEntrega = string.Empty;
    public string NumeroOrdenEntrega
    {
        get => _numeroOrdenEntrega;
        set => SetProperty(ref _numeroOrdenEntrega, value);
    }

    private string _gandolero = string.Empty;
    public string Gandolero
    {
        get => _gandolero;
        set => SetProperty(ref _gandolero, value);
    }

    private string _placa = string.Empty;
    public string Placa
    {
        get => _placa;
        set => SetProperty(ref _placa, value);
    }

    private string _observaciones = string.Empty;
    public string Observaciones
    {
        get => _observaciones;
        set => SetProperty(ref _observaciones, value);
    }

    public int TotalPaletas => Lineas.Sum(l => l.CantidadValor);

    protected override bool Validar(out string? error)
    {
        if (Lineas.Any(l => l.TipoBotellaSeleccionado is not null && !l.CantidadEsValida))
        {
            error = "Hay una cantidad de paletas que no es un número válido.";
            return false;
        }

        // La autoridad es el servicio: aquí solo se le pregunta, para que el mensaje salga en el
        // formulario en vez de en un diálogo de error después de cerrarlo.
        return _servicio.Validar(ObtenerResultado(), out error);
    }

    public override RecepcionMateriaPrima ObtenerResultado()
    {
        var recepcion = _original.Clonar();
        recepcion.Fecha = Fecha.Date;
        recepcion.NumeroOrdenEntrega = NumeroOrdenEntrega.Trim();
        recepcion.Gandolero = Gandolero.Trim();
        recepcion.Placa = Placa.Trim();
        recepcion.Observaciones = Observaciones.Trim();

        // Las líneas en blanco no se mandan: una fila vacía al final es lo normal mientras se
        // carga la recepción, y no tiene por qué impedir guardar.
        recepcion.Lineas = [.. Lineas
            .Where(l => l.TipoBotellaSeleccionado is not null)
            .Select(l => l.Construir())];

        return recepcion;
    }

    private LineaRecepcionEditorViewModel NuevaLinea()
    {
        var linea = new LineaRecepcionEditorViewModel(TiposBotella);
        linea.Cambio += AlCambiarLinea;
        return linea;
    }

    private void AlCambiarLineas(object? remitente, NotifyCollectionChangedEventArgs e)
    {
        foreach (var linea in e.OldItems?.OfType<LineaRecepcionEditorViewModel>() ?? [])
            linea.Cambio -= AlCambiarLinea;

        AlCambiarLinea(this, EventArgs.Empty);
    }

    private void AlCambiarLinea(object? remitente, EventArgs e) => OnPropertyChanged(nameof(TotalPaletas));
}

/// <summary>
/// Un renglón de la grilla de líneas mientras se está escribiendo.
///
/// Es un ViewModel y no el modelo <see cref="RecepcionMateriaPrimaLinea"/> porque los modelos no
/// avisan de sus cambios, y aquí hace falta: el total de botellas tiene que moverse según se
/// teclea. La cantidad viaja como texto por el mismo motivo que en el resto de los editores: un
/// campo vacío no es un cero.
/// </summary>
public sealed class LineaRecepcionEditorViewModel : ViewModelBase
{
    /// <summary>Avisa al editor de que hay que recalcular el total.</summary>
    public event EventHandler? Cambio;

    public LineaRecepcionEditorViewModel(IReadOnlyList<TipoBotella> tiposBotella)
    {
        TiposBotella = tiposBotella;
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

    /// <summary>Vista previa de cuántas botellas trae la cantidad de paletas tecleada.</summary>
    public int TotalBotellas => CantidadValor * (TipoBotellaSeleccionado?.BotellasPorPaleta ?? 0);

    public string TotalBotellasTexto => TipoBotellaSeleccionado is null ? string.Empty : $"{TotalBotellas} botellas";

    public RecepcionMateriaPrimaLinea Construir() => new()
    {
        TipoBotellaId = TipoBotellaSeleccionado!.Id,
        TipoBotellaEtiqueta = TipoBotellaSeleccionado.Etiqueta,
        CantidadPaletas = CantidadValor,
        BotellasPorPaletaSnapshot = TipoBotellaSeleccionado.BotellasPorPaleta
    };

    private void Recalcular()
    {
        OnPropertyChanged(nameof(TotalBotellas));
        OnPropertyChanged(nameof(TotalBotellasTexto));
        Cambio?.Invoke(this, EventArgs.Empty);
    }
}
