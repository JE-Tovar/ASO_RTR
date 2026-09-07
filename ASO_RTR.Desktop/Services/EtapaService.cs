using System;
using System.Collections.Generic;
using System.Linq;
using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.Services;

/// <summary>
/// Reglas de las etapas de un proceso. A diferencia de una factura, aquí no hay pago ni
/// documento externo que reconciliar — solo la máquina de estados propia de la etapa, con
/// reproceso: una etapa rechazada puede reintentarse en vez de quedar muerta.
/// </summary>
public sealed class EtapaService
{
    private readonly IEtapaDataSource _etapas;
    private readonly SalidasInventarioService _salidasInventario;
    private readonly ISesionActual _sesion;

    public EtapaService(IEtapaDataSource etapas, SalidasInventarioService salidasInventario, ISesionActual sesion)
    {
        _etapas = etapas;
        _salidasInventario = salidasInventario;
        _sesion = sesion;
    }

    // --- Reglas de transición (alimentan el CanExecute) ---

    public bool PuedeEditar(Etapa e) => e.Estado == EstadoEtapa.Pendiente;

    public bool PuedeEliminar(Etapa e) => e.Estado == EstadoEtapa.Pendiente;

    public bool PuedeIniciar(Etapa e) => e.Estado == EstadoEtapa.Pendiente;

    public bool PuedeCompletar(Etapa e) => e.Estado == EstadoEtapa.EnProceso;

    public bool PuedeRechazar(Etapa e) => e.Estado == EstadoEtapa.EnProceso;

    public bool PuedeReintentar(Etapa e) => e.Estado == EstadoEtapa.Rechazada;

    /// <summary>
    /// Valida antes de guardar. Al menos un empleado: la etapa existe para dejar constancia de
    /// quién hizo el trabajo, y una etapa sin nadie asignado no cumple ese propósito.
    /// </summary>
    public bool Validar(Etapa etapa, out string? error)
    {
        if (etapa.ProcesoId == 0)
        {
            error = "Seleccione el proceso al que pertenece esta etapa.";
            return false;
        }

        if (etapa.Empleados.Count == 0)
        {
            error = "Indique al menos un empleado involucrado en esta etapa.";
            return false;
        }

        if (etapa.Consumos.Any(c => c.ArticuloId == 0))
        {
            error = "Hay un material de consumo sin artículo seleccionado.";
            return false;
        }

        if (etapa.Consumos.Any(c => c.UnidadesPorBotella <= 0))
        {
            error = "Las unidades por botella de cada material de consumo deben ser mayores que cero.";
            return false;
        }

        if (etapa.Consumos.Select(c => c.ArticuloId).Distinct().Count() != etapa.Consumos.Count)
        {
            error = "Un mismo artículo no puede aparecer dos veces en el consumo de la etapa.";
            return false;
        }

        error = null;
        return true;
    }

    public Etapa Iniciar(Etapa etapa)
    {
        if (!PuedeIniciar(etapa))
            throw new InvalidOperationException("Solo se puede iniciar una etapa pendiente.");

        if (!_sesion.Puede(Permisos.Operaciones.Iniciar))
            throw new InvalidOperationException("No tienes permiso para iniciar etapas.");

        var copia = etapa.Clonar();
        copia.Estado = EstadoEtapa.EnProceso;
        copia.FechaInicio = DateTime.Now;
        _etapas.Update(copia);
        return copia;
    }

    /// <summary>Lo que procesó bien un empleado.</summary>
    public sealed record DatoProcesadoEmpleado(int EmpleadoId, int Cantidad);

    /// <summary>Una línea de merma reportada al completar: quién, qué artículo y cuánto.</summary>
    public sealed record DatoMermaEmpleado(int EmpleadoId, Articulo Articulo, int Cantidad);

    /// <summary>
    /// Completa la etapa. Genera hasta dos salidas de almacén distintas —no se pueden combinar en
    /// una sola porque <see cref="SalidaInventario.Motivo"/> es de cabecera, no de línea—:
    ///
    /// - <b>Consumo</b>: automático, a partir de <see cref="Etapa.Consumos"/> (configurado al
    ///   crear/editar la etapa) por el total procesado. Etiquetar 200 botellas gasta 200 etiquetas
    ///   de verdad, no solo cuenta botellas.
    /// - <b>Merma</b>: lo que se reporta aquí, líneas libres (empleado + artículo + cantidad), de
    ///   0 a N por empleado.
    ///
    /// Los totales (<see cref="Etapa.CantidadProcesada"/>, <see cref="Etapa.MermaTotal"/>) no se
    /// reciben aparte: se derivan de la suma, igual que
    /// <see cref="Models.TipoBotella.BotellasPorPaleta"/> se deriva de sus partes.
    ///
    /// Ambas salidas —las que pueden rechazar por existencia insuficiente— se registran ANTES de
    /// marcar la etapa como completada, mismo orden que usa <see cref="EntradasInventarioService"/>
    /// con la factura.
    /// </summary>
    public Etapa Completar(Etapa etapa,
                           IReadOnlyList<DatoProcesadoEmpleado> procesados,
                           IReadOnlyList<DatoMermaEmpleado> mermas)
    {
        if (!PuedeCompletar(etapa))
            throw new InvalidOperationException("Solo se puede completar una etapa en proceso.");

        if (!_sesion.Puede(Permisos.Operaciones.Completar))
            throw new InvalidOperationException("No tienes permiso para completar etapas.");

        if (procesados.Any(p => p.Cantidad < 0))
            throw new InvalidOperationException("Las cantidades procesadas no pueden ser negativas.");

        if (mermas.Any(m => m.Cantidad <= 0))
            throw new InvalidOperationException("La cantidad de cada línea de merma debe ser mayor que cero.");

        if (mermas.Any(m => etapa.Empleados.All(e => e.EmpleadoId != m.EmpleadoId)))
            throw new InvalidOperationException("Hay una línea de merma de un empleado que no pertenece a esta etapa.");

        var usuarioId = _sesion.UsuarioActual?.Id ?? 0;
        var destino = MapearAreaDestino(etapa.Tipo);
        var destinoDetalle = destino == AreaDestino.Otro ? etapa.TipoTexto : string.Empty;
        var totalProcesado = procesados.Sum(p => p.Cantidad);

        var consumoNumero = string.Empty;
        if (etapa.Consumos.Count > 0 && totalProcesado > 0)
        {
            var salidaConsumo = new SalidaInventario
            {
                Fecha = DateTime.Today,
                Destino = destino,
                DestinoDetalle = destinoDetalle,
                Motivo = MotivoSalida.Consumo,
                RetiradoPor = string.Join(", ", etapa.Empleados.Select(e => e.EmpleadoNombre)),
                Observaciones = $"Consumo por {totalProcesado} botellas procesadas — etapa {etapa.TipoTexto} — {etapa.ProcesoEtiqueta}.",
                Lineas =
                [
                    .. etapa.Consumos.Select(c => new SalidaInventarioLinea
                    {
                        ArticuloId = c.ArticuloId,
                        ArticuloCodigo = c.ArticuloCodigo,
                        ArticuloNombre = c.ArticuloNombre,
                        UnidadTexto = c.UnidadTexto,
                        Cantidad = c.UnidadesPorBotella * totalProcesado
                    })
                ]
            };

            consumoNumero = _salidasInventario.Registrar(salidaConsumo, usuarioId).Numero;
        }

        var mermaNumero = string.Empty;
        if (mermas.Count > 0)
        {
            var nombres = mermas.Select(m => etapa.Empleados.First(e => e.EmpleadoId == m.EmpleadoId).EmpleadoNombre).Distinct();

            var salidaMerma = new SalidaInventario
            {
                Fecha = DateTime.Today,
                Destino = destino,
                DestinoDetalle = destinoDetalle,
                Motivo = MotivoSalida.Merma,
                RetiradoPor = string.Join(", ", nombres),
                Observaciones = $"Merma registrada al completar la etapa {etapa.TipoTexto} — {etapa.ProcesoEtiqueta}.",
                Lineas =
                [
                    .. mermas.Select(m => new SalidaInventarioLinea
                    {
                        ArticuloId = m.Articulo.Id,
                        ArticuloCodigo = m.Articulo.Codigo,
                        ArticuloNombre = m.Articulo.Nombre,
                        UnidadTexto = m.Articulo.UnidadCorta,
                        Cantidad = m.Cantidad
                    })
                ]
            };

            mermaNumero = _salidasInventario.Registrar(salidaMerma, usuarioId).Numero;
        }

        var copia = etapa.Clonar();
        copia.Estado = EstadoEtapa.Completada;
        copia.FechaFin = DateTime.Now;

        foreach (var empleado in copia.Empleados)
            empleado.Cantidad = procesados.First(p => p.EmpleadoId == empleado.EmpleadoId).Cantidad;

        copia.CantidadProcesada = totalProcesado;
        copia.Mermas =
        [
            .. mermas.Select(m => new MermaEtapa
            {
                EmpleadoId = m.EmpleadoId,
                EmpleadoNombre = etapa.Empleados.First(e => e.EmpleadoId == m.EmpleadoId).EmpleadoNombre,
                ArticuloId = m.Articulo.Id,
                ArticuloNombre = m.Articulo.Nombre,
                Cantidad = m.Cantidad
            })
        ];
        copia.MermaTotal = mermas.Sum(m => m.Cantidad);
        copia.MermaSalidaNumero = mermaNumero;
        copia.ConsumoSalidaNumero = consumoNumero;

        try
        {
            _etapas.Update(copia);
        }
        catch (Exception ex) when (!string.IsNullOrEmpty(consumoNumero) || !string.IsNullOrEmpty(mermaNumero))
        {
            // No se anulan las salidas solas aquí: SalidasInventario.Anular es de Supervisor
            // (Services/MatrizPermisos.cs), y un Operador completando una etapa se quedaría sin
            // ese permiso — cambiar un error claro por uno de permisos confuso sería peor. Los
            // números ya emitidos quedan en el mensaje para que un supervisor los concilie a mano.
            var emitidas = new List<string>();
            if (!string.IsNullOrEmpty(consumoNumero)) emitidas.Add($"consumo {consumoNumero}");
            if (!string.IsNullOrEmpty(mermaNumero)) emitidas.Add($"merma {mermaNumero}");

            throw new InvalidOperationException(
                $"Se registró la salida de {string.Join(" y ", emitidas)}, pero la etapa no pudo completarse. " +
                $"Pide a un supervisor que revise o anule esa(s) salida(s). Detalle: {ex.Message}", ex);
        }

        return copia;
    }

    /// <summary>
    /// AreaDestino y TipoEtapa son dos listas independientes (ver PROVISIONAL en CLAUDE.md); esto
    /// es un mapeo heurístico, no una relación de datos. Recepción/Despacho no tienen equivalente,
    /// así que caen en Otro con el nombre de la etapa como detalle.
    /// </summary>
    private static AreaDestino MapearAreaDestino(TipoEtapa tipo) => tipo switch
    {
        TipoEtapa.ControlCalidad => AreaDestino.ControlDeCalidad,
        TipoEtapa.Limpieza => AreaDestino.Lavado,
        TipoEtapa.Empacado => AreaDestino.Empacado,
        TipoEtapa.Etiquetado => AreaDestino.Etiquetado,
        _ => AreaDestino.Otro
    };

    public Etapa Rechazar(Etapa etapa, string motivo)
    {
        if (!PuedeRechazar(etapa))
            throw new InvalidOperationException("Solo se puede rechazar una etapa en proceso.");

        if (!_sesion.Puede(Permisos.Operaciones.Rechazar))
            throw new InvalidOperationException("No tienes permiso para rechazar etapas.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Indique el motivo del rechazo.");

        var copia = etapa.Clonar();
        copia.Estado = EstadoEtapa.Rechazada;
        copia.MotivoRechazo = motivo.Trim();
        copia.FechaFin = DateTime.Now;
        _etapas.Update(copia);
        return copia;
    }

    /// <summary>Reproceso: la etapa rechazada vuelve a estar en curso, sin arrastrar el motivo anterior.</summary>
    public Etapa Reintentar(Etapa etapa)
    {
        if (!PuedeReintentar(etapa))
            throw new InvalidOperationException("Solo se puede reintentar una etapa rechazada.");

        if (!_sesion.Puede(Permisos.Operaciones.Reintentar))
            throw new InvalidOperationException("No tienes permiso para reintentar etapas.");

        var copia = etapa.Clonar();
        copia.Estado = EstadoEtapa.EnProceso;
        copia.MotivoRechazo = null;
        copia.FechaFin = null;
        copia.CantidadProcesada = null;
        copia.MermaTotal = null;
        copia.MermaSalidaNumero = string.Empty;
        copia.ConsumoSalidaNumero = string.Empty;
        copia.Mermas = [];
        copia.FechaInicio = DateTime.Now;

        // Un reproceso no arrastra lo capturado en el intento anterior. No hay ninguna salida que
        // anular: una etapa Rechazada nunca llegó a tener merma (Rechazar es antes de Completar).
        // Consumos NO se toca: es la configuración de la etapa, no algo capturado al completar.
        foreach (var empleado in copia.Empleados)
            empleado.Cantidad = 0;

        _etapas.Update(copia);
        return copia;
    }
}
