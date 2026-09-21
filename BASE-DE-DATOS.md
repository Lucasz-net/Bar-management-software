# Base de datos

El sistema usa **MySQL local** con **EF Core Code-First** (provider Pomelo). Cada integrante tiene
su propio MySQL en su máquina: la base **no se comparte y no viaja en el repositorio**.

**Las tablas no se crean a mano y no hay ningún script de tablas.** Las crea la migración
`SistemaGestionBar/Data/Migraciones/20260914180334_Inicial.cs`, que la aplicación aplica sola al
arrancar (`Data/SembradorDeDatos.cs` llama a `Migrate()` y después siembra los datos de prueba).
El único SQL que se ejecuta a mano es el del paso 2: crear el schema vacío y el usuario.

---

## 1. Requisitos

| Qué | Versión | Por qué esa |
|---|---|---|
| .NET SDK | **10.x** | El proyecto es `net10.0-windows` |
| MySQL Community Server | 8.0 o superior | Probado en 26.7.0 |
| DBeaver (u otro cliente) | cualquiera | Solo para mirar y administrar la base |
| Internet | en el primer build | NuGet baja MaterialDesignThemes, Pomelo y QuestPDF |

**No actualizar EF Core a 10.x.** `Pomelo.EntityFrameworkCore.MySql` va por 9.0.0 y no tiene build
para EF Core 10 ni en preview. Un proyecto `net10.0-windows` consume paquetes de .NET 9 sin
problema.

`dotnet-ef` **no hace falta** para correr el proyecto, solo para generar migraciones nuevas
(ver paso 9).

---

## 2. Crear la base y el usuario

Una sola vez por máquina, **conectado como `root`** (en DBeaver o con el cliente de consola):

```sql
CREATE SCHEMA IF NOT EXISTS sistema_gestion_bar
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

CREATE USER IF NOT EXISTS 'bar_app'@'localhost' IDENTIFIED BY 'BarAdmin';
GRANT ALL PRIVILEGES ON sistema_gestion_bar.* TO 'bar_app'@'localhost';
FLUSH PRIVILEGES;
```

Dejar el schema **vacío**. No crear ninguna tabla.

Dos detalles que no son opcionales:

- **La collation `utf8mb4_0900_ai_ci` la tiene que poner el schema.** La migración fija
  `CharSet=utf8mb4` tabla por tabla pero no la collation, así que las tablas heredan la del schema.
  Esa collation compara ignorando mayúsculas **y acentos**, y las reglas de "no repetir nombre" se
  apoyan en eso del lado del servidor (por eso no hay `OrdinalIgnoreCase` en las consultas).
- **El schema hay que crearlo como `root`.** `bar_app` tiene permisos solo dentro de
  `sistema_gestion_bar`, no puede crear bases.

No se usa `root` para la aplicación: la contraseña de `root` abre todo el servidor.

---

## 3. Crear `conexion.local.txt`

La cadena de conexión **no está en el código**: lleva la contraseña de la base y este proyecto se
versiona en GitHub. En la carpeta `SistemaGestionBar/`, copiar `conexion.ejemplo.txt` como
`conexion.local.txt` y poner la contraseña real, todo en una sola línea:

```
server=127.0.0.1;port=3306;database=sistema_gestion_bar;user=bar_app;password=TU_CLAVE;CharSet=utf8mb4;
```

`conexion.local.txt` está en el `.gitignore` y **no viene en el clon**: cada uno escribe el suyo.
`conexion.ejemplo.txt` sí está versionado, con una clave de mentira, como plantilla.

Lo lee `Data/CadenaDeConexion.cs`, que busca el archivo junto al `.exe` y, si no está, sube hasta la
carpeta del `.csproj`. Así sirve igual para la aplicación y para los comandos de migración.

---

## 4. Primer arranque

```bash
cd SistemaGestionBar
dotnet run
```

Al arrancar, la aplicación crea las **13 tablas** y carga los datos de prueba: catálogo, padrón de
personas y una semana de ventas.

**La semilla no está dentro de una migración a propósito.** Dos valores no son fijos: las
contraseñas se hashean con un salt aleatorio y las ventas de ejemplo son "de los últimos siete
días" contados desde hoy. Metidas en una migración quedarían congeladas el día que se generó, y el
resumen del bar abriría siempre sin ventas del día.

> **La semilla corre UNA sola vez.** El guardia es `if (db.Roles.Any()) return;`
> (`Data/SembradorDeDatos.cs:35`). Si la base ya tiene datos, no toca nada — así los cambios que
> cada uno hace desde la aplicación no se pisan al reabrir. La contracara: **si alguien agrega
> datos nuevos a `DatosPrueba.cs`, las bases ya sembradas no los van a ver nunca**, sin ningún
> error, solo faltando cosas. Para incorporarlos hay que reiniciar la base (paso 7).

---

## 5. Verificar que quedó bien

```sql
USE sistema_gestion_bar;

SHOW TABLES;                          -- 13 tablas + __efmigrationshistory
SELECT * FROM __EFMigrationsHistory;  -- 20260914180334_Inicial

SELECT COUNT(*) AS productos FROM producto;   -- 13
SELECT COUNT(*) AS personas  FROM persona;    -- 9
SELECT COUNT(*) AS ventas    FROM venta;      -- 13

SELECT u.IdUsuario, u.Email, r.NombreRol
FROM usuario u JOIN rol r ON r.IdRol = u.IdRol
ORDER BY u.IdUsuario;                 -- 5 usuarios, 4 roles
```

Si falta algo de eso, la base no se sembró con el código actual: ver paso 7.

**Usuarios de prueba** — los cinco entran con la clave `12345678`:

| Correo | Rol |
|---|---|
| `admin@bar.com` | Administrador |
| `gerente@bar.com` | Gerente |
| `vendedor@bar.com` | Vendedor |
| `mesero@bar.com` | Mesero |
| `valentina@bar.com` | Mesero |

---

## 6. Exportar e importar los datos

El repositorio lleva **código, no datos**. Todo lo que se carga o edita desde la aplicación vive
solo en el MySQL de esa máquina. Para llevar los datos a otra PC hay que copiar la base.

Los binarios de MySQL no están en el PATH; la ruta en una instalación por defecto es
`C:\Program Files\MySQL\MySQL Server 26.7\bin\`.

**Exportar** (estructura + datos, en la máquina de origen):

```powershell
cd "C:\Program Files\MySQL\MySQL Server 26.7\bin"
.\mysqldump.exe -u bar_app -p --single-transaction --no-tablespaces --set-gtid-purged=OFF `
  sistema_gestion_bar --result-file=C:\temp\bar.sql
```

`mysqldump` va a imprimir un `Error ... for table 'column_masking_policy'`: es ruido de MySQL 26,
porque `bar_app` no tiene permiso de lectura sobre tablas internas del servidor que no nos
interesan. **El volcado sale completo igual**; para confirmarlo, la última línea del archivo tiene
que decir `-- Dump completed on ...`.

**Importar** (en la máquina de destino, con el schema y el usuario ya creados del paso 2, y
**antes** de abrir la aplicación por primera vez):

```powershell
Get-Content C:\temp\bar.sql | & "C:\Program Files\MySQL\MySQL Server 26.7\bin\mysql.exe" `
  -u bar_app -p sistema_gestion_bar
```

Se importa con una tubería y no con `mysql ... < bar.sql` porque **PowerShell no soporta la
redirección de entrada `<`**. Tampoco sirve `mysql -e "source archivo.sql"`: `source` es un comando
del cliente interactivo, y con `-e` se manda al servidor, que responde error de sintaxis. La otra
opción cómoda es abrir el `.sql` en DBeaver y usar *Execute script* (Alt+X).

Después de importar, la aplicación ve la base con datos y **no siembra**: respeta lo importado.

Solo la estructura, sin filas (útil para armar el DER en DBeaver):

```powershell
.\mysqldump.exe -u bar_app -p --no-data --no-tablespaces sistema_gestion_bar --result-file=C:\temp\estructura.sql
```

El volcado **no se versiona**: son datos, y además cada uno tiene los suyos. Se pasa por fuera del
repositorio.

---

## 7. Reiniciar la base desde cero

Cuando la base quedó vieja o inconsistente (por ejemplo, faltan datos que sí están en
`DatosPrueba.cs`). **Borra todo lo cargado desde la aplicación**, así que conviene exportar antes si
hay algo que valga la pena.

Como `root`:

```sql
DROP DATABASE sistema_gestion_bar;
CREATE SCHEMA sistema_gestion_bar
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;
```

No hace falta recrear el usuario: MySQL **no borra los privilegios** de una base eliminada, el
`GRANT` de `bar_app` sigue en pie. Se abre la aplicación y queda todo rearmado.

---

## 8. Problemas frecuentes

| Síntoma | Causa | Solución |
|---|---|---|
| `No encontré «conexion.local.txt»` | Falta el archivo del paso 3 | Crearlo en `SistemaGestionBar/` |
| `Access denied for user 'bar_app'` | La contraseña no es la del `CREATE USER` | Corregir `conexion.local.txt`, o como root: `ALTER USER 'bar_app'@'localhost' IDENTIFIED BY '...'` |
| `Unknown database 'sistema_gestion_bar'` | No se creó el schema | Paso 2 |
| `Unable to connect to any of the specified MySQL hosts` | El servicio de MySQL está parado | Arrancarlo en `services.msc` — los datos no se pierden |
| Faltan datos (un usuario, un producto) | La base se sembró con una versión más vieja del código, y la semilla ya no vuelve a correr | Paso 7, o cargarlo a mano desde el panel de administración |
| Al validar nombres repetidos no se ignoran acentos o mayúsculas | El schema se creó con otra collation | Paso 7, con el `COLLATE` correcto |

Borrar una **conexión** en DBeaver no borra la base: es una ficha guardada del cliente, con host,
usuario y contraseña. Los datos viven en el servicio de MySQL, y la aplicación se conecta directo al
servidor sin pasar por DBeaver. Lo que sí borra la base es un `DROP DATABASE`, o hacer *Delete*
sobre el **schema** en el árbol de DBeaver.

---

## 9. Cuando cambia el modelo (migraciones)

Al agregar o modificar una entidad en `Models/` y su mapeo en `Data/BarDbContext.cs` hay que
generar una migración. Requiere la herramienta global, fijada en la misma línea que EF Core:

```bash
dotnet tool install --global dotnet-ef --version 9.0.20
```

```bash
cd SistemaGestionBar

dotnet ef migrations has-pending-model-changes   # ¿el modelo se fue de la migración?
dotnet ef migrations add NombreDelCambio -o Data/Migraciones
dotnet ef database update                        # opcional: la app también la aplica al arrancar
```

**Una migración nueva, no editar una ya aplicada.** Editar una migración en el lugar deja las bases
desincronizadas: la que ya la aplicó no la vuelve a correr —EF la ve como aplicada— y queda distinta
de la de quien clona de cero, sin ningún aviso. Ya pasó una vez con las tablas de factura, y hubo
que borrarlas a mano en cada máquina.

Después de generar la migración hay que **commitear los tres archivos** que aparecen en
`Data/Migraciones/`: la migración, su `.Designer.cs` y `BarDbContextModelSnapshot.cs`. Sin el
snapshot, la próxima migración que genere otro integrante sale mal.
