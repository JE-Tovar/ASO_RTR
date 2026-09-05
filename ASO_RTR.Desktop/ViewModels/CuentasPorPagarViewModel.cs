using System;
using System.Windows.Input;
using ASO_RTR.Desktop.Configuration;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Navigation;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>
/// Facturas de compra pendientes de pago. Sub-listado de Finanzas · Cuentas por Pagar; el
/// encabezado y el conmutador los pone <see cref="CuentasPorPagarViewModel"/>.
/// </summary>
public sealed class FacturasProveedorCrudViewModel : CrudViewModelBase<FacturaProveedor, int>
{
    private const string FiltroTodas = "Todas";

    private readonly IProveedorDataSource _proveedores;
    private readonly IServicioDialogo _dialogos;
    private readonly ISesionActual _sesionActual;
    private readonly CuentasPorPagarService _servicio;
    private readonly BancoService _banco;

    private string _filtroEstado = FiltroTodas;

    public FacturasProveedorCrudViewModel(IFacturaProveedorDataSource facturas,
                                          IProveedorDataSource proveedores,
                                          IServicioDialogo dialogos,
                                          ISesionActual sesion)
        : base(facturas, dialogos, sesion)
    {
        _proveedores = proveedores;
        _dialogos = dialogos;
        _sesionActual = sesion;
        _banco = new BancoService(DataSourceFactory.CrearMovimientosBanco(),
                                  DataSourceFactory.CrearCuentasBancarias(), sesion);
        _servicio = new CuentasPorPagarService(facturas, _banco, sesion);

        CambiarFiltroEstadoCommand = new RelayCommand<string>(filtro =>
        {
            _filtroEstado = filtro;
            ItemsView.Refresh();
        });

        RegistrarPagoCommand = new RelayCommand(RegistrarPago,
            () => SelectedItem is { } f && _servicio.PuedeRegistrarPago(f) && _sesionActual.Puede("Finanzas.Pagar"));

        AnularCommand = new RelayCommand(Anular,
            () => SelectedItem is { } f && _servicio.PuedeAnular(f) && _sesionActual.Puede("Finanzas.Anular"));
    }

    public ICommand CambiarFiltroEstadoCommand { get; }
    public ICommand RegistrarPagoCommand { get; }
    public ICommand AnularCommand { get; }

    /// <summary>Estado de la deuda, visible sin ir al dashboard del módulo.</summary>
    public string ResumenDeuda =>
        $"Por pagar {_servicio.TotalPorPagar():N2} · vencido {_servicio.TotalVencido():N2}";

    protected override string ModuloPermiso => "FacturasProveedor";

    protected override bool CoincideBusqueda(FacturaProveedor item, string texto) =>
        item.ProveedorNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.NumeroDocumento.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Descripcion.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override bool PasaFiltroExtra(FacturaProveedor item) => _filtroEstado switch
    {
        "Pendientes" => item.Estado == EstadoFacturaProveedor.Pendiente,
        "Vencidas" => item.EstaVencida,
        "Pagadas" => item.Estado == EstadoFacturaProveedor.Pagada,
        "Anuladas" => item.Estado == EstadoFacturaProveedor.Anulada,
        _ => true
    };

    protected override bool PuedeEditar(FacturaProveedor item) => _servicio.PuedeEditar(item);

    protected override bool PuedeEliminar(FacturaProveedor item) => _servicio.PuedeEliminar(item);

    protected override FacturaProveedor CrearNuevo() => new()
    {
        FechaEmision = DateTime.Today,
        FechaVencimiento = DateTime.Today.AddDays(30),
        Estado = EstadoFacturaProveedor.Pendiente,
        CreadoPorId = _sesionActual.UsuarioActual?.Id ?? 0,
        FechaCreacion = DateTime.Now
    };

    protected override CrudEditorViewModelBase<FacturaProveedor> CrearEditor(FacturaProveedor item) =>
        new FacturaProveedorEditorViewModel(item, _proveedores, _servicio);

    /// <summary>
    /// Pregunta de qué cuenta salió el dinero y lo anota en el libro de banco, además de dar la
    /// factura por pagada. Ver <see cref="CuentasPorCobrarViewModel"/> para por qué sustituyó al
    /// Confirmar de sí/no que había antes.
    /// </summary>
    private void RegistrarPago()
    {
        if (SelectedItem is not { } factura)
            return;

        var editor = new AsientoBancoEditorViewModel(
            $"Registrar pago de la factura Nº {factura.NumeroDocumento}",
            $"{factura.ProveedorNombre} — {factura.Descripcion}",
            factura.Monto,
            esEntrada: false,
            _banco.CuentasActivas(),
            "Registrar pago");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.RegistrarPago(factura, editor.Resultado,
                                              _sesionActual.UsuarioActual?.Id ?? 0));
    }

    private void Anular()
    {
        if (SelectedItem is not { } factura)
            return;

        var editor = new MotivoEditorViewModel(
            $"Anular factura {factura.NumeroDocumento}",
            $"{factura.ProveedorNombre} — {factura.Descripcion} — {factura.MontoTexto}",
            "Motivo de la anulación",
            "Indique el motivo de la anulación.");

        if (!_dialogos.MostrarEditor(editor))
            return;

        Aplicar(() => _servicio.Anular(factura, editor.Motivo));
    }

    /// <summary>
    /// La lista la repuebla la recarga que dispara la escritura del servicio; aquí solo se
    /// apunta qué factura dejar seleccionada.
    /// </summary>
    private void Aplicar(Func<FacturaProveedor> transicion)
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

/// <summary>Maestro de proveedores. Sub-listado de Finanzas · Cuentas por Pagar.</summary>
public sealed class ProveedoresCrudViewModel : CrudViewModelBase<Proveedor, int>
{
    private readonly IProveedorDataSource _proveedores;

    public ProveedoresCrudViewModel(IProveedorDataSource proveedores,
                                    IServicioDialogo dialogos,
                                    ISesionActual sesion)
        : base(proveedores, dialogos, sesion)
    {
        _proveedores = proveedores;
    }

    protected override string ModuloPermiso => "Proveedores";

    protected override bool CoincideBusqueda(Proveedor item, string texto) =>
        item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Rif.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Telefono.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Proveedor CrearNuevo() => new() { Activo = true };

    protected override CrudEditorViewModelBase<Proveedor> CrearEditor(Proveedor item) =>
        new ProveedorEditorViewModel(item, _proveedores);
}

/// <summary>
/// Finanzas · Cuentas por Pagar: las facturas de compra y el maestro de proveedores en una
/// pantalla conmutable, con el mismo patrón que Nómina · Empleados.
/// </summary>
public sealed class CuentasPorPagarViewModel : PantallaViewModelBase
{
    public const string VistaFacturas = "Facturas";
    public const string VistaProveedores = "Proveedores";

    public CuentasPorPagarViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private CuentasPorPagarViewModel(Modulo modulo,
                                     Submodulo submodulo,
                                     IServicioDialogo dialogos,
                                     ISesionActual sesion)
        : base(modulo, submodulo)
    {
        var proveedores = DataSourceFactory.CrearProveedores();

        Facturas = new FacturasProveedorCrudViewModel(
            DataSourceFactory.CrearFacturasProveedor(), proveedores, dialogos, sesion);

        Proveedores = new ProveedoresCrudViewModel(proveedores, dialogos, sesion);

        CambiarVistaCommand = new RelayCommand<string>(vista => VistaActual = vista);
    }

    public FacturasProveedorCrudViewModel Facturas { get; }
    public ProveedoresCrudViewModel Proveedores { get; }

    /// <summary>
    /// Los dos listados, aunque solo se vea uno: dar de alta un proveedor desde la vista de
    /// facturas tiene que dejarlo disponible al conmutar, sin salir y volver a entrar.
    /// </summary>
    public override void Recargar()
    {
        Facturas.Recargar();
        Proveedores.Recargar();
    }

    private string _vistaActual = VistaFacturas;
    public string VistaActual
    {
        get => _vistaActual;
        set
        {
            // Notifica todas: enumerar aqui un OnPropertyChanged por cada Mostrar… es
            // la lista que se queda corta el dia que se agrega un padron mas.
            if (SetProperty(ref _vistaActual, value))
                OnTodasLasPropiedadesCambiaron();
        }
    }

    public bool MostrarFacturas => VistaActual == VistaFacturas;
    public bool MostrarProveedores => VistaActual == VistaProveedores;

    /// <summary>
    /// Las dos pestañas, enlazadas en DOS VÍAS al <c>IsChecked</c> de su botón, como en
    /// <see cref="AdministracionViewModel"/>. Antes la selección viajaba solo de la vista al
    /// ViewModel por <c>Command</c>, con <c>IsChecked="True"</c> a fuego en la primera: si algo
    /// cambiaba <see cref="VistaActual"/> desde el código, los botones no se enteraban.
    ///
    /// El setter solo actúa al marcar: al desmarcar ya hay otro botón del grupo encendiéndose.
    /// </summary>
    public bool EsFacturas
    {
        get => MostrarFacturas;
        set { if (value) VistaActual = VistaFacturas; }
    }

    public bool EsProveedores
    {
        get => MostrarProveedores;
        set { if (value) VistaActual = VistaProveedores; }
    }

    public ICommand CambiarVistaCommand { get; }
}
