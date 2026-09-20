using System.Linq;
using System.Text;

namespace SistemaGestionBar.Models
{
    /// <summary>
    /// El DNI tiene dos caras y conviene no confundirlas: <b>cómo se guarda</b> y
    /// <b>cómo se muestra</b>.
    ///
    /// Se guarda y se teclea con los 8 números pelados —"38987654"—, que es como se carga
    /// un documento en cualquier sistema y lo que hace que dos filas nunca queden con el
    /// mismo DNI escrito de dos formas distintas. Se muestra agrupado —"38.987.654"—,
    /// que es como se lee.
    ///
    /// Esta clase es el único lugar donde se pasa de una cara a la otra.
    /// </summary>
    public static class Documento
    {
        /// <summary>Cantidad de dígitos de un DNI argentino.</summary>
        public const int Digitos = 8;

        /// <summary>
        /// Deja solo los números. Es la forma en que el DNI se guarda y con la que se
        /// comparan dos documentos: "38.987.654" y "38987654" son el mismo.
        /// </summary>
        public static string Normalizar(string? documento) =>
            new((documento ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>
        /// Lo agrupa de a tres con puntos, para mostrar: "38987654" → "38.987.654".
        ///
        /// Si el valor no son exactamente 8 dígitos se devuelve tal cual vino. Es a
        /// propósito: lo que no es un DNI —un CUIT viejo, por ejemplo— no se disfraza de
        /// DNI en el listado, se ve raro y así se nota que hay que corregirlo.
        /// </summary>
        public static string Formatear(string? documento)
        {
            string original = (documento ?? string.Empty).Trim();
            string numeros = Normalizar(original);

            if (numeros.Length != Digitos)
                return original;

            var texto = new StringBuilder(numeros.Length + 2);

            for (int i = 0; i < numeros.Length; i++)
            {
                // Un punto cada vez que quedan grupos de a tres por delante.
                if (i > 0 && (numeros.Length - i) % 3 == 0)
                    texto.Append('.');

                texto.Append(numeros[i]);
            }

            return texto.ToString();
        }
    }
}
