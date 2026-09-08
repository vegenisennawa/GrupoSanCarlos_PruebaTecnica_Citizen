using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;                     
using System.Collections.Generic;
using System.Linq;                
using System.Text.Json;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Esta es la estructura que representará a cada ciudadano
    /// </summary>
    public class CitizenViewModel
    {
        public string Nombre { get; set; }
        public string Apellido_Paterno { get; set; }
        public string Apellido_Materno { get; set; }
        public string Sexo { get; set; }
        public string Clave_Edo_Nac { get; set; }
        public string Fecha_Nac { get; set; }
        public string Rfc_Comparacion { get; set; }
        public string Curp_Comparacion { get; set; }

        // Diccionario oficial de palabras inconvenientes
        private static readonly string[] Inconvenientes = { "BUEY", "CACA", "CACO", "CAGA", "CAGO", "CAKA", "CAKO", "COGE", "COJA", "COJE", "COJI", "COJO", "CULO", "FETO", "GUEY", "JOTO", "KACA", "KACO", "KAGA", "KAGO", "KOGE", "KOJO", "KAKA", "KULO", "MAME", "MAMO", "MEAR", "MEAS", "MEON", "MION", "MOCO", "MULA", "PEDA", "PEDO", "PENE", "PUTA", "PUTO", "QULO", "RATA", "RUIN", "TETA", "VACA", "VAGA", "VAGO", "WEY" };
        private static readonly string[] Preposiciones = { "DA ", "DAS ", "DE ", "DEL ", "DER ", "DI ", "DIE ", "DD ", "EL ", "LA ", "LOS ", "LAS ", "LE ", "LES ", "MAC ", "MC ", "VAN ", "VON ", "Y " };

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
                    string baseRfc = ObtenerLetrasBase();

                    if (DateTime.TryParse(Fecha_Nac, out DateTime fecha))
                    {
                        string f = fecha.ToString("yyMMdd");
                        string homoclave = GenerarHomoclave(Nombre + Apellido_Paterno + Fecha_Nac, 3); // Tres caracteres para RFC.
                        return $"{baseRfc}{f}{homoclave}";
                    }
                    return "FECHA_ERR";
                }
                catch { return "ERROR"; }
            }
        }

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

                    string baseRfc = ObtenerLetrasBase();
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

                        string homoclaveCurp = GenerarHomoclave(baseRfc + f, 2); // 2 caracteres para CURP
                        return $"{baseRfc}{f}{s}{edo}{consP}{consM}{consN}{homoclaveCurp}";
                    }
                    return "FECHA_ERR";
                }
                catch { return "ERROR"; }
            }
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
        private string ObtenerLetrasBase()
        {
            string p = RemoverPreposiciones(Apellido_Paterno);
            string m = RemoverPreposiciones(Apellido_Materno);
            string n = ObtenerNombreValido(Nombre);

            char c1 = p.Length > 0 ? p[0] : 'X';
            char c2 = p.Length > 1 ? (p.Substring(1).FirstOrDefault(c => "AEIOU".Contains(c))) : 'X';
            if (c2 == '\0') c2 = 'X';
            char c3 = string.IsNullOrEmpty(m) ? 'X' : m[0];
            char c4 = n.Length > 0 ? n[0] : 'X';

            string base4 = $"{c1}{c2}{c3}{c4}";

            // Validar palabras inconvenientes
            if (Inconvenientes.Contains(base4))
                base4 = base4[0] + "X" + base4.Substring(2);

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
    public class CitizenController : Controller
    {
        /// <summary>
        /// Responde a la URL: /Citizen/Index
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            List<CitizenViewModel> listaCiudadanos = new List<CitizenViewModel>();

            // Recuperamos el JSON de manera segura desde TempData
            if (TempData["DatosCiudadanos"] is string jsonString)
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

            // Enviamos la lista real extraída de la API a la vista de manera limpia
            return View(listaCiudadanos);
        }
    }
}
