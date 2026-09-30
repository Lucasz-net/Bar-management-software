# Sistema de Gestión de Bar

Aplicación de escritorio para Windows que resuelve el día a día de un bar: la venta en barra y en
mesa, la carta y el stock, el personal y los reportes.

## Características

- **Un solo inicio de sesión, tres espacios de trabajo.** Según el rol, cada usuario entra al
  tablero de administración, al de gerencia o al punto de venta.
- **Punto de venta** para vender en barra o en mesa, registrando qué mesero atendió.
- **Carta y stock:** productos con foto, categorías, precios y reposición de stock.
- **Padrón de personas y usuarios**, separados: una persona puede existir sin tener cuenta.
- **Parámetros del negocio:** mesas y medios de pago.
- **Reportes en PDF** de ventas y del rendimiento del equipo.

### Roles

| Rol | Entra a |
|---|---|
| Administrador | Tablero de administración: quién entra al sistema y con qué rol |
| Gerente | Tablero de gerencia: la operación del bar (carta, stock, ventas, equipo, reportes) |
| Vendedor / Mesero | Punto de venta |

## Tecnologías

- **.NET 10** y **WPF**, con el patrón **MVVM**
- **Material Design in XAML** (`MaterialDesignThemes` 5.3) para la interfaz
- **Entity Framework Core 9** con enfoque Code-First y migraciones
- **MySQL** a través de **Pomelo.EntityFrameworkCore.MySql** 9.0
- **QuestPDF** para generar los reportes

> EF Core queda en 9.x a propósito: Pomelo todavía no tiene versión para EF Core 10. No actualizar
> esos paquetes a la última sin revisar la compatibilidad.

## Requisitos

- Windows
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MySQL Server corriendo en `127.0.0.1:3306`
- Un cliente para ejecutar SQL (DBeaver, MySQL Workbench o la consola de `mysql`)

## Configurar la base de datos

Dos pasos, una sola vez por máquina, antes de abrir la aplicación. El detalle está también en
[BASE-DE-DATOS.md](BASE-DE-DATOS.md).

### 1. Crear la base y el usuario

Conectado como **`root`**:

```sql
CREATE SCHEMA IF NOT EXISTS sistema_gestion_bar
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

CREATE USER IF NOT EXISTS 'bar_app'@'localhost' IDENTIFIED BY 'TU_CONTRASEÑA';
GRANT ALL PRIVILEGES ON sistema_gestion_bar.* TO 'bar_app'@'localhost';
FLUSH PRIVILEGES;
```

El schema queda **vacío**: las tablas las crea la aplicación al arrancar, aplicando las migraciones
y cargando los datos de prueba.

Respetar la collation `utf8mb4_0900_ai_ci`: las tablas la heredan del schema, y es la que hace que
"no repetir nombre" no distinga mayúsculas ni acentos.

### 2. Crear `conexion.local.txt`

La cadena de conexión no está en el código porque lleva la contraseña de la base.

En la carpeta `SistemaGestionBar/`, copiar **`conexion.ejemplo.txt`** como **`conexion.local.txt`**
y reemplazar `PONE_TU_CLAVE` por la contraseña del paso 1:

```
server=127.0.0.1;port=3306;database=sistema_gestion_bar;user=bar_app;password=TU_CONTRASEÑA;CharSet=utf8mb4;
```

- `conexion.ejemplo.txt` **está versionado**, con una clave de mentira: es la plantilla.
- `conexion.local.txt` **está en el `.gitignore`**: cada integrante lo escribe en su máquina, con la
  contraseña de su propio MySQL.

## Ejecutar

```bash
dotnet run --project SistemaGestionBar
```

O abrir `SistemaGestionBar.slnx` en Visual Studio y ejecutar.

### Usuarios de prueba

Todos entran con la clave `12345678`:

| Correo | Rol |
|---|---|
| admin@bar.com | Administrador |
| gerente@bar.com | Gerente |
| vendedor@bar.com | Vendedor |
| mesero@bar.com | Mesero |

## Colaboradores

- **Lucas Escobar** ([@Lucasz-net](https://github.com/Lucasz-net))
- **Nicolás Kern**  ([@Nicolasbitmil](https://github.com/Nicolasbitmil))
