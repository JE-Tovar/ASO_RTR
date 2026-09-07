# ASO RTR

Sistema de gestión para una empresa de logística de **control de calidad, limpieza, empacado y
etiquetado de botellas de vidrio** para bebidas alcohólicas.

Este proyecto es un **scaffold reiniciado a partir de ASO** (un sistema para una empresa de
cosecha mecanizada de caña de azúcar): conserva su armazón técnico completo — autenticación,
roles y permisos, aislamiento multi-organización, framework CRUD/MVVM, tema claro/oscuro,
peticiones de cambio — y trae un único módulo de negocio de ejemplo (Finanzas · Cuentas por
Pagar y Banco) como plantilla viva de los patrones a seguir. Ver `CLAUDE.md` para el detalle
completo de qué se conservó, qué se descartó y cómo se agrega un módulo nuevo.

## Requisitos

- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11

## Ejecutar

```bash
cd ASO_RTR.Desktop
dotnet run
```

O abrir `ASO_RTR.slnx` en Visual Studio 2022.

La base de datos es un archivo **SQL Server LocalDB** (`ASO_RTR.Desktop/App_Data/AsoRtr.mdf`), no
un servidor aparte: no hace falta configurar nada para arrancar. Hace falta tener **LocalDB**
instalado (viene con Visual Studio; si no, se instala aparte con el instalador liviano
"SqlLocalDB.msi" de SQL Server Express). La primera vez, aplica el esquema:

```bash
cd ASO_RTR.Desktop
dotnet ef database update
```

Esto crea `App_Data/AsoRtr.mdf` (no se sube al repo — cada máquina tiene el suyo, ver
`.gitignore`). En el primer arranque contra una base sin usuarios, la aplicación pide el nombre
de la organización y crea el usuario desarrollador. No hay usuarios ni contraseñas por defecto.

Si en cambio quieres apuntar a un SQL Server real (compartido o remoto) en vez de tu LocalDB
local, copia `ASO_RTR.Desktop/appsettings.local.example.json` como `appsettings.local.json` y pon
ahí tu cadena de conexión — no se sube al repo, así que cada quien puede tener la suya.

## Estructura

```
ASO_RTR/
├── ASO_RTR.slnx
└── ASO_RTR.Desktop/      # Aplicación WPF (MVVM ligero)
    ├── Models/           # Entidades del dominio
    ├── Services/         # Servicios de dominio, sesión/permisos y contratos de datos
    ├── ViewModels/       # Lógica de presentación
    ├── Views/            # Pantallas y editores
    ├── Navigation/       # Catálogo de módulos y submódulos (fuente única)
    ├── Configuration/     # Configuración y composición de fuentes de datos
    ├── BD/               # EF Core / SQL Server (DbContext + fuentes Sql…)
    ├── Migrations/       # Migraciones EF Core
    ├── Controls/         # Sidebar y componentes reutilizables
    └── Styles/           # Paleta y estilos
```

## Módulos

| Módulo | Submódulos | Estado |
|---|---|---|
| Finanzas | Cuentas por Pagar · Banco | módulo de ejemplo, funcional |
| Inventario | Almacén · Entradas · Salidas | funcional |
| Nómina | Empleados | funcional |
| Operaciones | Procesos | funcional |
| Catálogo | Tipos de Botella | funcional |

Además hay **cuatro secciones fijas** según el rol: **Inicio**, **Peticiones** (bandeja de
solicitudes de cambio), **Administración** (usuarios y permisos, y los datos de la organización)
y **Configuración**, esta última anclada al pie del menú lateral: tema claro/oscuro, escala de la
interfaz, cambio de la propia contraseña y las preferencias de la máquina. Se guardan en
`%AppData%\ASO RTR\ajustes.json`, no en la base de datos.

**Inventario** lleva el almacén de insumos: un catálogo de artículos cuya existencia **no se
teclea**, se calcula de lo que entró menos lo que salió. Registrar una entrada por compra deja
sola su cuenta por pagar en Finanzas, y las salidas se emiten como boleto con destino, quién
retira y quién autoriza.

Los módulos reales de producción (control de calidad, lavado, empacado, etiquetado…) se agregan
siguiendo la receta de "Cómo se agrega un submódulo" en `CLAUDE.md`.

## Roles

Cuatro roles genéricos, sin atar todavía a los puestos reales de la planta — ver "PROVISIONAL"
en `CLAUDE.md`.

| Rol | Qué puede |
|---|---|
| **Operador** | El día a día: registra facturas de proveedor y da de alta proveedores; mantiene el catálogo del almacén y registra entradas y salidas. No mueve dinero, no anula ni borra nada: para eso levanta una petición |
| **Supervisor** | Todo lo de Operador, más registrar pagos (que asientan el movimiento en el libro de banco), administrar cuentas bancarias, y anular entradas y salidas de almacén. Resuelve peticiones de su dominio |
| **Administrador de organización** | Todo dentro de la organización. Lo único que no puede es crear otros usuarios Desarrollador |
| **Desarrollador** | Todo, y es el único que puede crear otros usuarios Desarrollador |

**Una sola organización trabaja por instalación.** Ningún formulario pregunta a qué organización
pertenece algo: se estampa la de la instalación.

## Estado del proyecto

El armazón técnico está completo y probado (compila, migra y arranca). Sobre él ya hay cinco
módulos de negocio: **Finanzas** (el ejemplo conservado del scaffold), **Inventario**, **Nómina**,
**Operaciones** y **Catálogo**. Lo que sigue pendiente de definición —los roles reales de la
planta, el color de marca— está en la sección "PROVISIONAL" de `CLAUDE.md`.
