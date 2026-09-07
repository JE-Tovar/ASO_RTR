using System;
using System.Windows.Input;
using ASO_RTR.Desktop.Configuration;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Navigation;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>
/// Catálogo · Tipos de Botella: registro del producto que maneja la empresa. Listado único
/// (sin conmutador de padrones), igual que Nómina · Empleados.
/// </summary>
public sealed class TiposBotellaViewModel : PantallaCrudViewModel<TipoBotella, int>
{
    private readonly ITipoBotellaDataSource _tiposBotella;
    private readonly IServicioDialogo _dialogos;

    public TiposBotellaViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearTiposBotella(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private TiposBotellaViewModel(Modulo modulo,
                                  Submodulo submodulo,
                                  ITipoBotellaDataSource tiposBotella,
                                  IServicioDialogo dialogos,
                                  ISesionActual sesion)
        : base(modulo, submodulo, tiposBotella, dialogos, sesion)
    {
        _tiposBotella = tiposBotella;
        _dialogos = dialogos;

        VerDetalleCommand = new RelayCommand(
            () => _dialogos.MostrarEditor(new TipoBotellaDetalleViewModel(SelectedItem!)),
            () => SelectedItem is not null);
    }

    /// <summary>
    /// Solo lectura, así que no lleva permiso propio: quien ve el listado (Ver.Catalogo.TiposBotella)
    /// ya ve el mismo dato resumido en la grilla; esto solo lo desglosa por nivel.
    /// </summary>
    public ICommand VerDetalleCommand { get; }

    protected override string ModuloPermiso => "TiposBotella";

    protected override bool CoincideBusqueda(TipoBotella item, string texto) =>
        item.Marca.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Medida.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override TipoBotella CrearNuevo() => new() { Activo = true };

    protected override CrudEditorViewModelBase<TipoBotella> CrearEditor(TipoBotella item) =>
        new TipoBotellaEditorViewModel(item, _tiposBotella);
}
