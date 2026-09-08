using System;
using System.Windows.Input;
using ASO_RTR.Desktop.Configuration;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Navigation;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>Maestro de procesos. Sub-listado de Operaciones · Procesos.</summary>
public sealed class ProcesosCrudViewModel : CrudViewModelBase<Proceso, int>
{
    private readonly ITipoBotellaDataSource _tiposBotella;
    private readonly IEtapaDataSource _etapas;

    public ProcesosCrudViewModel(IProcesoDataSource procesos,
                                 ITipoBotellaDataSource tiposBotella,
                                 IEtapaDataSource etapas,
                                 IServicioDialogo dialogos,
                                 ISesionActual sesion)
        : base(procesos, dialogos, sesion)
    {
        _tiposBotella = tiposBotella;
        _etapas = etapas;
    }

    protected override string ModuloPermiso => "Procesos";

    protected override bool CoincideBusqueda(Proceso item, string texto) =>
        item.TipoBotellaEtiqueta.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Notas.Contains(texto, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Un proceso con al menos una etapa ya no se puede editar: cambiarle el tipo de botella
    /// después de que una etapa completada le tomó botellas de la custodia corrompería ese
    /// cálculo (que se deriva del `TipoBotellaId` vigente del proceso, no de uno congelado).
    /// </summary>
    protected override bool PuedeEditar(Proceso item) => TieneEtapas(item) is false;

    /// <summary>
    /// Eliminarlo dejaría sus etapas huérfanas (`Etapa.ProcesoId` no tiene clave foránea real) y
    /// el descuento que ya hicieron en la custodia desaparecería del cálculo derivado sin que las
    /// botellas hayan dejado de estar procesadas de verdad.
    /// </summary>
    protected override bool PuedeEliminar(Proceso item) => TieneEtapas(item) is false;

    private bool TieneEtapas(Proceso proceso) => _etapas.GetByProceso(proceso.Id).Any();

    protected override Proceso CrearNuevo() => new() { FechaCreacion = DateTime.Today };

    protected override CrudEditorViewModelBase<Proceso> CrearEditor(Proceso item) =>
        new ProcesoEditorViewModel(item, _tiposBotella);
}

/// <summary>
/// Etapas de los procesos en planta. Sub-listado de Operaciones · Procesos; el encabezado y el
/// conmutador los pone <see cref="OperacionesViewModel"/>.
/// </summary>
public sealed class EtapasCrudViewModel : CrudViewModelBase<Etapa, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IProcesoDataSource _procesos;
    private readonly IEmpleadoDataSource _empleados;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly InventarioService _inventario;
    private readonly CustodiaProduccionService _custodiaProduccion;
    private readonly EtapaService _servicio;

    private string _filtroEstado = FiltroTodas;

    public EtapasCrudViewModel(IEtapaDataSource etapas,
                               IProcesoDataSource procesos,
                               IEmpleadoDataSource empleados,
                               IServicioDialogo dialogos,
                               ISesionActual sesion)
        : base(etapas, dialogos, sesion)
    {
        _procesos = procesos;
        _empleados = empleados;
        _dialogos = dialogos;
        _sesionActual = sesion;

        // La merma de una etapa dispara una salida real de almacén: Operaciones pasa a depender
        // de Inventario por primera vez, mismo patrón que EntradasInventarioService con
        // CuentasPorPagarService en Finanzas.
        _inventario = new InventarioService(DataSourceFactory.CrearArticulos(),
                                            DataSourceFactory.CrearEntradasInventario(),
                                            DataSourceFactory.CrearSalidasInventario());
        var salidasInventario = new SalidasInventarioService(
            DataSourceFactory.CrearSalidasInventario(), _inventario, sesion);

        // Completar la primera etapa de un proceso también toma botellas de la custodia de
        // Materia Prima, mismo espíritu que el consumo de artículos de Inventario.
        var materiaPrima = new MateriaPrimaService(DataSourceFactory.CrearRecepcionesMateriaPrima(),
                                                   DataSourceFactory.CrearDespachosProductoTerminado());
        _custodiaProduccion = new CustodiaProduccionService(procesos, etapas,
                                                            DataSourceFactory.CrearTiposBotella(), materiaPrima);

        _servicio = new EtapaService(etapas, procesos, salidasInventario, _custodiaProduccion, sesion);

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        IniciarCommand = new RelayCommand(Iniciar,
            () => SelectedItem is { } e && _servicio.PuedeIniciar(e) && _sesionActual.Puede(Permisos.Operaciones.Iniciar));

        CompletarCommand = new RelayCommand(Completar,
            () => SelectedItem is { } e && _servicio.PuedeCompletar(e) && _sesionActual.Puede(Permisos.Operaciones.Completar));

        RechazarCommand = new RelayCommand(Rechazar,
            () => SelectedItem is { } e && _servicio.PuedeRechazar(e) && _sesionActual.Puede(Permisos.Operaciones.Rechazar));

        ReintentarCommand = new RelayCommand(Reintentar,
            () => SelectedItem is { } e && _servicio.PuedeReintentar(e) && _sesionActual.Puede(Permisos.Operaciones.Reintentar));

        VerDetalleCommand = new RelayCommand(
            () => _dialogos.MostrarEditor(new EtapaDetalleViewModel(SelectedItem!)),
            () => SelectedItem is not null);
    }

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand IniciarCommand { get; }
    public ICommand CompletarCommand { get; }
    public ICommand RechazarCommand { get; }
    public ICommand ReintentarCommand { get; }

    /// <summary>
    /// Solo lectura, así que no lleva permiso propio: quien ve el listado ya ve el mismo dato
    /// resumido en la grilla; esto solo lo desglosa por empleado.
    /// </summary>
    public ICommand VerDetalleCommand { get; }

    protected override string ModuloPermiso => "Etapas";

    protected override bool CoincideBusqueda(Etapa item, string texto) =>
        item.ProcesoEtiqueta.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.TipoTexto.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Notas.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(Etapa item) => _filtroEstado switch
    {
        "Pendientes" => item.Estado == EstadoEtapa.Pendiente,
        "En proceso" => item.Estado == EstadoEtapa.EnProceso,
        "Completadas" => item.Estado == EstadoEtapa.Completada,
        "Rechazadas" => item.Estado == EstadoEtapa.Rechazada,
        _ => true
    };

    protected override bool PuedeEditar(Etapa item) => _servicio.PuedeEditar(item);

    protected override bool PuedeEliminar(Etapa item) => _servicio.PuedeEliminar(item);

    protected override Etapa CrearNuevo() => new()
    {
        Estado = EstadoEtapa.Pendiente,
        CreadoPorId = _sesionActual.UsuarioActual?.Id ?? 0,
        FechaCreacion = DateTime.Now
    };

    protected override CrudEditorViewModelBase<Etapa> CrearEditor(Etapa item) =>
        new EtapaEditorViewModel(item, _procesos, _empleados, _inventario.ActivosConExistencia(), _servicio);

    private void Iniciar() => Aplicar(() => _servicio.Iniciar(SelectedItem!));

    /// <summary>
    /// Un solo camino sin importar cuántos empleados tenga la etapa: la lista de procesadas, más
    /// las líneas de merma que se hayan capturado (0 a N, cualquier empleado y artículo).
    /// </summary>
    private void Completar()
    {
        if (SelectedItem is not { } etapa)
            return;

        var editor = new CompletarEtapaViewModel(etapa, _inventario.ActivosConExistencia(),
                                                 _servicio.BotellasDisponiblesParaCompletar(etapa));
        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.Completar(etapa, editor.ObtenerProcesados(), editor.ObtenerMermas()));
    }

    private void Reintentar() => Aplicar(() => _servicio.Reintentar(SelectedItem!));

    private void Rechazar()
    {
        if (SelectedItem is not { } etapa)
            return;

        var editor = new MotivoEditorViewModel(
            $"Rechazar etapa {etapa.TipoTexto}",
            $"{etapa.ProcesoEtiqueta} — {etapa.EmpleadosTexto}",
            "Motivo del rechazo",
            "Indique el motivo del rechazo.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.Rechazar(etapa, editor.Motivo));
    }

    /// <summary>
    /// La lista la repuebla la recarga que dispara la escritura del servicio; aquí solo se
    /// apunta qué etapa dejar seleccionada.
    /// </summary>
    private void Aplicar(Func<Etapa> transicion)
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
/// Operaciones · Procesos: los procesos en planta y sus etapas en una pantalla conmutable,
/// mismo patrón que Finanzas · Cuentas por Pagar.
/// </summary>
public sealed class OperacionesViewModel : PantallaViewModelBase
{
    public const string VistaProcesos = "Procesos";
    public const string VistaEtapas = "Etapas";

    public OperacionesViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private OperacionesViewModel(Modulo modulo,
                                 Submodulo submodulo,
                                 IServicioDialogo dialogos,
                                 ISesionActual sesion)
        : base(modulo, submodulo)
    {
        var procesos = DataSourceFactory.CrearProcesos();
        var etapas = DataSourceFactory.CrearEtapas();
        var empleados = DataSourceFactory.CrearEmpleados();
        var tiposBotella = DataSourceFactory.CrearTiposBotella();

        Procesos = new ProcesosCrudViewModel(procesos, tiposBotella, etapas, dialogos, sesion);
        Etapas = new EtapasCrudViewModel(etapas, procesos, empleados, dialogos, sesion);

        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public ProcesosCrudViewModel Procesos { get; }
    public EtapasCrudViewModel Etapas { get; }

    /// <summary>
    /// Los dos listados, aunque solo se vea uno: agregar un proceso desde la vista de etapas
    /// tiene que dejarlo disponible al conmutar, sin salir y volver a entrar.
    /// </summary>
    public override void Recargar()
    {
        Procesos.Recargar();
        Etapas.Recargar();
    }

    private string _vistaActual = VistaProcesos;
    public string VistaActual
    {
        get => _vistaActual;
        set
        {
            if (SetProperty(ref _vistaActual, value))
                OnTodasLasPropiedadesCambiaron();
        }
    }

    public bool MostrarProcesos => VistaActual == VistaProcesos;
    public bool MostrarEtapas => VistaActual == VistaEtapas;

    public bool EsProcesos
    {
        get => MostrarProcesos;
        set { if (value) VistaActual = VistaProcesos; }
    }

    public bool EsEtapas
    {
        get => MostrarEtapas;
        set { if (value) VistaActual = VistaEtapas; }
    }

    public ICommand CambiarVistaCommand { get; }
}
