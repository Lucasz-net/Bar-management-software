# Base de datos

Dos pasos, una sola vez por máquina, antes de abrir la aplicación.

---

## 1. Crear la base y el usuario

Conectado como **`root`** (en DBeaver o en el cliente de consola):

```sql
CREATE SCHEMA IF NOT EXISTS sistema_gestion_bar
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

CREATE USER IF NOT EXISTS 'bar_app'@'localhost' IDENTIFIED BY 'TU_CONTRASEÑA';
GRANT ALL PRIVILEGES ON sistema_gestion_bar.* TO 'bar_app'@'localhost';
FLUSH PRIVILEGES;
```

El schema queda **vacío**: las tablas las crea la aplicación al arrancar.

---

## 2. Crear `conexion.local.txt`

La cadena de conexión no está en el código, porque lleva la contraseña de la base y el proyecto se
versiona en GitHub.

En la carpeta `SistemaGestionBar/`, copiar **`conexion.ejemplo.txt`** como
**`conexion.local.txt`** y reemplazar `PONE_TU_CLAVE` por la contraseña que pusiste en el paso 1:

```
server=127.0.0.1;port=3306;database=sistema_gestion_bar;user=bar_app;password=TU_CONTRASEÑA;CharSet=utf8mb4;
```

- `conexion.ejemplo.txt` **está versionado**, con una clave de mentira: es la plantilla que
  encuentra el que clona el repositorio.
- `conexion.local.txt` **está en el `.gitignore`** y no viene en el clon: lo escribe cada
  integrante en su máquina, con la contraseña de su propio MySQL.
