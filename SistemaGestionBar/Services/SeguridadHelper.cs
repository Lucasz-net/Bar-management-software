using System;
using System.Security.Cryptography;

namespace SistemaGestionBar.Services
{
    public static class SeguridadHelper
    {
        private const int Iteraciones = 100_000;
        private const int TamanioSalt = 16;
        private const int TamanioHash = 32;

        public static string GenerarHash(string clave)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(TamanioSalt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(clave, salt, Iteraciones, HashAlgorithmName.SHA256, TamanioHash);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool VerificarClave(string claveIngresada, string hashAlmacenado)
        {
            if (string.IsNullOrWhiteSpace(hashAlmacenado))
                return false;

            var partes = hashAlmacenado.Split('.');
            if (partes.Length != 2)
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(partes[0]);
                byte[] esperado = Convert.FromBase64String(partes[1]);
                byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(
                    claveIngresada, salt, Iteraciones, HashAlgorithmName.SHA256, esperado.Length);

                return CryptographicOperations.FixedTimeEquals(calculado, esperado);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
