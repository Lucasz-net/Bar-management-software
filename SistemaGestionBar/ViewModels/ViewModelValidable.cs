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
    /// <summary>
    /// Base de los ViewModels que tienen formulario. Implementa INotifyDataErrorInfo,
    /// que es la forma en que WPF pregunta "¿este campo está mal?" sin que la vista
    /// necesite una sola línea de code-behind: el binding lee los errores de acá y el
    /// estilo de Material Design pinta el borde rojo y el texto debajo del campo.
    ///
    /// La regla de negocio sigue viviendo en el repositorio, que es quien la hace cumplir
    /// venga de donde venga el dato. Esto es la otra mitad: avisar en pantalla, mientras
    /// se escribe, antes de que el usuario apriete Guardar.
    ///
    /// <b>Errores calculados vs. errores mostrados.</b> Un formulario recién abierto está
    /// vacío y por lo tanto es inválido, pero pintarlo entero de rojo antes de que el
    /// usuario escriba una letra no ayuda: parece que hizo algo mal cuando todavía no
    /// hizo nada. Por eso las reglas se calculan siempre —el botón Guardar necesita saber
    /// si el formulario es válido— pero <see cref="GetErrors"/> solo devuelve el error de
    /// un campo cuando ese campo ya fue tocado, o cuando el usuario apretó Guardar y hay
    /// que mostrarle de una vez todo lo que falta.
    /// </summary>
    public abstract class ViewModelValidable : ViewModelBase, INotifyDataErrorInfo
    {
        private static readonly Regex ExpresionCorreo =
            new(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.Compiled);

        private readonly Dictionary<string, List<string>> _errores = new();

        /// <summary>Campos que el usuario ya editó: los únicos que pueden pintarse de rojo.</summary>
        private readonly HashSet<string> _tocados = new();

        /// <summary>Se enciende al apretar Guardar: de ahí en más se muestran todos los errores.</summary>
        private bool _mostrarTodos;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <summary>
        /// Es true si hay CUALQUIER error, se esté mostrando o no. Lo mira el botón Guardar.
        /// </summary>
        public bool HasErrors => _errores.Count > 0;

        public bool FormularioValido => !HasErrors;

        /// <summary>Errores que en este momento se le están mostrando al usuario.</summary>
        private IEnumerable<string> ErroresVisibles =>
            _errores.Where(par => EsVisible(par.Key)).SelectMany(par => par.Value);

        public bool HayErroresVisibles => ErroresVisibles.Any();

        /// <summary>Los errores a la vista, juntos, para el resumen arriba de los botones.</summary>
        public string ResumenDeErrores => string.Join("  ·  ", ErroresVisibles);

        public int CantidadErrores => _errores.Values.Sum(lista => lista.Count);

        private bool EsVisible(string propiedad) => _mostrarTodos || _tocados.Contains(propiedad);

        public IEnumerable GetErrors(string? propertyName) =>
            propertyName is not null && EsVisible(propertyName) &&
            _errores.TryGetValue(propertyName, out var lista)
                ? lista
                : Array.Empty<string>();

        /// <summary>
        /// Mientras esto sea false no se valida nada: un formulario cerrado no tiene
        /// por qué mostrar los campos vacíos en rojo.
        /// </summary>
        protected virtual bool ValidacionActiva => true;

        /// <summary>
        /// Cada formulario declara acá sus reglas, llamando a los ayudantes de abajo.
        /// Se ejecuta entera cada vez que cambia un campo: son cuatro o cinco
        /// comparaciones, y así nunca queda un error viejo colgado.
        /// </summary>
        protected virtual void DeclararReglas() { }

        /// <summary>Vuelve a correr todas las reglas y avisa a la vista qué cambió.</summary>
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

            // Sin esto el botón Guardar no se entera de que ya puede habilitarse:
            // RelayCommand se apoya en CommandManager para reevaluar CanExecute.
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Deja el formulario "en limpio": las reglas se siguen calculando, pero ningún
        /// campo se pinta de rojo hasta que el usuario lo toque. Se llama al abrir un alta,
        /// al empezar a editar y al cancelar.
        /// </summary>
        protected void ReiniciarValidacion()
        {
            _tocados.Clear();
            _mostrarTodos = false;
            Revalidar();
        }

        /// <summary>
        /// Lo llama Guardar antes de tocar el repositorio: muestra de una vez todo lo que
        /// falta y devuelve si el formulario está en condiciones de confirmarse.
        /// </summary>
        protected bool IntentarConfirmar()
        {
            // Se revalida, no se reusa el último cálculo: hay datos que cambian sin pasar
            // por un SetCampo (las cantidades de la receta, por ejemplo) y el veredicto
            // tiene que ser sobre lo que el formulario dice ahora.
            _mostrarTodos = true;
            Revalidar();
            return FormularioValido;
        }

        /// <summary>Texto para la barra de mensajes cuando Guardar no puede seguir.</summary>
        protected string MensajeDeFormularioInvalido =>
            CantidadErrores == 1
                ? "Falta corregir un dato: revisá el campo marcado en rojo."
                : $"Faltan corregir {CantidadErrores} datos: revisá los campos marcados en rojo.";

        /// <summary>
        /// Setter de los campos del formulario: avisa el cambio, marca el campo como
        /// tocado y revalida. Los campos que no son del formulario siguen usando SetProperty.
        /// </summary>
        protected bool SetCampo<T>(ref T campo, T valor, [CallerMemberName] string? nombrePropiedad = null)
        {
            if (!SetProperty(ref campo, valor, nombrePropiedad))
                return false;

            if (nombrePropiedad is not null)
                _tocados.Add(nombrePropiedad);

            Revalidar();
            return true;
        }

        // ---------------------------------------------------------------
        // Reglas reutilizables. Son las mismas que aplica el repositorio:
        // acá se adelantan para que el usuario las vea mientras escribe.
        // ---------------------------------------------------------------
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

        /// <summary>DNI o CUIT: dígitos, puntos y guiones. Nada de letras.</summary>
        protected void FormatoDocumento(string? valor, string propiedad)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            if (!valor.Trim().All(c => char.IsDigit(c) || c is '.' or '-' or ' '))
                Agregar(propiedad, "El DNI/CUIT solo admite números, puntos y guiones.");
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

        /// <summary>
        /// RF-13 adelantado a la pantalla: el duplicado lo rechaza igual el repositorio,
        /// pero avisarlo mientras se escribe evita completar el formulario para nada.
        /// </summary>
        protected void NoRepetido(string? valor, string propiedad, IEnumerable<string?> existentes, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            if (existentes.Any(e => string.Equals(e?.Trim(), valor.Trim(), StringComparison.OrdinalIgnoreCase)))
                Agregar(propiedad, mensaje);
        }
    }
}
