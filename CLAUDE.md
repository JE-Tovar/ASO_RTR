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

Hoy hay **cinco módulos de negocio**: **Finanzas** (Cuentas por Pagar y Banco — el ejemplo
conservado del scaffold), **Inventario** (Almacén, Entradas y Salidas), **Nómina** (Empleados),
**Operaciones** (Procesos y sus etapas) y **Catálogo** (Tipos de Botella), más **cuatro módulos
fijados** sin submódulos: **Inicio**, **Peticiones**
(bandeja de solicitudes de cambio), **Administración** (usuarios con sus permisos, y los datos de la propia
organización) y **Configuración** (apariencia, cuenta propia, preferencias de la máquina — anclada
al pie del sidebar, fuera de su `ScrollViewer`, porque no es trabajo del día).

- `Navigation/ModuloCatalogo.cs` — **fuente única** de la estructura (clave, nombre, descripción,
  icono, submódulos). Sidebar, Inicio, dashboard y enrutado leen de aquí.
- `Controls/Sidebar.xaml(.cs)` + `ViewModels/SidebarViewModel.cs` — menú de dos niveles; solo
  emite `NavegacionSolicitada`. `MainWindow.Navegar(modulo, submodulo)` decide la vista.
- `Views/InicioView` — lanzador con una tarjeta por módulo.
- `Views/ModuloDashboardView` — resumen del módulo: indicadores + tarjeta por submódulo. Los
  valores los calcula `ModuloDashboardViewModel.CalcularIndicadores` (un `switch` por clave de
  módulo) — hoy con los casos `"Finanzas"`, `"Inventario"`, `"Nomina"`, `"Operaciones"` y
  `"Catalogo"`.
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

## Inventario (2026-09-06)

Primer módulo construido para esta planta, no heredado del scaffold. Tres submódulos: **Almacén**
(catálogo de artículos con su existencia), **Entradas** (lo que llega) y **Salidas** (boletos de
salida). Las decisiones que no se deducen del código:

- **La existencia NO se guarda: se deriva.** `Articulo` no tiene columna de existencia;
  `InventarioService.ExistenciasPorArticulo()` la calcula como Σ líneas de entradas − Σ líneas de
  salidas, sin contar documentos anulados. `Articulo.Existencia` es una propiedad `Ignore()`-ada
  que rellena el servicio antes de pintar, igual que `CuentaBancaria.SaldoActual`. Se eligió así
  porque un número editable a mano se desincroniza del historial y no deja rastro de por qué
  cambió; el precio a pagar es que **hay que refrescar la vista a mano** tras rellenarla, porque
  los modelos no notifican cambios (ver `AlmacenViewModel.Recargar`). El cálculo es una pasada por
  cada tabla sobre un diccionario, no una consulta por artículo.
- **El stock inicial se carga con una entrada de tipo `Ajuste`**, que es el único tipo que no
  genera cuenta por pagar. Un ajuste negativo es una salida con motivo `Merma` o `Traslado`; no
  hay entradas con cantidad negativa.
- **Registrar una entrada de compra crea su cuenta por pagar en Finanzas**, y la dependencia no es
  opcional: `EntradasInventarioService` exige `CuentasPorPagarService` por constructor, igual que
  éste exige `BancoService`. La factura se escribe **antes** que la entrada, por el mismo motivo
  que el asiento de banco va antes de marcar una factura pagada: es la operación que puede
  rechazar. Si aun así la entrada fallara al guardarse, se retira la factura recién creada — no
  hay transacción que abarque las dos tablas, porque cada fuente de datos abre su propio contexto.
- **El anti-duplicado ya existía**: `CuentasPorPagarService.Validar` rechaza un número de documento
  repetido para el mismo proveedor, que es exactamente el caso "la misma factura se cargó dos
  veces". No hizo falta un `GetByOrigen` como el de Banco.
- **Las líneas de `FacturaProveedor` por fin se usan.** Estaban en el modelo desde el scaffold pero
  ningún editor las llenaba; ahora las llena la entrada de almacén con lo que se compró.
- **El comercio de una compra externa entra al padrón de proveedores** (se busca por nombre, se da
  de alta solo si no estaba), porque la deuda tiene que quedar a nombre de alguien y así se paga
  por el camino de siempre. Riesgo conocido: un nombre mal escrito crea un proveedor duplicado.
- **Anular una entrada** exige dos cosas, comprobadas antes de escribir nada: que el almacén no
  quede en negativo, y que su factura no esté ya pagada (eso se deshace con una nota de crédito).
  Anular una salida no exige nada: la existencia vuelve sola porque el kardex ignora lo anulado.
- **Los correlativos** (`ENT-000123`, `SAL-000123`) los asigna el servicio al registrar, con
  "el último + 1", y el índice único `(OrganizacionId, Numero)` es la red por si dos puestos
  coincidieran. Se persisten, no se derivan del `Id`, porque la descripción de la cuenta por pagar
  cita el número de la entrada y hace falta antes de insertar.
- **Quién autoriza un boleto no es un campo del formulario**: lo estampa el servicio con el usuario
  de la sesión, para que no se pueda escribir otro nombre en el papel.
- **`Agregar()` de `CrudViewModelBase` pasó a `protected virtual`** (`Editar` y `Eliminar` ya lo
  eran, por este mismo motivo): Entradas y Salidas lo redefinen para que el alta pase por el
  servicio de dominio y no escriba directo contra la fuente de datos.
- **La grilla de líneas editable se diseñó aquí**; no había ninguna en el repo. Las líneas del
  editor son ViewModels propios (`LineaEntradaEditorViewModel`, `LineaSalidaEditorViewModel`) y no
  los modelos, porque el subtotal y el total del pie tienen que moverse según se teclea. Van en un
  `DataGrid` con `IsReadOnly="True"` y controles vivos dentro de `DataGridTemplateColumn`, que es
  más manejable que el modo de edición del `DataGrid`.
- **`Controls/PuenteDeDatos.cs`** es nuevo y genérico: las columnas de un `DataGrid` no están en el
  árbol visual y no heredan `DataContext`, así que un `Binding` puesto en una columna falla en
  silencio. Lo usan las dos grillas de líneas para ocultar la columna de precios en un ajuste y
  para llegar al comando de quitar línea.

## Persistencia

Las entidades de dominio persisten en **SQL Server vía EF Core Migrations**.

- **Migraciones** en `ASO_RTR.Desktop/Migrations/`, empezando en `Baseline` (generada contra el
  modelo recortado: `Organizacion`, `Usuario`, `PermisoUsuario`, `PeticionCambio`, `Proveedor`,
  `FacturaProveedor`+`FacturaProveedorLinea`, `CuentaBancaria`, `MovimientoBanco`) y seguida de
  `Inventario` (`Articulo`, `EntradaInventario`+`EntradaInventarioLinea`,
  `SalidaInventario`+`SalidaInventarioLinea`).
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
| **Operador** | El día a día: crea/edita proveedores y facturas; mantiene el catálogo de artículos y registra entradas y salidas de almacén. No mueve dinero, no anula ni borra nada |
| **Supervisor** | Todo lo de Operador, más registrar pagos (dispara el asiento en Banco), administrar cuentas bancarias, anular documentos de almacén, eliminar, y resolver peticiones de su dominio |
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
5. **Todo relleno de acento usa `PrimaryButtonBrush` con `OnAccentBrush` encima; `PrimaryBrush`
   es solo color de texto/icono, nunca fondo.** Regla nueva desde que la marca es ámbar (claro):
   con el teal anterior (oscuro) daba igual cuál de los dos se usara de fondo, porque los dos
   aguantaban texto blanco encima. Con un acento claro ya no da igual — un botón pintado con
   `PrimaryBrush` en la paleta clara saldría en `#7A4E00` con el texto que le pongas encima sin
   ninguna garantía de contraste, en vez del `#FFAD3E` que se espera ver. Ver el comentario de
   cabecera de `Colors.xaml` para el detalle de contraste de cada escalón de la familia.

## Logo (2026-09-07)

`Assets/Logo/` trae el logo real de la empresa, ya rasterizado porque no hay lector de SVG en el
proyecto. El original **no se guarda versionado**: quien lo aportó lo dejó en
`docs/RTR_icon_only.svg` (ignorado por git, ver "Documentación de referencia"), y ese archivo
traía un renglón de texto de la razón social cortado y pegado sin espacio al borde inferior del
ícono — recorte de un export previo, no parte del diseño; hay que volver a recortarlo si se
re-exporta desde el vector original (el arco azul no cierra hasta y≈348 en las coordenadas del
SVG; cualquier recorte tiene que dejarlo completo).

Hay **dos variantes**, para dos contextos distintos:

- **`logo-256.png`** — el logo completo a color (globo con rejilla + flechas + "RTR"), usado
  donde hay espacio de sobra: `Sidebar.xaml` (44px) y `LoginView.xaml` (72px), con
  `RenderOptions.BitmapScalingMode="HighQuality"` en el `Image`. Por debajo de ~64px este logo se
  ve borroso/apelmazado — es el detalle fino de la rejilla del globo, no arreglable por más
  calidad de escalado que se le ponga; de ahí el piso de 44px en el sidebar en vez de calzarlo en
  la fila de 30px que traía el placeholder.
- **`logo.ico`** — variante recortada **solo del globo y las flechas, sin las letras "RTR"**,
  para `ApplicationIcon` en el `.csproj` y el `Icon` de las cuatro ventanas (esto es lo que
  realmente pinta Windows en la barra de tareas, Alt+Tab y el Administrador de tareas — no el
  ícono del `.exe`). Se descartaron dos alternativas antes de llegar a esta: el logo completo
  (con letras) es ilegible a 16–32px por la rejilla fina, igual que `logo-256.png`; y solo las
  letras "RTR" en azul de marca sin fondo se leían mejor pero casi desaparecían en una barra de
  tareas oscura (`#1E427C` sobre transparente tiene contraste pésimo contra un tema oscuro). El
  globo+flechas sin letras funciona en los dos temas porque conserva el naranja (`#EF7E22`) y el
  blanco del hueco central, que dan contraste sin necesitar un fondo sólido propio. El hueco
  donde iban las letras se deja transparente (recorte rectangular sobre el render ya recortado en
  y=370), no se intentó rellenar la rejilla ahí — a los tamaños de ícono no se nota.

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

1. ~~**Color de marca**~~ — resuelto (2026-09-07): ámbar `#FFAD3E` de principal, azul `#3E90FF`
   de complemento, en `Colors.xaml`/`ColorsOscuro.xaml`. A diferencia del teal provisional que
   sustituye, el ámbar es **claro**, así que no basta con cambiar valores hex: la regla de
   acento cambió. Ver "El sistema visual" más abajo.
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
5. **Módulos de producción**: los procesos de planta se modelan hoy de forma genérica en
   **Operaciones** (`Proceso` con sus `Etapa`s). Si control de calidad, limpieza, empacado y
   etiquetado acaban necesitando pantallas propias, usar la receta de "Cómo se agrega un
   submódulo"; como referencia, el paquete de Cuentas por Pagar para los cuatro patrones del
   scaffold, e Inventario para un módulo construido de cero contra este armazón (kardex derivado,
   documento con líneas editables, acoplamiento entre módulos). Pendiente de revisar: que
   `AreaDestino` (las áreas a las que el almacén despacha) siga coincidiendo con los procesos
   reales de Operaciones — hoy son dos listas independientes.
6. **`Solicitables` vacío**: no hay ninguna petición de cambio configurada todavía — ver
   "Peticiones de cambio" arriba. Los dos candidatos naturales ya existen:
   `EntradasInventario.Anular` y `SalidasInventario.Anular`, que hoy un Operador simplemente no
   ve.
7. **Pantalla de datos de la organización**: `Administración · Organización` (antes "Datos del
   Núcleo" en ASO) solo pide nombre y código; si el negocio real necesita más datos de la
   organización (dirección, RIF, etc.), agregarlos ahí.

## Próximo paso sugerido

1. **Definir con el cliente los módulos de producción** (control de calidad, limpieza, empacado,
   etiquetado) y construir el primero; Inventario ya deja el almacén del que tirarán todos.
2. **Decidir los roles reales** de la planta y reemplazar los cuatro genéricos.
3. **Elegir el color de marca** y actualizar `Colors.xaml`/`ColorsOscuro.xaml`.
4. **Llevar la comprobación de permisos a los servicios de dominio** de cada módulo nuevo desde
   el principio (no repetir el hueco que tenía ASO en su CRUD genérico).
