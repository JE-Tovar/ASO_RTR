using System;
using System.Collections.Generic;
using System.Linq;
using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.Services;

/// <summary>
/// Reglas de los despachos de producto terminado.
///
/// Más simple que su contraparte de recepciones: un despacho no le debe nada a nadie, así que no
/// arrastra ningún documento de otro módulo. Lo único que no puede pasar es que la custodia quede
/// en negativo, y esa es la regla que este servicio defiende.
/// </summary>
public sealed class DespachosService
{
    private const string Prefijo = "DES-";

    private readonly IDespachoProductoTerminadoDataSource _despachos;
    private readonly MateriaPrimaService _materiaPrima;
    private readonly ISesionActual _sesion;

    public DespachosService(IDespachoProductoTerminadoDataSource despachos,
                            MateriaPrimaService materiaPrima,
                            ISesionActual sesion)
    {
        _despachos = despachos;
        _materiaPrima = materiaPrima;
        _sesion = sesion;
    }

    // --- Reglas de transición (alimentan el CanExecute) ---

    public bool PuedeAnular(DespachoProductoTerminado despacho) => despacho.Estado == EstadoDespacho.Registrada;

    // --- Validación ---

    /// <summary>
    /// Valida el despacho contra la custodia REAL del momento, no contra la que tenía el
    /// formulario al abrirse: entre que se abre el modal y se guarda, otro puesto pudo haber
    /// despachado paletas. La pantalla avisa antes, pero quien manda es esta comprobación.
    /// </summary>
    public bool Validar(DespachoProductoTerminado despacho, out string? error)
    {
        if (despacho.Lineas.Count == 0)
        {
            error = "Agregue al menos un tipo de botella al despacho.";
            return false;
        }

        if (despacho.Lineas.Any(l => l.TipoBotellaId == 0))
        {
            error = "Hay una línea sin tipo de botella seleccionado.";
            return false;
        }

        if (despacho.Lineas.Any(l => l.CantidadPaletas <= 0))
        {
            error = "La cantidad de paletas de cada línea debe ser mayor que cero.";
            return false;
        }

        var existencias = _materiaPrima.ExistenciasPorTipoBotella();

        // Se agrupa por tipo de botella antes de comparar: dos líneas del mismo tipo que por
        // separado caben, juntas pueden no caber.
        foreach (var grupo in despacho.Lineas.GroupBy(l => l.TipoBotellaId))
        {
            var pedido = grupo.Sum(l => l.CantidadPaletas);
            var disponible = existencias.GetValueOrDefault(grupo.Key);

            if (pedido > disponible)
            {
                var linea = grupo.First();
                error = $"No hay paletas suficientes de {linea.TipoBotellaEtiqueta}: " +
                        $"hay {disponible} y se piden {pedido}.";
                return false;
            }
        }

        error = null;
        return true;
    }

    // --- Transiciones ---

    /// <summary>
    /// Emite el despacho. Quién autoriza no es un campo del formulario: es quien tiene la sesión
    /// abierta, y se estampa aquí para que no se pueda escribir otro nombre.
    /// </summary>
    public DespachoProductoTerminado Registrar(DespachoProductoTerminado despacho, int usuarioId)
    {
        if (despacho.Id != 0)
            throw new InvalidOperationException("Este despacho ya está emitido.");

        if (!_sesion.Puede(Permisos.Despachos.Crear))
            throw new InvalidOperationException("No tienes permiso para registrar despachos de producto terminado.");

        if (!Validar(despacho, out var error))
            throw new InvalidOperationException(error);

        despacho.Numero = SiguienteNumero();
        despacho.Estado = EstadoDespacho.Registrada;
        despacho.AutorizadoPorId = usuarioId;
        despacho.AutorizadoPorNombre = _sesion.UsuarioActual?.NombreCompleto ?? string.Empty;
        despacho.CreadoPorId = usuarioId;
        despacho.FechaCreacion = DateTime.Now;

        return _despachos.Add(despacho);
    }

    /// <summary>
    /// Anula el despacho. La custodia vuelve sola: el kardex no cuenta los documentos anulados,
    /// así que no hay que devolver nada a mano.
    /// </summary>
    public DespachoProductoTerminado Anular(DespachoProductoTerminado despacho, string motivo)
    {
        if (!PuedeAnular(despacho))
            throw new InvalidOperationException("Solo se puede anular un despacho registrado.");

        if (!_sesion.Puede(Permisos.Despachos.Anular))
            throw new InvalidOperationException("No tienes permiso para anular despachos de producto terminado.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Indique el motivo de la anulación.");

        var copia = despacho.Clonar();
        copia.Estado = EstadoDespacho.Anulada;
        copia.MotivoAnulacion = motivo.Trim();
        copia.FechaAnulacion = DateTime.Now;
        _despachos.Update(copia);
        return copia;
    }

    // --- Piezas internas ---

    /// <summary>
    /// Correlativo interno. Mismo criterio que el de las recepciones: se calcula al emitir y el
    /// índice único <c>(OrganizacionId, Numero)</c> es la red por si dos puestos coincidieran.
    /// </summary>
    private string SiguienteNumero()
    {
        var ultimo = _despachos.GetAll()
            .Select(d => d.Numero)
            .Where(n => n.StartsWith(Prefijo, StringComparison.Ordinal))
            .Select(n => int.TryParse(n[Prefijo.Length..], out var valor) ? valor : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{Prefijo}{ultimo + 1:D6}";
    }

    // --- Resúmenes para el panel del módulo ---

    public IReadOnlyList<DespachoProductoTerminado> DelMes()
    {
        var desde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        return [.. _despachos.GetAll()
            .Where(d => d.Estado == EstadoDespacho.Registrada && d.Fecha.Date >= desde)];
    }
}
