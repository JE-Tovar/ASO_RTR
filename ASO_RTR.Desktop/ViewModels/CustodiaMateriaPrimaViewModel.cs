using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ASO_RTR.Desktop.Configuration;
using ASO_RTR.Desktop.Navigation;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>
/// Una fila del padrón de custodia: cuántas paletas de este tipo de botella tiene RTR en planta
/// ahora mismo, a cuántas botellas equivalen con el patrón de paletizado vigente, y de esas
/// cuántas ya tomó algún proceso de Operaciones (ver <see cref="CustodiaProduccionService"/>) —
/// la respuesta a "cuántas botellas están procesadas y cuántas no" que motivó esta columna.
/// </summary>
public sealed record ItemCustodia(int TipoBotellaId, string Etiqueta, bool Activo, int Paletas, int Botellas,
                                  int BotellasTomadas, int BotellasDisponibles)
{
    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
    public string PaletasTexto => $"{Paletas}";
    public string BotellasTexto => $"{Botellas}";
    public string BotellasTomadasTexto => $"{BotellasTomadas}";
    public string BotellasDisponiblesTexto => $"{BotellasDisponibles}";
}

/// <summary>
/// Materia Prima · Custodia: cuánto hay de cada tipo de botella, de un vistazo.
///
/// No hereda de <see cref="PantallaCrudViewModel{T, TId}"/> porque no hay nada que crear, editar
/// ni eliminar aquí: es un reporte derivado, igual de espíritu que <see cref="AlmacenViewModel"/>
/// frente a Entradas/Salidas, pero sin ningún padrón propio que mantener — arma su propia lista
/// filtrable a mano, al estilo de <see cref="PeticionesViewModel"/>.
/// </summary>
public sealed class CustodiaMateriaPrimaViewModel : PantallaViewModelBase
{
    private const string FiltroTodos = "Todos";

    private readonly ITipoBotellaDataSource _tiposBotella;
    private readonly MateriaPrimaService _materiaPrima;
    private readonly CustodiaProduccionService _custodiaProduccion;

    private string _filtro = FiltroTodos;

    public CustodiaMateriaPrimaViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearTiposBotella(),
               DataSourceFactory.CrearProcesos(), DataSourceFactory.CrearEtapas())
    {
    }

    private CustodiaMateriaPrimaViewModel(Modulo modulo,
                                          Submodulo submodulo,
                                          ITipoBotellaDataSource tiposBotella,
                                          IProcesoDataSource procesos,
                                          IEtapaDataSource etapas)
        : base(modulo, submodulo)
    {
        _tiposBotella = tiposBotella;

        _materiaPrima = new MateriaPrimaService(DataSourceFactory.CrearRecepcionesMateriaPrima(),
                                                DataSourceFactory.CrearDespachosProductoTerminado());

        // Solo lectura: componer esto de más no cuesta nada, son fuentes sin estado. Ver
        // CustodiaProduccionService sobre por qué este cálculo vive del lado de Operaciones y no
        // dentro de MateriaPrimaService.
        _custodiaProduccion = new CustodiaProduccionService(procesos, etapas, tiposBotella, _materiaPrima);

        Items = [];
        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FiltrarItem;

        CambiarFiltroCommand = new RelayCommand<string>(filtro =>
        {
            _filtro = filtro;
            ItemsView.Refresh();
        });

        CargarItems();
    }

    public ObservableCollection<ItemCustodia> Items { get; }
    public ICollectionView ItemsView { get; }

    public ICommand CambiarFiltroCommand { get; }

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set { if (SetProperty(ref _textoBusqueda, value)) ItemsView.Refresh(); }
    }

    public int TotalPaletas => Items.Sum(i => i.Paletas);

    public string Resumen => $"{Items.Count(i => i.Activo)} tipos activos · {TotalPaletas} paletas en custodia";

    /// <summary>Vuelve a armar el padrón entero: es barato (dos tablas de documentos, un
    /// catálogo) y no hay nada que preservar entre recargas, a diferencia de un listado con
    /// selección.</summary>
    public override void Recargar()
    {
        CargarItems();
        OnPropertyChanged(nameof(Resumen));
        OnPropertyChanged(nameof(TotalPaletas));
    }

    private void CargarItems()
    {
        var existencias = _materiaPrima.ExistenciasPorTipoBotella();

        Items.Clear();

        // Activos e inactivos, igual que Almacén no esconde artículos inactivos: un tipo
        // desactivado que todavía tiene paletas en planta no debe desaparecer del padrón.
        foreach (var tipo in _tiposBotella.GetAll().OrderBy(t => t.Etiqueta))
        {
            var paletas = existencias.GetValueOrDefault(tipo.Id);
            var botellas = paletas * tipo.BotellasPorPaleta;
            var tomadas = _custodiaProduccion.BotellasTomadasPorProcesos(tipo.Id);

            Items.Add(new ItemCustodia(tipo.Id, tipo.Etiqueta, tipo.Activo, paletas, botellas,
                                       tomadas, botellas - tomadas));
        }

        ItemsView.Refresh();
    }

    private bool FiltrarItem(object obj)
        => obj is ItemCustodia item
           && (string.IsNullOrWhiteSpace(TextoBusqueda)
               || item.Etiqueta.Contains(TextoBusqueda.Trim(), StringComparison.OrdinalIgnoreCase))
           && PasaFiltroExtra(item);

    private bool PasaFiltroExtra(ItemCustodia item) => _filtro switch
    {
        "Con paletas" => item.Paletas > 0,
        "Sin paletas" => item.Paletas == 0,
        "Inactivos" => !item.Activo,
        _ => true
    };
}
