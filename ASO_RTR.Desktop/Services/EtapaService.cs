using System;
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
    private readonly ISesionActual _sesion;

    public EtapaService(IEtapaDataSource etapas, ISesionActual sesion)
    {
        _etapas = etapas;
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

    public Etapa Completar(Etapa etapa, int cantidadProcesada)
    {
        if (!PuedeCompletar(etapa))
            throw new InvalidOperationException("Solo se puede completar una etapa en proceso.");

        if (!_sesion.Puede(Permisos.Operaciones.Completar))
            throw new InvalidOperationException("No tienes permiso para completar etapas.");

        if (cantidadProcesada < 0)
            throw new InvalidOperationException("La cantidad procesada no puede ser negativa.");

        var copia = etapa.Clonar();
        copia.Estado = EstadoEtapa.Completada;
        copia.FechaFin = DateTime.Now;
        copia.CantidadProcesada = cantidadProcesada;
        _etapas.Update(copia);
        return copia;
    }

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
        copia.FechaInicio = DateTime.Now;
        _etapas.Update(copia);
        return copia;
    }
}
