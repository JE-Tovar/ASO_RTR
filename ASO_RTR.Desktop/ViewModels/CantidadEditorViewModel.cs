namespace ASO_RTR.Desktop.ViewModels;

/// <summary>
/// Editor genérico de "pedir un número": botellas procesadas al completar una etapa, o
/// cualquier otro cierre que necesite una cantidad. Igual que <see cref="MotivoEditorViewModel"/>
/// pide un texto, este pide un entero, con un solo editor reutilizable en vez de uno por pantalla.
/// </summary>
public sealed class CantidadEditorViewModel : CrudEditorViewModelBase
{
    private readonly string _titulo;
    private readonly int _minimo;

    /// <param name="titulo">Encabezado de la ventana, p. ej. "Completar etapa: Limpieza".</param>
    /// <param name="descripcion">Resumen de lo que se está cerrando, para que se vea qué se está firmando.</param>
    /// <param name="etiquetaCantidad">Rótulo del campo, p. ej. "Botellas procesadas exitosamente".</param>
    /// <param name="minimo">Valor mínimo aceptado (inclusive).</param>
    public CantidadEditorViewModel(string titulo,
                                   string descripcion,
                                   string etiquetaCantidad = "Cantidad",
                                   int minimo = 0)
    {
        _titulo = titulo;
        _minimo = minimo;
        Descripcion = descripcion;
        EtiquetaCantidad = etiquetaCantidad;
    }

    public override string Titulo => _titulo;

    public string Descripcion { get; }
    public string EtiquetaCantidad { get; }

    private string _texto = string.Empty;
    public string Texto
    {
        get => _texto;
        set => SetProperty(ref _texto, value);
    }

    /// <summary>Resultado, válido solo después de un <c>Guardar</c> exitoso.</summary>
    public int Valor { get; private set; }

    protected override bool Validar(out string? error)
    {
        if (!int.TryParse(Texto, out var valor) || valor < _minimo)
        {
            error = $"Indique un número entero mayor o igual a {_minimo}.";
            return false;
        }

        Valor = valor;
        error = null;
        return true;
    }
}
