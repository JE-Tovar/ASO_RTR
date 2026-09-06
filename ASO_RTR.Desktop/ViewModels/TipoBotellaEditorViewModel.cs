using System.Collections.Generic;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>Una opción del combo de presentación: el valor del enum y su texto en español.</summary>
public sealed record OpcionPresentacion(PresentacionEmpaque Valor, string Texto);

/// <summary>
/// Alta/edición de un tipo de botella. <see cref="UnidadesPorNivel"/> cambia de significado
/// según la presentación (cajas por nivel si es Caja, botellas por nivel si es Bulk) — por eso
/// su etiqueta en el formulario se calcula, no se escribe dos veces.
/// </summary>
public sealed class TipoBotellaEditorViewModel : CrudEditorViewModelBase<TipoBotella>
{
    private readonly TipoBotella _original;

    public TipoBotellaEditorViewModel(TipoBotella original, ITipoBotellaDataSource tiposBotella)
    {
        _original = original;

        Presentaciones =
        [
            new OpcionPresentacion(PresentacionEmpaque.Caja, "Por caja"),
            new OpcionPresentacion(PresentacionEmpaque.Bulk, "A granel (bulk)")
        ];

        Marca = original.Marca;
        Nombre = original.Nombre;
        Medida = original.Medida;
        PresentacionSeleccionada = original.Id == 0 ? PresentacionEmpaque.Caja : original.Presentacion;
        BotellasPorCaja = original.BotellasPorCaja == 0 ? string.Empty : original.BotellasPorCaja.ToString();
        UnidadesPorNivel = original.UnidadesPorNivel == 0 ? string.Empty : original.UnidadesPorNivel.ToString();
        Niveles = original.Niveles == 0 ? string.Empty : original.Niveles.ToString();
        Activo = original.Activo;
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo tipo de botella" : $"Editar tipo de botella Nº {_original.Id}";

    public IReadOnlyList<OpcionPresentacion> Presentaciones { get; }

    private string _marca = string.Empty;
    public string Marca
    {
        get => _marca;
        set => SetProperty(ref _marca, value);
    }

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    private string _medida = string.Empty;
    public string Medida
    {
        get => _medida;
        set => SetProperty(ref _medida, value);
    }

    private PresentacionEmpaque _presentacionSeleccionada;
    public PresentacionEmpaque PresentacionSeleccionada
    {
        get => _presentacionSeleccionada;
        set
        {
            if (SetProperty(ref _presentacionSeleccionada, value))
            {
                OnPropertyChanged(nameof(EsCaja));
                OnPropertyChanged(nameof(EtiquetaUnidadesPorNivel));
            }
        }
    }

    /// <summary>Muestra u oculta "Botellas por caja" en el formulario.</summary>
    public bool EsCaja => PresentacionSeleccionada == PresentacionEmpaque.Caja;

    public string EtiquetaUnidadesPorNivel => EsCaja ? "Cajas por nivel" : "Botellas por nivel";

    private string _botellasPorCaja = string.Empty;
    public string BotellasPorCaja
    {
        get => _botellasPorCaja;
        set => SetProperty(ref _botellasPorCaja, value);
    }

    private string _unidadesPorNivel = string.Empty;
    public string UnidadesPorNivel
    {
        get => _unidadesPorNivel;
        set => SetProperty(ref _unidadesPorNivel, value);
    }

    private string _niveles = string.Empty;
    public string Niveles
    {
        get => _niveles;
        set => SetProperty(ref _niveles, value);
    }

    private bool _activo = true;
    public bool Activo
    {
        get => _activo;
        set => SetProperty(ref _activo, value);
    }

    protected override bool Validar(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Marca))
        {
            error = "Indique la marca de la botella.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            error = "Indique el nombre del tipo de botella.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Medida))
        {
            error = "Indique la medida de la botella.";
            return false;
        }

        if (EsCaja && (!int.TryParse(BotellasPorCaja, out var botellasPorCaja) || botellasPorCaja <= 0))
        {
            error = "Las botellas por caja deben ser un número mayor que cero.";
            return false;
        }

        if (!int.TryParse(UnidadesPorNivel, out var unidadesPorNivel) || unidadesPorNivel <= 0)
        {
            error = $"{EtiquetaUnidadesPorNivel} debe ser un número mayor que cero.";
            return false;
        }

        if (!int.TryParse(Niveles, out var niveles) || niveles <= 0)
        {
            error = "Los niveles de la paleta deben ser un número mayor que cero.";
            return false;
        }

        error = null;
        return true;
    }

    public override TipoBotella ObtenerResultado()
    {
        var tipoBotella = _original.Clonar();
        tipoBotella.Marca = Marca.Trim();
        tipoBotella.Nombre = Nombre.Trim();
        tipoBotella.Medida = Medida.Trim();
        tipoBotella.Presentacion = PresentacionSeleccionada;
        tipoBotella.BotellasPorCaja = EsCaja && int.TryParse(BotellasPorCaja, out var botellasPorCaja) ? botellasPorCaja : 0;
        tipoBotella.UnidadesPorNivel = int.TryParse(UnidadesPorNivel, out var unidadesPorNivel) ? unidadesPorNivel : 0;
        tipoBotella.Niveles = int.TryParse(Niveles, out var niveles) ? niveles : 0;
        tipoBotella.Activo = Activo;
        return tipoBotella;
    }
}
