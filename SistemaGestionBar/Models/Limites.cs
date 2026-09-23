namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Cuánto entra en cada columna de la base.
    ///
    /// <b>Por qué esto existe.</b> Cada columna de texto tiene un largo y cada columna de
    /// número un techo. Si un formulario deja escribir más de eso, el dato llega a MySQL,
    /// MySQL lo rechaza con un <c>Data too long for column 'telefono'</c> y —como nadie
    /// atrapa esa excepción— la aplicación se cierra en la cara del usuario. La validación
    /// de largo no es una formalidad: es lo único que separa un cartel rojo de un cierre.
    ///
    /// <b>Por qué en un solo lugar.</b> El mismo número lo necesitan tres capas: el mapeo
    /// de EF Core (que es el que crea la columna), la regla del formulario (que avisa
    /// mientras se escribe) y el <c>MaxLength</c> del campo (que directamente no deja
    /// pasarse). Escrito tres veces, tarde o temprano uno de los tres queda viejo y el
    /// agujero vuelve. Escrito acá, cambiar la columna es cambiar una línea —eso sí:
    /// agrandar un largo es una migración, porque cambia el modelo—.
    ///
    /// Es el mismo criterio de <see cref="Documento"/>, que es el único lugar donde el DNI
    /// pasa de cómo se guarda a cómo se muestra.
    /// </summary>
    public static class Limites
    {
        // ---------------------------------------------------------------
        // Texto
        // ---------------------------------------------------------------
        public const int NombrePersona = 60;
        public const int ApellidoPersona = 60;
        public const int Telefono = 30;

        /// <summary>
        /// Sirve para las dos: el correo personal de <see cref="Persona"/> y el correo de
        /// trabajo de <see cref="Usuario"/>. Son columnas distintas con el mismo largo, y
        /// un correo es un correo en las dos.
        /// </summary>
        public const int Email = 120;

        public const int NombreProducto = 80;
        public const int DescripcionProducto = 200;

        /// <summary>
        /// 260 es el largo máximo histórico de una ruta en Windows (MAX_PATH), que es de
        /// donde salió el número de la columna.
        /// </summary>
        public const int RutaImagen = 260;

        public const int NombreInsumo = 60;
        public const int UnidadMedida = 15;

        /// <summary>
        /// Las tres tablas paramétricas —categoría, método de pago y ubicación— comparten
        /// el largo de su nombre: son listas cortas de etiquetas, no de descripciones.
        /// </summary>
        public const int NombreParametrico = 40;

        public const int NombreRol = 40;

        /// <summary>El hash PBKDF2 mide siempre lo mismo (unos 66 caracteres); la columna sobra.</summary>
        public const int ClaveHasheada = 200;

        // ---------------------------------------------------------------
        // Números
        // ---------------------------------------------------------------
        /// <summary>
        /// Techo de las columnas de dinero, que son <c>decimal(12,2)</c>: doce dígitos en
        /// total, dos de ellos después de la coma. O sea diez enteros.
        /// </summary>
        public const decimal Dinero = 9_999_999_999.99m;

        /// <summary>
        /// Techo de las columnas de cantidad, que son <c>decimal(12,3)</c>: nueve enteros
        /// y tres decimales. Es el stock de los insumos y las cantidades de una receta.
        /// </summary>
        public const decimal Cantidad = 999_999_999.999m;

        /// <summary>
        /// Techo del stock de un producto, que es una columna <c>int</c> porque son unidades
        /// enteras —no hay media botella—.
        ///
        /// El número es absurdo para un bar, y ése no es el punto: el punto es que sumarle a
        /// un <c>int</c> más de lo que entra <b>da la vuelta y lo deja negativo</b>, sin
        /// excepción y sin aviso. Comprobar contra este techo antes de sumar es lo que
        /// impide que un ajuste de stock desmedido termine guardando un stock negativo.
        /// </summary>
        public const int StockDeProducto = int.MaxValue;
    }
}
