using System;
using System.Collections.Generic;
using System.Linq;
using ASO_RTR.Desktop.Models;
using ASO_RTR.Desktop.Services;

namespace ASO_RTR.Desktop.ViewModels;

/// <summary>Alta/edición de un proceso: el tipo de botella (del catálogo) que se va a trabajar.</summary>
public sealed class ProcesoEditorViewModel : CrudEditorViewModelBase<Proceso>
{
    private readonly Proceso _original;

    public ProcesoEditorViewModel(Proceso original, ITipoBotellaDataSource tiposBotella)
    {
        _original = original;

        TiposBotella = tiposBotella.GetAll().Where(t => t.Activo).OrderBy(t => t.Nombre).ToList();
        TipoBotellaSeleccionado = TiposBotella.FirstOrDefault(t => t.Id == original.TipoBotellaId);

        FechaCreacion = original.FechaCreacion == default ? DateTime.Today : original.FechaCreacion;
        Notas = original.Notas;
    }

    public override string Titulo => _original.Id == 0 ? "Nuevo proceso" : $"Editar proceso Nº {_original.Id}";

    public IReadOnlyList<TipoBotella> TiposBotella { get; }

    private TipoBotella? _tipoBotellaSeleccionado;
    public TipoBotella? TipoBotellaSeleccionado
    {
        get => _tipoBotellaSeleccionado;
        set => SetProperty(ref _tipoBotellaSeleccionado, value);
    }

    private DateTime _fechaCreacion = DateTime.Today;
    public DateTime FechaCreacion
    {
        get => _fechaCreacion;
        set => SetProperty(ref _fechaCreacion, value);
    }

    private string _notas = string.Empty;
    public string Notas
    {
        get => _notas;
        set => SetProperty(ref _notas, value);
    }

    protected override bool Validar(out string? error)
    {
        if (TipoBotellaSeleccionado is null)
        {
            error = "Seleccione el tipo de botella que se va a procesar.";
            return false;
        }

        error = null;
        return true;
    }

    public override Proceso ObtenerResultado()
    {
        var proceso = _original.Clonar();
        proceso.TipoBotellaId = TipoBotellaSeleccionado?.Id ?? 0;
        proceso.TipoBotellaEtiqueta = TipoBotellaSeleccionado?.Etiqueta ?? string.Empty;
        proceso.FechaCreacion = FechaCreacion;
        proceso.Notas = Notas.Trim();
        return proceso;
    }
}
