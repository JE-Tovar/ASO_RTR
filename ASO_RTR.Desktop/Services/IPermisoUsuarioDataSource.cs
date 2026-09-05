using ASO_RTR.Desktop.Models;

namespace ASO_RTR.Desktop.Services;

/// <summary>Ajustes de permisos por usuario, administrados dentro de la organizacion activa.</summary>
public interface IPermisoUsuarioDataSource : ICrudDataSource<PermisoUsuario, int>
{
}
