using System;
using ASO_RTR.Desktop.Configuration;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Navigation;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>
/// Nómina · Empleados: registro del personal de la planta. Listado único (sin conmutador de
/// padrones), a diferencia de Finanzas · Cuentas por Pagar.
/// </summary>
public sealed class EmpleadosViewModel : PantallaCrudViewModel<Empleado, int>
{
    private readonly IEmpleadoDataSource _empleados;

    public EmpleadosViewModel(Modulo modulo, Submodulo submodulo)
        : this(modulo, submodulo, DataSourceFactory.CrearEmpleados(), new ServicioDialogo(), SesionActual.Instancia)
    {
    }

    private EmpleadosViewModel(Modulo modulo,
                               Submodulo submodulo,
                               IEmpleadoDataSource empleados,
                               IServicioDialogo dialogos,
                               ISesionActual sesion)
        : base(modulo, submodulo, empleados, dialogos, sesion)
    {
        _empleados = empleados;
    }

    protected override string ModuloPermiso => "Empleados";

    protected override bool CoincideBusqueda(Empleado item, string texto) =>
        item.NombreCompleto.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Cedula.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || item.Cargo.Contains(texto, StringComparison.OrdinalIgnoreCase);

    protected override Empleado CrearNuevo() => new() { Activo = true, FechaIngreso = DateTime.Today, Cargo = "Operador" };

    protected override CrudEditorViewModelBase<Empleado> CrearEditor(Empleado item) =>
        new EmpleadoEditorViewModel(item, _empleados);
}
