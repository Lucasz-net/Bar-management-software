using System.Linq;
using System.Text;

namespace SistemaGestionBar.Models
{
    /// <summary>El DNI se guarda con 8 números sin separadores y se muestra agrupado ("38.987.654"). Acá se convierte de uno a otro.</summary>
    public static class Documento
    {
        public const int Digitos = 8;

        public static string Normalizar(string? documento) =>
            new((documento ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>Devuelve el valor sin cambios si no tiene 8 dígitos, para que un dato inválido se note en los listados.</summary>
        public static string Formatear(string? documento)
        {
            string original = (documento ?? string.Empty).Trim();
            string numeros = Normalizar(original);

            if (numeros.Length != Digitos)
                return original;

            var texto = new StringBuilder(numeros.Length + 2);

            for (int i = 0; i < numeros.Length; i++)
            {
                if (i > 0 && (numeros.Length - i) % 3 == 0)
                    texto.Append('.');

                texto.Append(numeros[i]);
            }

            return texto.ToString();
        }
    }
}
