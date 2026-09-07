using System;
using System.Linq;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>
/// Alta/edición de un empleado. La cédula identifica a la persona, así que se valida que no
/// se repita: dos fichas de la misma persona partirían su historial en dos.
/// </summary>
public sealed class EmpleadoEditorViewModel : CrudEditorViewModelBase<Empleado>
{
    private readonly Empleado _original;
    private readonly IEmpleadoDataSource _empleados;

    public EmpleadoEditorViewModel(Empleado original, IEmpleadoDataSource empleados)
    {
        _original = original;
        _empleados = empleados;

        Cedula = original.Cedula;
        NombreCompleto = original.NombreCompleto;
        Cargo = original.Cargo;
        FechaIngreso = original.FechaIngreso;
        Telefono = original.Telefono;
        Activo = original.Activo;
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo empleado" : $"Editar empleado Nº {_original.Id}";

    private string _cedula = string.Empty;
    public string Cedula
    {
        get => _cedula;
        set => SetProperty(ref _cedula, value);
    }

    private string _nombreCompleto = string.Empty;
    public string NombreCompleto
    {
        get => _nombreCompleto;
        set => SetProperty(ref _nombreCompleto, value);
    }

    private string _cargo = string.Empty;
    public string Cargo
    {
        get => _cargo;
        set => SetProperty(ref _cargo, value);
    }

    private DateTime _fechaIngreso = DateTime.Today;
    public DateTime FechaIngreso
    {
        get => _fechaIngreso;
        set => SetProperty(ref _fechaIngreso, value);
    }

    private string _telefono = string.Empty;
    public string Telefono
    {
        get => _telefono;
        set => SetProperty(ref _telefono, value);
    }

    private bool _activo = true;
    public bool Activo
    {
        get => _activo;
        set => SetProperty(ref _activo, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(NombreCompleto))
        {
            error = "Indique el nombre completo del empleado.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Cedula))
        {
            error = "Indique la cédula del empleado.";
            return false;
        }

        var repetido = _empleados.GetAll()
            .Any(e => e.Id != _original.Id
                      && string.Equals(e.Cedula.Trim(), Cedula.Trim(), StringComparison.OrdinalIgnoreCase));

        if (repetido)
        {
            error = $"Ya existe un empleado con la cédula {Cedula.Trim()}.";
            return false;
        }

        error = null;
        return true;
    }

    public override Empleado ObtenerResultado()
    {
        var empleado = _original.Clonar();
        empleado.Cedula = Cedula.Trim();
        empleado.NombreCompleto = NombreCompleto.Trim();
        empleado.Cargo = Cargo.Trim();
        empleado.FechaIngreso = FechaIngreso;
        empleado.Telefono = Telefono.Trim();
        empleado.Activo = Activo;
        return empleado;
    }
}
