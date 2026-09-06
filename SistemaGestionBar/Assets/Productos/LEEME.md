# Fotos de productos

Los PNG de esta carpeta son **placeholders generados** (degradado por categoría + ícono de
Material Design). Están para que el catálogo se vea completo mientras no haya fotos reales.

## Cómo reemplazarlos por fotos reales

1. Guardá la foto acá con **el mismo nombre de archivo** que el placeholder que reemplaza
   (por ejemplo `mojito.png`). No hace falta tocar código: la ruta ya está en la semilla.
2. Formato: PNG o JPG. Proporción recomendada **400 × 260 px** (relación ~1.54:1), que es la
   que usa la tarjeta del punto de venta. Otras medidas también funcionan porque la imagen se
   recorta con `Stretch="UniformToFill"`, pero una foto muy vertical va a quedar muy recortada.
3. El `.csproj` toma la carpeta entera con un comodín (`Assets\**\*.png`), así que un archivo
   nuevo se incluye solo. Si agregás un producto nuevo, definí su ruta en
   `Data/DatosPrueba.cs` con el helper `RutaFoto("nombre-del-archivo")`.

## Cuando entre la base de datos

La columna `Producto.ruta_imagen` guarda **la ruta**, no el binario de la imagen. Meter fotos
como `VARBINARY` en la tabla infla la base y hace lento cada `SELECT` del catálogo.

Si las fotos las va a cargar el administrador desde el ABM (RF-12), conviene dejar de usar
recursos compilados y pasar a una carpeta en disco (por ejemplo `%ProgramData%\SistemaGestionBar\fotos\`),
porque un recurso embebido no se puede agregar sin recompilar la aplicación.

Un producto sin foto no rompe nada: la tarjeta muestra un degradado con las iniciales.
