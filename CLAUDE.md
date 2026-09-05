# ASO RTR — Software ASO RTR (Control de Calidad, Limpieza, Empacado y Etiquetado de Botellas)

Sistema de gestión para una empresa de logística de **control de calidad, limpieza, empacado y
etiquetado de botellas de vidrio** de bebidas alcohólicas. Aplicación de escritorio **WPF · .NET
8** (`net8.0-windows`), instalación local en LAN, una sola organización por instalación.

## Origen de este proyecto (2026-09-05)

Este proyecto es un **scaffold reiniciado a partir de ASO** (`ASO_SOFTWARE_BASE`), un sistema
construido para una empresa de cosecha mecanizada y transporte de caña de azúcar. ASO tenía 17
submódulos de negocio maduros sobre un armazón técnico sólido. Como el negocio de ASO RTR no
tiene nada que ver con el de ASO, se conservó **todo el armazón técnico** (descrito abajo) y se
descartaron los 17 submódulos, salvo **uno que se dejó a propósito como plantilla viva**:
**Finanzas · Cuentas por Pagar y Banco**. Se eligió ese y no otro porque, de los 17, era el que
menos vocabulario de caña de azúcar tenía (prácticamente cero) y el que demuestra en un solo
paquete autocontenido los cuatro patrones que vale la pena tener a la vista al construir el
primer módulo real: CRUD simple (`Proveedor`), contenedor de dos padrones
(`CuentasPorPagarViewModel`, `BancoViewModel`), documento con líneas (`FacturaProveedor`) y
documento con máquina de estados (`CuentasPorPagarService`, `BancoService`). Cuentas por Pagar y
Banco se conservaron **juntos y no por separado** porque están deliberadamente acoplados:
`CuentasPorPagarService.RegistrarPago` exige un `BancoService` por constructor y siempre asienta
el pago en el libro — esa regla ("un movimiento de dinero siempre dispara un asiento, la
dependencia no es opcional") es una de las lecciones más importantes de todo el código, y
partirla para quedarse solo con la mitad habría vaciado el sentido de conservar una plantilla.

Todo lo demás de ASO (Operaciones, Flota, Inventario·Repuestos/Combustible/Compras, Nómina,
Finanzas·CuentasPorCobrar/Tarifas, el concepto de Zafra/temporada de cosecha, el vocabulario de
"núcleo"/C.O.D. del central azucarero) se eliminó. La migración de EF Core también se reinició
desde cero: la historia real de ASO (28 migraciones) entrelaza tablas de armazón con tablas de
negocio en casi todas las fases, así que no había una "migración base" limpia que extraer —
`Migrations/` de este proyecto empieza en una única migración `Baseline` generada contra el
modelo ya recortado.

## Documentación de referencia

`docs/` (vacía por ahora, en `.gitignore`) es donde va la documentación que aporte el equipo
sobre el negocio real: especificaciones de control de calidad, formatos de ticket, tarifas de
proveedores de insumos, etc. Revisarla antes de implementar reglas de negocio nuevas.

## Cómo ejecutar

Es escritorio, no web (no hay dev server / puerto). `dotnet run` dentro de `ASO_RTR.Desktop`, o
F5 en Visual Studio (`ASO_RTR.slnx`).

## Decisiones de arquitectura (heredadas de ASO, sin cambios)

- **WPF con MVVM ligero**: `ViewModels/ViewModelBase.cs` (INotifyPropertyChanged). Lógica fuera
  del code-behind.
- **Datos detrás de interfaces** (`Services/I<X>DataSource.cs`), resueltas en
  `Configuration/DataSourceFactory.cs`. La UI y los ViewModels no conocen EF Core.
- **Sin mocks**: el único camino es SQL Server vía EF Core. Un modo "sin BD" no sobrevive al
  aislamiento por organización, porque los mocks no pasan por el filtro global de EF.
- **Regla de oro**: toda regla de negocio vive en servicios de dominio, nunca en eventos de
  botones.
- **Autorización en capas**: hoy solo la capa 1 (RBAC por permiso de comando) y la 3
  (segregación de funciones: aprobador ≠ solicitante, en `PeticionService`) están completas. La
  autorización se comprueba en el `CanExecute` de los comandos y, para las transiciones propias
  de un documento, dentro del servicio de dominio (`CuentasPorPagarService`, `BancoService`
  exigen `ISesionActual` por constructor). El CRUD genérico (`CrudViewModelBase`) todavía escribe
  directo contra el `IDataSource` sin repetir la comprobación — mismo hueco puntual que tenía ASO.

## Estructura de módulos

Hoy hay **un solo módulo de negocio, Finanzas**, con dos submódulos (Cuentas por Pagar y Banco),
más **cuatro módulos fijados** sin submódulos: **Inicio**, **Peticiones** (bandeja de solicitudes
de cambio), **Administración** (usuarios con sus permisos, y los datos de la propia
organización) y **Configuración** (apariencia, cuenta propia, preferencias de la máquina — anclada
al pie del sidebar, fuera de su `ScrollViewer`, porque no es trabajo del día).

- `Navigation/ModuloCatalogo.cs` — **fuente única** de la estructura (clave, nombre, descripción,
  icono, submódulos). Sidebar, Inicio, dashboard y enrutado leen de aquí.
- `Controls/Sidebar.xaml(.cs)` + `ViewModels/SidebarViewModel.cs` — menú de dos niveles; solo
  emite `NavegacionSolicitada`. `MainWindow.Navegar(modulo, submodulo)` decide la vista.
- `Views/InicioView` — lanzador con una tarjeta por módulo.
- `Views/ModuloDashboardView` — resumen del módulo: indicadores + tarjeta por submódulo. Los
  valores los calcula `ModuloDashboardViewModel.CalcularIndicadores` (un `switch` por clave de
  módulo) — hoy solo tiene el caso `"Finanzas"`.
- `Views/SubmoduloView` — submódulo en construcción, para cuando se agregue uno nuevo al catálogo
  antes de tener su pantalla real.
- Framework CRUD reutilizable (`CrudViewModelBase`, `CrudEditorViewModelBase`, `CrudEditorWindow`,
  `IServicioDialogo`), login/sesión y capa de datos (`Models`, `Services`, `BD`,
  `DataSourceFactory`).

## Cómo se agrega un submódulo (receta, heredada de ASO)

Archivos a **crear**: `Models/<X>.cs` (con `Clonar()`, y **`: IDeOrganizacion` con su
`OrganizacionId`** si la entidad pertenece a una organización; los modelos NO implementan
INotifyPropertyChanged) · `Services/I<X>DataSource.cs` + `BD/Sql<X>DataSource.cs` ·
`Services/<X>Service.cs` si es documento (métodos `PuedeX` puros para el `CanExecute` +
transiciones que revalidan y lanzan `InvalidOperationException` en español) ·
`ViewModels/<Submodulo>ViewModel.cs` · editores · vistas XAML.

La fuente de datos es una línea, no una clase: hereda de `SqlCrudDataSource<T, TId>` y solo
declara las consultas que sean suyas. Si la entidad es una raíz con hijos que se guardan juntos,
hereda de `SqlAgregadoDataSource<T, TId>` en su lugar (ver `FacturaProveedor`/`FacturaProveedorLinea`
como ejemplo de documento con líneas). El ViewModel de la pantalla hereda de
`PantallaViewModelBase`, o de `PantallaCrudViewModel<T, TId>` si además es el listado CRUD de un
maestro; las dos ramas cumplen `IPantalla`, que es lo que el shell enruta.

Archivos a **modificar** siempre: `Configuration/DataSourceFactory.cs` (campo cacheado `??=`),
`BD/DbContext.cs` (`DbSet` + configuración en `OnModelCreating`), la tabla `Pantallas` de
`MainWindow.xaml.cs` (una línea por clave de submódulo), `Styles/PantallaTemplates.xaml` (un
`DataTemplate` por pantalla, o el área de contenido muestra el nombre del tipo del ViewModel),
`Styles/EditorTemplates.xaml` (un `DataTemplate` por editor, o la ventana sale vacía) y
`Styles/Theme.xaml` (un `Chip…Style` por enum de estado nuevo, con
`BasedOn="{StaticResource ChipBaseStyle}"`). Después, `dotnet ef migrations add`.

El permiso de navegación **no se declara**: `Submodulo.Permiso` lo deriva de la clave
(`Ver.<clave>`), así que basta con dar de alta el submódulo en `ModuloCatalogo`. Lo que sí hay
que hacer es sumarlo al rol que corresponda en `Services/MatrizPermisos.cs`, o no lo verá nadie
salvo el Desarrollador.

Arquetipo a copiar (el único que sobrevivió): **Finanzas · Cuentas por Pagar** —
`Models/Proveedor.cs` (CRUD simple), `Models/FacturaProveedor.cs` (documento con líneas),
`Services/CuentasPorPagarService.cs` (documento con máquina de estados),
`ViewModels/CuentasPorPagarViewModel.cs` (contenedor de dos padrones).

## Persistencia

Las entidades de dominio persisten en **SQL Server vía EF Core Migrations**.

- **Migraciones** en `ASO_RTR.Desktop/Migrations/`, empezando en `Baseline` (generada contra el
  modelo recortado: `Organizacion`, `Usuario`, `PermisoUsuario`, `PeticionCambio`, `Proveedor`,
  `FacturaProveedor`+`FacturaProveedorLinea`, `CuentaBancaria`, `MovimientoBanco`).
- **La cadena de conexión vive solo en `appsettings.local.json`** (por máquina, en `.gitignore`);
  la de `appsettings.json` (clave `ConnectionStrings:AsoRtrDb`) apunta a LocalDB con un `.mdf` en
  `App_Data`.
- **No hay claves foráneas reales** en las tablas planas: las relaciones son `int` sueltos y la
  integridad es de la aplicación, con snapshots de texto (`…Nombre`) en cada documento.

## Una sola organización por instalación

**Una instalación atiende a una sola organización.** Ningún formulario pregunta a qué
organización pertenece algo: se estampa la de la organización instalada. A diferencia de ASO
(que tenía núcleo→fincas), **aquí no hay ningún nivel intermedio todavía** — si el negocio real
necesita varias plantas o líneas de producción bajo la misma organización, ese nivel hay que
diseñarlo cuando se conozca la necesidad real (ver PROVISIONAL más abajo).

- **`Models/IDeOrganizacion.cs`** marca las entidades sujetas al ámbito.
- **`BD/DbContext.cs` hace todo el trabajo, en dos sitios:**
  - `AplicarFiltroDeOrganizacion` recorre el modelo y pone un `HasQueryFilter` a cada
    `IDeOrganizacion`. Es **fail-closed**: sin ámbito fijado no se ve nada, en vez de verse todo.
  - `SaveChanges()` estampa `OrganizacionId` en toda fila nueva, y en una fila modificada
    conserva el valor original (no el que traiga el formulario) para no dejar filas huérfanas.
- **`Services/Ambito.cs`** guarda la organización activa. La fija `SesionActual.IniciarSesion` a
  partir del usuario y **no cambia mientras dure la sesión**.
- **`IgnoreQueryFilters()`** se usa solo en el login (todavía no hay ámbito fijado), en
  `BD/SqlUsuarioDataSource.cs`.
- **`Organizaciones` no lleva filtro** — es la tabla que define el ámbito. La fila nace en el
  primer arranque (`Views/PrimerArranqueView`) y se corrige después en Administración ·
  Organización.

## Roles y permisos

Cuatro roles genéricos (`Models/Rol.cs`), cada uno con un conjunto base en
`Services/MatrizPermisos.cs` que el administrador ajusta por usuario con `PermisoUsuario`
(concede o revoca; **revocar gana**).

| Rol | Alcance |
|---|---|
| **Operador** | El día a día en Finanzas: crea/edita proveedores y facturas de proveedor. No mueve dinero ni borra nada |
| **Supervisor** | Todo lo de Operador, más registrar pagos (dispara el asiento en Banco), administrar cuentas bancarias, eliminar, y resolver peticiones de su dominio |
| **AdministradorOrganizacion** | Todo dentro de la organización, salvo crear usuarios Desarrollador |
| **Desarrollador** | Todos los permisos, y es el único que reparte su propio rol |

- **`Services/Permisos.cs`** es el catálogo de cadenas, formato `"Modulo.Accion"`. Los de
  navegación llevan prefijo `Ver.` y se **derivan de la clave del submódulo**.
- **`Rol` se persiste como ORDINAL** (`Usuarios.Rol int`, sin `HasConversion`): los miembros se
  añaden **siempre al final**. Añadir un rol es enum + conjunto en `MatrizPermisos` + el array
  `asignables` de `UsuarioEditorViewModel` (escrito a mano, no `Enum.GetValues`).
- **`SesionActual`** calcula el conjunto efectivo **una vez al entrar** y lo cachea: un cambio de
  rol o de permisos se aplica al volver a iniciar sesión, no en caliente.
- **Contraseñas**: PBKDF2-SHA256 con salt por usuario y 210 000 iteraciones
  (`Services/Passwords.cs`). No hay usuarios sembrados ni contraseñas por defecto: en la primera
  ejecución contra una base sin usuarios, `Views/PrimerArranqueView` pide el nombre de la
  organización, su código, y crea el usuario Desarrollador.

## Peticiones de cambio

Cuando a un rol le falta un permiso **de los sensibles** (`MatrizPermisos.Solicitables`), el botón
puede pedir el motivo con el `MotivoEditorViewModel` de siempre y dejar una `PeticionCambio` en la
bandeja del administrador (módulo fijado **Peticiones**). El mecanismo es genérico y está
completo (`PeticionService`, segregación de funciones, auditoría de la decisión); lo que está
vacío es la lista `Solicitables` — hoy no hay ninguna acción sensible gateada en las pantallas
que ve un Operador. Al agregar un módulo real con acciones sensibles (anular, aprobar, etc.),
sumarlas ahí y cablear el comando con `Services/SolicitudesDeCambio.cs`.

## El sistema visual

Mismo sistema de ASO (tokens en `Styles/Tokens.xaml`, dos paletas en `Colors.xaml` /
`ColorsOscuro.xaml` superpuestas en caliente por `Services/Tema.cs`, controles de WPF
re-estilados en `Styles/Controles.xaml` y `Componentes.xaml` para que el tema oscuro no deje
cajas blancas). Las cuatro reglas de siempre siguen aplicando al escribir XAML nuevo:

1. Color por `DynamicResource` a una clave de `Colors.xaml`, nunca un hex a mano.
2. Una clave nueva de color va en **las dos** paletas (`Colors.xaml` y `ColorsOscuro.xaml`).
3. No se anima un brush: se anima la opacidad de un velo o borde superpuesto.
4. Sombra solo en lo que flota (`Effect="{DynamicResource SombraFlotante}"`), nunca en una
   tarjeta de datos.

## Configuración y preferencias

Tres pestañas en `Views/ConfiguracionView`: **Apariencia** (tema, escala), **Mi cuenta** (ficha +
cambiar contraseña) y **Aplicación** (hoy solo "abrir en la última sección"; no lleva permiso
propio porque no decide nada del negocio — a diferencia de ASO, donde esa pestaña sí tenía un
ajuste con permiso propio, el umbral de alerta de consumo de combustible, que no aplica aquí y se
quitó). Las preferencias NO van a la base de datos: viven en `%AppData%\ASO RTR\ajustes.json`
(`Models/AjustesApp.cs` + `Configuration/AjustesStoreJson.cs`).

## PROVISIONAL — decisiones pendientes, marcadas a propósito

Este scaffold arrancó sin negocio real todavía cargado. Lo siguiente son placeholders
deliberados, fáciles de revisar y cambiar; no son bugs:

1. **Color de marca**: `Colors.xaml`/`ColorsOscuro.xaml` usan un azul-teal neutro de relleno
   (mismo criterio de paleta que ASO — un solo tono base, WCAG cuidado en los pares
   fondo/texto). Cambiar solo requiere editar los valores de esos dos archivos; la estructura de
   claves no cambia.
2. **Roles genéricos**: `Operador` / `Supervisor` / `AdministradorOrganizacion` / `Desarrollador`
   no están atados a los puestos reales de la planta (inspector de calidad, operario de lavado,
   empacador, etiquetador, supervisor de turno…). Ajustar los nombres y los conjuntos base en
   `Models/Rol.cs` / `Services/MatrizPermisos.cs` en cuanto el negocio real esté definido — son
   *el único* rediseño pendiente, porque los cuatro roles conservan intacto el mecanismo
   (ordinal append-only, permisos por deltas, revocar gana).
3. **Jerarquía física**: hoy es "una organización, sin nivel intermedio". Si el negocio real
   tiene varias plantas o líneas de producción bajo la misma organización, diseñar ese nivel
   (equivalente al núcleo→fincas de ASO) cuando se conozca la necesidad real — no está
   prefigurado en el modelo de datos actual.
4. **Nombre de la empresa / razón social**: el `.csproj` usa "ASO RTR" como placeholder en
   `Company`/`Description`.
5. **Módulos de negocio reales**: control de calidad, limpieza, empacado, etiquetado y cualquier
   otro proceso de la planta están sin construir. Usar la receta de "Cómo se agrega un
   submódulo" de más arriba, y el paquete de Cuentas por Pagar como referencia de los cuatro
   patrones (CRUD simple, contenedor de dos padrones, documento con líneas, documento con
   máquina de estados).
6. **`Solicitables` vacío**: no hay ninguna petición de cambio configurada todavía — ver
   "Peticiones de cambio" arriba.
7. **Pantalla de datos de la organización**: `Administración · Organización` (antes "Datos del
   Núcleo" en ASO) solo pide nombre y código; si el negocio real necesita más datos de la
   organización (dirección, RIF, etc.), agregarlos ahí.

## Próximo paso sugerido

1. **Definir con el cliente los módulos reales** (control de calidad, limpieza, empacado,
   etiquetado) y construir el primero copiando el arquetipo de Cuentas por Pagar.
2. **Decidir los roles reales** de la planta y reemplazar los cuatro genéricos.
3. **Elegir el color de marca** y actualizar `Colors.xaml`/`ColorsOscuro.xaml`.
4. **Llevar la comprobación de permisos a los servicios de dominio** de cada módulo nuevo desde
   el principio (no repetir el hueco que tenía ASO en su CRUD genérico).
