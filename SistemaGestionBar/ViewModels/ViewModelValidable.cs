using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>Validación de formularios. Un error solo se muestra una vez que el campo fue tocado o se intentó confirmar, para no marcar en rojo un formulario recién abierto.</summary>
    public abstract class ViewModelValidable : ViewModelConMensaje, INotifyDataErrorInfo
    {
        private static readonly Regex ExpresionCorreo =
            new(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.Compiled);

        private readonly Dictionary<string, List<string>> _errores = new();

        private readonly HashSet<string> _tocados = new();

        private bool _mostrarTodos;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public bool HasErrors => _errores.Count > 0;

        public bool FormularioValido => !HasErrors;

        private IEnumerable<string> ErroresVisibles =>
            _errores.Where(par => EsVisible(par.Key)).SelectMany(par => par.Value);

        public bool HayErroresVisibles => ErroresVisibles.Any();

        public string ResumenDeErrores => string.Join("  ·  ", ErroresVisibles);

        public int CantidadErrores => _errores.Values.Sum(lista => lista.Count);

        private bool EsVisible(string propiedad) => _mostrarTodos || _tocados.Contains(propiedad);

        public IEnumerable GetErrors(string? propertyName) =>
            propertyName is not null && EsVisible(propertyName) &&
            _errores.TryGetValue(propertyName, out var lista)
                ? lista
                : Array.Empty<string>();

        // // Permite apagar la validación mientras el formulario está cerrado.
        protected virtual bool ValidacionActiva => true;

        // // Las subclases declaran acá sus reglas llamando a Requerido, LargoMaximo, etc.
        protected virtual void DeclararReglas() { }

        protected void Revalidar()
        {
            var propiedadesPrevias = _errores.Keys.ToList();
            _errores.Clear();

            if (ValidacionActiva)
                DeclararReglas();

            foreach (var propiedad in propiedadesPrevias.Union(_errores.Keys).Distinct())
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propiedad));

            NotificarEstadoDeValidacion();
        }

        private void NotificarEstadoDeValidacion()
        {
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(FormularioValido));
            OnPropertyChanged(nameof(HayErroresVisibles));
            OnPropertyChanged(nameof(ResumenDeErrores));
            OnPropertyChanged(nameof(CantidadErrores));

            CommandManager.InvalidateRequerySuggested();
        }

        protected void ReiniciarValidacion()
        {
            _tocados.Clear();
            _mostrarTodos = false;
            Revalidar();
        }

        // // Muestra todos los errores pendientes y dice si se puede guardar.
        protected bool IntentarConfirmar()
        {
            _mostrarTodos = true;
            Revalidar();
            return FormularioValido;
        }

        protected string MensajeDeFormularioInvalido =>
            CantidadErrores == 1
                ? "Falta corregir un dato: revisá el campo marcado en rojo."
                : $"Faltan corregir {CantidadErrores} datos: revisá los campos marcados en rojo.";

        protected bool SetCampo<T>(ref T campo, T valor, [CallerMemberName] string? nombrePropiedad = null)
        {
            if (!SetProperty(ref campo, valor, nombrePropiedad))
                return false;

            if (nombrePropiedad is not null)
                _tocados.Add(nombrePropiedad);

            Revalidar();
            return true;
        }

        protected void Agregar(string propiedad, string mensaje)
        {
            if (!_errores.TryGetValue(propiedad, out var lista))
            {
                lista = new List<string>();
                _errores[propiedad] = lista;
            }

            if (!lista.Contains(mensaje))
                lista.Add(mensaje);
        }

        protected void Requerido(string? valor, string propiedad, string etiqueta)
        {
            if (string.IsNullOrWhiteSpace(valor))
                Agregar(propiedad, $"{etiqueta} es obligatorio.");
        }

        protected void RequeridoElegir(object? valor, string propiedad, string etiqueta)
        {
            if (valor is null)
                Agregar(propiedad, $"Seleccione {etiqueta}.");
        }

        protected void LargoMaximo(string? valor, string propiedad, string etiqueta, int maximo)
        {
            if (!string.IsNullOrEmpty(valor) && valor.Trim().Length > maximo)
                Agregar(propiedad, $"{etiqueta} no puede superar los {maximo} caracteres.");
        }

        protected void LargoMinimo(string? valor, string propiedad, string etiqueta, int minimo)
        {
            if (!string.IsNullOrWhiteSpace(valor) && valor.Trim().Length < minimo)
                Agregar(propiedad, $"{etiqueta} debe tener al menos {minimo} caracteres.");
        }

        protected void FormatoCorreo(string? valor, string propiedad)
        {
            if (!string.IsNullOrWhiteSpace(valor) && !ExpresionCorreo.IsMatch(valor.Trim()))
                Agregar(propiedad, "El correo no tiene un formato válido (nombre@dominio.com).");
        }

        public const int DigitosDeDni = Models.Documento.Digitos;

        protected void FormatoDni(string? valor, string propiedad)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            string texto = valor.Trim();

            if (!texto.All(char.IsDigit))
            {
                Agregar(propiedad, "El DNI se carga sin puntos ni guiones: solo los números.");
                return;
            }

            if (texto.Length != DigitosDeDni)
                Agregar(propiedad, $"El DNI tiene que tener {DigitosDeDni} números (van {texto.Length}).");
        }

        protected void FormatoTelefono(string? valor, string propiedad)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            if (!valor.Trim().All(c => char.IsDigit(c) || c is '-' or '+' or '(' or ')' or ' '))
                Agregar(propiedad, "El teléfono solo admite números, espacios y los signos + - ( ).");
        }

        protected void MayorACero(decimal valor, string propiedad, string etiqueta)
        {
            if (valor <= 0)
                Agregar(propiedad, $"{etiqueta} debe ser mayor a cero.");
        }

        protected void NoNegativo(decimal valor, string propiedad, string etiqueta)
        {
            if (valor < 0)
                Agregar(propiedad, $"{etiqueta} no puede ser negativo.");
        }

        protected void NoSupera(decimal valor, string propiedad, string etiqueta, decimal maximo,
                                string formato = "N2")
        {
            if (valor > maximo)
                Agregar(propiedad, $"{etiqueta}: máximo {maximo.ToString(formato)}.");
        }

        protected void NoRepetido(string? valor, string propiedad, IEnumerable<string?> existentes, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            if (existentes.Any(e => string.Equals(e?.Trim(), valor.Trim(), StringComparison.OrdinalIgnoreCase)))
                Agregar(propiedad, mensaje);
        }
    }
}
