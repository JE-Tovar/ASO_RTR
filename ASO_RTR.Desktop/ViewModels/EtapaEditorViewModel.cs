using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>
/// Una fila del checklist de empleados: envuelve al empleado con el booleano de selección, sin
/// tocar el modelo — mismo mecanismo que la fila de permiso en <c>AdministracionViewModel</c>.
/// </summary>
public sealed class EmpleadoSeleccionable : ViewModelBase
{
    public EmpleadoSeleccionable(Empleado empleado, bool seleccionado)
    {
        Empleado = empleado;
        _seleccionado = seleccionado;
    }

    public Empleado Empleado { get; }

    private bool _seleccionado;
    public bool Seleccionado
    {
        get => _seleccionado;
        set => SetProperty(ref _seleccionado, value);
    }
}

/// <summary>
/// Una línea de "consumo de materiales por botella": qué artículo y cuántas unidades gasta la
/// etapa por cada botella procesada. Se agrega/quita dinámicamente, igual que las líneas de
/// <see cref="EntradaEditorViewModel"/>.
/// </summary>
public sealed class ConsumoLineaViewModel(IReadOnlyList<Articulo> articulos) : ViewModelBase
{
    public IReadOnlyList<Articulo> Articulos { get; } = articulos;

    private Articulo? _articulo;
    public Articulo? Articulo
    {
        get => _articulo;
        set => SetProperty(ref _articulo, value);
    }

    private string _unidadesPorBotella = string.Empty;
    public string UnidadesPorBotella
    {
        get => _unidadesPorBotella;
        set => SetProperty(ref _unidadesPorBotella, value);
    }
}

/// <summary>
/// Alta/edición de una etapa: a qué proceso pertenece, de qué tipo es, y quiénes la ejecutaron.
/// La validación de fondo (proceso obligatorio, al menos un empleado) la hace
/// <see cref="EtapaService"/>: el editor se la pide y muestra el mensaje que devuelva.
/// </summary>
/// <summary>Una opción del combo de tipo de etapa: el valor del enum y su texto en español.</summary>
public sealed record OpcionTipoEtapa(TipoEtapa Valor, string Texto);

public sealed class EtapaEditorViewModel : CrudEditorViewModelBase<Etapa>
{
    private readonly Etapa _original;
    private readonly EtapaService _servicio;

    public EtapaEditorViewModel(Etapa original,
                                IProcesoDataSource procesos,
                                IEmpleadoDataSource empleados,
                                IReadOnlyList<Articulo> articulos,
                                EtapaService servicio)
    {
        _original = original;
        _servicio = servicio;

        Procesos = procesos.GetAll().OrderByDescending(p => p.Id).ToList();
        ProcesoSeleccionado = Procesos.FirstOrDefault(p => p.Id == original.ProcesoId);

        TiposEtapa =
        [
            new OpcionTipoEtapa(TipoEtapa.Recepcion, "Recepción"),
            new OpcionTipoEtapa(TipoEtapa.ControlCalidad, "Control de calidad"),
            new OpcionTipoEtapa(TipoEtapa.Limpieza, "Limpieza"),
            new OpcionTipoEtapa(TipoEtapa.Empacado, "Empacado"),
            new OpcionTipoEtapa(TipoEtapa.Etiquetado, "Etiquetado"),
            new OpcionTipoEtapa(TipoEtapa.Despacho, "Despacho")
        ];
        TipoSeleccionado = original.Id == 0 ? TipoEtapa.Recepcion : original.Tipo;

        Empleados = empleados.GetAll()
            .Where(e => e.Activo)
            .OrderBy(e => e.NombreCompleto)
            .Select(e => new EmpleadoSeleccionable(e, original.Empleados.Any(x => x.EmpleadoId == e.Id)))
            .ToList();

        Notas = original.Notas;

        Consumos =
        [
            .. original.Consumos.Select(c => new ConsumoLineaViewModel(articulos)
            {
                Articulo = articulos.FirstOrDefault(a => a.Id == c.ArticuloId),
                UnidadesPorBotella = c.UnidadesPorBotella.ToString()
            })
        ];

        AgregarConsumoCommand = new RelayCommand(() => Consumos.Add(new ConsumoLineaViewModel(articulos)));
        QuitarConsumoCommand = new RelayCommand<ConsumoLineaViewModel>(linea =>
        {
            if (linea is not null)
                Consumos.Remove(linea);
        });
    }

    public ObservableCollection<ConsumoLineaViewModel> Consumos { get; }

    public ICommand AgregarConsumoCommand { get; }
    public ICommand QuitarConsumoCommand { get; }

    public override string Titulo => _original.Id == 0 ? "Nueva etapa" : $"Editar etapa Nº {_original.Id}";

    public override double AnchoEditor => Ancho.Estandar;

    public IReadOnlyList<Proceso> Procesos { get; }

    private Proceso? _procesoSeleccionado;
    public Proceso? ProcesoSeleccionado
    {
        get => _procesoSeleccionado;
        set => SetProperty(ref _procesoSeleccionado, value);
    }

    public IReadOnlyList<OpcionTipoEtapa> TiposEtapa { get; }

    private TipoEtapa _tipoSeleccionado;
    public TipoEtapa TipoSeleccionado
    {
        get => _tipoSeleccionado;
        set => SetProperty(ref _tipoSeleccionado, value);
    }

    public IReadOnlyList<EmpleadoSeleccionable> Empleados { get; }

    private string _notas = string.Empty;
    public string Notas
    {
        get => _notas;
        set => SetProperty(ref _notas, value);
    }

    protected override bool Validar(out string? error) => _servicio.Validar(Construir(), out error);

    public override Etapa ObtenerResultado() => Construir();

    private Etapa Construir()
    {
        var etapa = _original.Clonar();

        etapa.ProcesoId = ProcesoSeleccionado?.Id ?? 0;
        etapa.ProcesoEtiqueta = ProcesoSeleccionado?.Etiqueta ?? string.Empty;
        etapa.Tipo = TipoSeleccionado;
        etapa.Notas = Notas.Trim();
        etapa.Empleados = Empleados
            .Where(e => e.Seleccionado)
            .Select(e => new EtapaEmpleado { EmpleadoId = e.Empleado.Id, EmpleadoNombre = e.Empleado.NombreCompleto })
            .ToList();

        // Las líneas en blanco no se mandan: una fila vacía al final es lo normal mientras se
        // arma la lista, y no tiene por qué impedir guardar.
        etapa.Consumos =
        [
            .. Consumos
                .Where(c => c.Articulo is not null)
                .Select(c => new ConsumoEtapa
                {
                    ArticuloId = c.Articulo!.Id,
                    ArticuloCodigo = c.Articulo.Codigo,
                    ArticuloNombre = c.Articulo.Nombre,
                    UnidadTexto = c.Articulo.UnidadCorta,
                    UnidadesPorBotella = decimal.TryParse(c.UnidadesPorBotella, out var v) ? v : 0
                })
        ];

        return etapa;
    }
}
