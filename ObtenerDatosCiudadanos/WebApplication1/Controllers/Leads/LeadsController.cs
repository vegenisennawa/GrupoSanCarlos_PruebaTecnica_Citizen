using System;                     
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;                
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Esta es la estructura que representará a cada ciudadano
    /// </summary>
    [Table("Leads")]
    public class Lead
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido_Paterno { get; set; }
        public string Apellido_Materno { get; set; }
        public string Sexo { get; set; }
        public string Clave_Edo_Nac { get; set; }
        public string Fecha_Nac { get; set; }
        public string Rfc_Comparacion { get; set; }
        public string Curp_Comparacion { get; set; }
        public string CorreoElectronico { get; set; }
        public string Telefono { get; set; }
        public string EstadoCivil { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public bool Activo { get; set; } = true;

        // Diccionario oficial de palabras inconvenientes
        private static readonly string[] Inconvenientes = { "BUEY", "CACA", "CACO", "CAGA", "CAGO", "CAKA", "CAKO", "COGE", "COJA", "COJE", "COJI", "COJO", "CULO", "FETO", "GUEY", "JOTO", "KACA", "KACO", "KAGA", "KAGO", "KOGE", "KOJO", "KAKA", "KULO", "MAME", "MAMO", "MEAR", "MEAS", "MEON", "MION", "MOCO", "MULA", "PEDA", "PEDO", "PENE", "PUTA", "PUTO", "QULO", "RATA", "RUIN", "TETA", "VACA", "VAGA", "VAGO", "WEY" };
        // Diccionario de preposiciones y artículos que se deben ignorar al calcular el RFC y CURP
        private static readonly string[] Preposiciones = { "DA ", "DAS ", "DE ", "DEL ", "DER ", "DI ", "DIE ", "DD ", "EL ", "LA ", "LOS ", "LAS ", "LE ", "LES ", "MAC ", "MC ", "VAN ", "VON ", "Y " };

        /// <summary>
        /// Diccionario de estados de la República Mexicana para validar la clave de estado en CURP
        /// </summary>
        private static readonly Dictionary<string, string> DiccionarioEstados = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"AS", "Aguascalientes"}, {"BC", "Baja California"}, {"BS", "Baja California Sur"},
            {"CC", "Campeche"}, {"CL", "Coahuila"}, {"CM", "Colima"}, {"CS", "Chiapas"},
            {"CH", "Chihuahua"}, {"DF", "Ciudad de México"}, {"DG", "Durango"},
            {"GT", "Guanajuato"}, {"GR", "Guerrero"}, {"HG", "Hidalgo"}, {"JC", "Jalisco"},
            {"MC", "Estado de México"}, {"MN", "Michoacán"}, {"MS", "Morelos"}, {"NT", "Nayarit"},
            {"NL", "Nuevo León"}, {"OC", "Oaxaca"}, {"PL", "Puebla"}, {"QT", "Querétaro"},
            {"QR", "Quintana Roo"}, {"SP", "San Luis Potosí"}, {"SL", "Sinaloa"}, {"SR", "Sonora"},
            {"TC", "Tabasco"}, {"TS", "Tamaulipas"}, {"TL", "Tlaxcala"}, {"VZ", "Veracruz"},
            {"YN", "Yucatán"}, {"ZS", "Zacatecas"}, {"NE", "Extranjero"}
        };

        // Diccionario oficial de equivalencias del SAT para el cálculo de la homoclave
        private static readonly Dictionary<char, string> EquivalenciasSAT = new Dictionary<char, string>()
        {
            {' ', "00"}, {'0', "00"}, {'1', "01"}, {'2', "02"}, {'3', "03"}, {'4', "04"},
            {'5', "05"}, {'6', "06"}, {'7', "07"}, {'8', "08"}, {'9', "09"}, {'&', "10"},
            {'A', "11"}, {'B', "12"}, {'C', "13"}, {'D', "14"}, {'E', "15"}, {'F', "16"},
            {'G', "17"}, {'H', "18"}, {'I', "19"}, {'J', "21"}, {'K', "22"}, {'L', "23"},
            {'M', "24"}, {'N', "25"}, {'O', "26"}, {'P', "27"}, {'Q', "28"}, {'R', "29"},
            {'S', "32"}, {'T', "33"}, {'U', "34"}, {'V', "35"}, {'W', "36"}, {'X', "37"},
            {'Y', "38"}, {'Z', "39"}, {'Ñ', "40"}
        };

        /// <summary>
        /// Estado formateado para mostrar en la vista, por ejemplo: "Jalisco (JC)"
        /// </summary>
        [NotMapped]  
        public string EstadoFormateado
        {
            get
            {
                if (string.IsNullOrEmpty(Clave_Edo_Nac)) return "N/A";

                string claveLimpia = Clave_Edo_Nac.Trim().ToUpper();
                if (DiccionarioEstados.TryGetValue(claveLimpia, out string nombreEstado))
                {
                    return $"{nombreEstado} ({claveLimpia})";
                }
                return claveLimpia; // Si por algo llega un código raro, muestra el código
            }
        }

        private string _rfcCalculado;

        /// <summary>
        /// 
        /// </summary>
        public string Rfc_Calculado
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(Nombre) || string.IsNullOrEmpty(Apellido_Paterno) || string.IsNullOrEmpty(Fecha_Nac)) return "INCOMPLETO";
                    string baseRfc = ObtenerLetrasBaseRFC();

                    if (DateTime.TryParse(Fecha_Nac, out DateTime fecha))
                    {
                        string f = fecha.ToString("yyMMdd");
                        string homoclave = GenerarHomoclaveRFCExacta($"{Apellido_Paterno} {Apellido_Materno} {Nombre}"); 
                        return $"{baseRfc}{f}{homoclave}";
                    }
                    return "FECHA_ERR";
                }
                catch { return "ERROR"; }
            }

            set { _rfcCalculado = value; }
        }

        private string _curpCalculada;

        /// <summary>
        /// 
        /// </summary>
        public string Curp_Calculada
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(Nombre) || string.IsNullOrEmpty(Apellido_Paterno) || string.IsNullOrEmpty(Sexo) || string.IsNullOrEmpty(Clave_Edo_Nac)) return "INCOMPLETO";

                    string baseRfc = ObtenerLetrasBaseCURP();
                    if (DateTime.TryParse(Fecha_Nac, out DateTime fecha))
                    {
                        string f = fecha.ToString("yyMMdd");
                        string s_limpio = Sexo.ToUpper().Trim();
                        string s = "X";
                        if (s_limpio.StartsWith("F") || s_limpio == "MUJER") s = "M";  
                        else if (s_limpio.StartsWith("M") || s_limpio == "HOMBRE") s = "H";  
                        string edo = Clave_Edo_Nac.ToUpper().Trim();

                        char consP = ObtenerPrimeraConsonanteInterna(RemoverPreposiciones(Apellido_Paterno));
                        char consM = ObtenerPrimeraConsonanteInterna(RemoverPreposiciones(Apellido_Materno));
                        char consN = ObtenerPrimeraConsonanteInterna(ObtenerNombreValido(Nombre));

                        // Armamos los primeros 16 caracteres
                        string baseCurp16 = $"{baseRfc}{f}{s}{edo}{consP}{consM}{consN}";

                        // Generamos los últimos 2 caracteres con la regla oficial
                        string homoclaveCurp = GenerarHomoclaveCurpExacta(baseCurp16, fecha);
                        return $"{baseRfc}{f}{s}{edo}{consP}{consM}{consN}{homoclaveCurp}";
                    }
                    return "FECHA_ERR";
                }
                catch { return "ERROR"; }
            }

            set { _curpCalculada = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="texto"></param>
        /// <returns></returns>
        private string LimpiarCadena(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            texto = texto.ToUpper().Trim();
            texto = texto.Replace("Ñ", "X").Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U");
            texto = texto.Replace(".", "").Replace(",", "").Replace("-", "").Replace("/", "").Trim();
            return texto;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="texto"></param>
        /// <returns></returns>
        private string RemoverPreposiciones(string texto)
        {
            texto = LimpiarCadena(texto) + " ";
            bool cambios;
            do
            {
                cambios = false;
                foreach (var prep in Preposiciones)
                {
                    if (texto.StartsWith(prep))
                    {
                        texto = texto.Substring(prep.Length);
                        cambios = true;
                    }
                }
            } while (cambios);
            return texto.Trim();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nombreStr"></param>
        /// <returns></returns>
        private string ObtenerNombreValido(string nombreStr)
        {
            string limpio = RemoverPreposiciones(nombreStr);
            string[] partes = limpio.Split(' ');
            if (partes.Length > 1 && (partes[0] == "JOSE" || partes[0] == "MARIA" || partes[0] == "MA." || partes[0] == "MA" || partes[0] == "J." || partes[0] == "J"))
                return partes[1];
            return partes[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string ObtenerLetrasBaseRFC()
        {
            string p = RemoverPreposiciones(Apellido_Paterno);
            string m = RemoverPreposiciones(Apellido_Materno);
            string n = ObtenerNombreValido(Nombre);

            string base4 = "";

            if (string.IsNullOrEmpty(p) || string.IsNullOrEmpty(m))
            {
                string unicoApellido = string.IsNullOrEmpty(p) ? m : p;
                char c1 = unicoApellido.Length > 0 ? unicoApellido[0] : 'X';
                char c2 = unicoApellido.Length > 1 ? unicoApellido[1] : 'X';
                char c3 = n.Length > 0 ? n[0] : 'X';
                char c4 = n.Length > 1 ? n[1] : 'X';
                base4 = $"{c1}{c2}{c3}{c4}";
            }
            else
            {
                char c1 = p.Length > 0 ? p[0] : 'X';
                char c2 = p.Length > 1 ? (p.Substring(1).FirstOrDefault(c => "AEIOU".Contains(c))) : 'X';
                if (c2 == '\0') c2 = 'X';
                char c3 = m.Length > 0 ? m[0] : 'X';
                char c4 = n.Length > 0 ? n[0] : 'X';
                base4 = $"{c1}{c2}{c3}{c4}";
            }

            if (Inconvenientes.Contains(base4)) base4 = base4[0] + "X" + base4.Substring(2);
            return base4;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string ObtenerLetrasBaseCURP()
        {
            string p = RemoverPreposiciones(Apellido_Paterno);
            string m = RemoverPreposiciones(Apellido_Materno);
            string n = ObtenerNombreValido(Nombre);

            char c1 = p.Length > 0 ? p[0] : 'X';
            char c2 = p.Length > 1 ? (p.Substring(1).FirstOrDefault(c => "AEIOU".Contains(c))) : 'X';
            if (c2 == '\0') c2 = 'X';
            char c3 = string.IsNullOrEmpty(m) ? 'X' : m[0]; // La famosa 'X' de RENAPO
            char c4 = n.Length > 0 ? n[0] : 'X';

            string base4 = $"{c1}{c2}{c3}{c4}";

            if (Inconvenientes.Contains(base4)) base4 = base4[0] + "X" + base4.Substring(2);
            return base4;
        }

        /// <summary>
        /// Generador automático de homoclave determinista
        /// </summary>
        /// <param name="semilla"></param>
        /// <param name="longitud"></param>
        /// <returns></returns>
        private string GenerarHomoclave(string semilla, int longitud)
        {
            int hash = Math.Abs(semilla.GetHashCode());
            string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string resultado = "";
            for (int i = 0; i < longitud; i++)
            {
                resultado += charset[(hash / (int)Math.Pow(36, i)) % 36];
            }
            return resultado;
        }

        /// <summary>
        /// Algoritmo matemático oficial (Módulo 34) del SAT para la homoclave
        /// </summary>
        private string GenerarHomoclaveRFCExacta(string nombreCompleto)
        {
            if (string.IsNullOrEmpty(nombreCompleto)) return "XXX";

            string limpio = nombreCompleto.ToUpper()
                                          .Replace("Á", "A").Replace("É", "E")
                                          .Replace("Í", "I").Replace("Ó", "O")
                                          .Replace("Ú", "U").Trim();

            // 1. Asignar valores numéricos a cada letra
            string equivalencias = "0"; // Inicia con 0 por regla
            foreach (char c in limpio)
            {
                if (EquivalenciasSAT.TryGetValue(c, out string val))
                {
                    equivalencias += val;
                }
                else
                {
                    equivalencias += "00";
                }
            }

            // 2. Multiplicación cruzada
            int suma = 0;
            for (int i = 0; i < equivalencias.Length - 1; i++)
            {
                int val1 = int.Parse(equivalencias.Substring(i, 2));
                int val2 = int.Parse(equivalencias.Substring(i + 1, 1));
                suma += (val1 * val2);
            }

            // 3. Obtener el Módulo 34
            int digitos = suma % 1000;
            int cociente = digitos / 34;
            int residuo = digitos % 34;

            // Tabla oficial del SAT (34 caracteres)
            string tabla = "123456789ABCDEFGHIJKLMNPQRSTUVWXYZ";

            // Nota: Retornamos los 2 caracteres del cálculo + 'A' simulando el dígito verificador final
            return $"{tabla[cociente]}{tabla[residuo]}A";
        }

        /// <summary>
        /// Algoritmo oficial de RENAPO (Módulo 10) para el dígito verificador y homoclave de siglo
        /// </summary>
        private string GenerarHomoclaveCurpExacta(string curp16, DateTime fechaNacimiento)
        {
            if (curp16.Length != 16) return "00";

            // 1. Carácter 17: Homoclave de Siglo (0 para < 2000, A para >= 2000)
            char siglo = fechaNacimiento.Year < 2000 ? '0' : 'A';
            string curp17 = curp16 + siglo;

            // 2. Carácter 18: Dígito Verificador (Módulo 10)
            string diccionario = "0123456789ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";
            int suma = 0;

            for (int i = 0; i < 17; i++)
            {
                int valor = diccionario.IndexOf(curp17[i]);
                int peso = 18 - i; // Los pesos van del 18 al 2
                suma += (valor * peso);
            }

            int residuo = suma % 10;
            int digitoVerificador = residuo == 0 ? 0 : 10 - residuo;

            return $"{siglo}{digitoVerificador}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cadena"></param>
        /// <returns></returns>
        private char ObtenerPrimeraConsonanteInterna(string cadena)
        {
            if (string.IsNullOrEmpty(cadena)) return 'X';
            char cons = cadena.Substring(1).FirstOrDefault(c => !"AEIOU ".Contains(c));
            return cons != '\0' ? cons : 'X';
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Authorize]
    public class LeadsController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public LeadsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Responde a la URL: /Leads/Index
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            List<Lead> listaCiudadanos = new List<Lead>();

            // Recuperamos el JSON de manera segura desde TempData
            if (TempData.Peek("DatosCiudadanos") is string jsonString)
            {
                try
                {
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var resultadoApi = JsonSerializer.Deserialize<ApiResponseWrapper>(jsonString, opciones);

                    if (resultadoApi?.Respuesta != null)
                    {
                        listaCiudadanos = resultadoApi.Respuesta;
                    }
                }
                catch (JsonException)
                {
                    // Manejo elemental de error de deserialización en caso de datos corruptos
                    ViewBag.Error = "No se pudieron procesar los datos transferidos.";
                }
            }

            // Si no regresa ningún dato entonces me regresa al login.
            if (listaCiudadanos == null || listaCiudadanos.Count == 0)
            {
                return RedirectToAction("Index", "Login");
            }

            //Se ocultan de la vista los "eliminados" de la base de datos.
            var rfcEliminados = _context.Leads.Where(l => l.Activo == false).Select(l => l.Rfc_Comparacion).ToList();
            listaCiudadanos = listaCiudadanos.Where(c => !rfcEliminados.Contains(c.Rfc_Comparacion)).ToList();

            ViewBag.Guardados = _context.Leads.Where(l => l.Activo == true).Select(l => l.Rfc_Comparacion).ToList();

            // Enviamos la lista real extraída de la API a la vista de manera limpia
            return View(listaCiudadanos);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rfc"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Upsert(string rfc)
        {
            Lead ciudadanoActual = null;

            // 1. Buscamos en la API usando el RFC de Comparación
            if (TempData.Peek("DatosCiudadanos") is string jsonString)
            {
                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var resultadoApi = JsonSerializer.Deserialize<ApiResponseWrapper>(jsonString, opciones);
                ciudadanoActual = resultadoApi?.Respuesta?.FirstOrDefault(c => c.Rfc_Comparacion == rfc);
            }

            if (ciudadanoActual == null) return RedirectToAction("Index");

            // Buscamos en BD usando el Rfc_Comparacion (Este SÍ lo traduce EF Core sin problema)
            var leadExistente = _context.Leads.FirstOrDefault(l => l.Rfc_Comparacion == rfc);

            // Si ya existía mandamos ese, si no, el nuevo de la API
            return View(leadExistente ?? ciudadanoActual);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="leadFormulario"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Upsert(Lead leadFormulario)
        {
            // Validación de seguridad en el servidor (por si se saltaron el HTML)
            leadFormulario.CorreoElectronico = leadFormulario.CorreoElectronico?.Trim();
            leadFormulario.Telefono = leadFormulario.Telefono?.Trim();

            if (string.IsNullOrWhiteSpace(leadFormulario.CorreoElectronico) ||
                string.IsNullOrWhiteSpace(leadFormulario.Telefono) ||
                string.IsNullOrWhiteSpace(leadFormulario.EstadoCivil) ||
                !leadFormulario.Telefono.All(char.IsDigit)) // Verifica que TODOS sean números
            {
                // Si la información es basura o trae espacios en blanco, lo regresamos a la vista
                return View(leadFormulario);
            }

            // Volvemos a buscar por el RFC de la API
            var leadBd = _context.Leads.FirstOrDefault(l => l.Rfc_Comparacion == leadFormulario.Rfc_Comparacion);

            if (leadBd == null)
            {
                //// NUEVO: Se inserta tal cual viene de la API + lo que capturaste
                //if (DateTime.TryParse(leadFormulario.Fecha_Nac, out DateTime fechaLimpia))
                //{
                //    leadFormulario.Fecha_Nac = fechaLimpia.ToString("yyyyMMdd");
                //}
                leadFormulario.FechaRegistro = DateTime.Now;
                _context.Leads.Add(leadFormulario);
            }
            else
            {
                // EXISTENTE: Solo actualizamos los datos adicionales solicitados
                leadBd.CorreoElectronico = leadFormulario.CorreoElectronico;
                leadBd.Telefono = leadFormulario.Telefono;
                leadBd.EstadoCivil = leadFormulario.EstadoCivil;
                leadBd.FechaActualizacion = DateTime.Now;

                _context.Leads.Update(leadBd);
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rfc"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Eliminar(string rfc)
        {
            var leadBd = _context.Leads.FirstOrDefault(l => l.Rfc_Comparacion == rfc);

            if (leadBd == null)
            {
                // ¡Aviso! El lead no está en la BD, así que no hay nada que borrar físicamente.
                TempData["MensajeAlerta"] = "El ciudadano aún no ha sido guardado en la base de datos, no se puede eliminar.";
            }
            else
            {
                // Si ya existía en BD, lo apagamos
                leadBd.Activo = false;
                leadBd.FechaActualizacion = DateTime.Now;
                _context.Leads.Update(leadBd);
                _context.SaveChanges();

                TempData["MensajeExito"] = "Registro eliminado correctamente de la base de datos.";
            }

            return RedirectToAction("Index");
        }
    }
}
