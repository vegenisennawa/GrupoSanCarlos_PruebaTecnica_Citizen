using Microsoft.AspNetCore.Mvc;
using System;                     
using System.Collections.Generic;
using System.Linq;                
using System.Text.Json;

namespace WebApplication1.Controllers
{
    // Esta es la estructura que representará a cada ciudadano
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

        /// <summary>
        /// 
        /// </summary>
        public string Rfc_Calculado
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(Nombre) || string.IsNullOrEmpty(Apellido_Paterno) || string.IsNullOrEmpty(Fecha_Nac))
                        return "INCOMPLETO";

                    string p = Apellido_Paterno.ToUpper().Trim();
                    string m = (Apellido_Materno ?? "").ToUpper().Trim();
                    string n = Nombre.ToUpper().Trim();

                    // 1. Primera letra del apellido paterno y primera vocal interna
                    char c1 = p[0];
                    char c2 = p.Substring(1).FirstOrDefault(c => "AEIOU".Contains(c));
                    if (c2 == '\0') c2 = 'X';

                    // 2. Primera letra del apellido materno (o 'X' si no tiene)
                    char c3 = !string.IsNullOrEmpty(m) ? m[0] : 'X';

                    // 3. Primera letra del primer nombre
                    char c4 = n[0];

                    // 4. Fecha de nacimiento (YYMMDD) extraída de una fecha estándar (ej: 1995-10-25)
                    // Ajusta este parse si tu formato de texto de fecha es diferente
                    if (DateTime.TryParse(Fecha_Nac, out DateTime fecha))
                    {
                        string f = fecha.ToString("yyMMdd");
                        return $"{c1}{c2}{c3}{c4}{f}";
                    }
                    return "FECHA_ERR";
                }
                catch
                {
                    return "ERROR";
                }
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
                    if (string.IsNullOrEmpty(Nombre) || string.IsNullOrEmpty(Apellido_Paterno) || string.IsNullOrEmpty(Sexo) || string.IsNullOrEmpty(Clave_Edo_Nac))
                        return "INCOMPLETO";

                    // Base del RFC compartida (Primeras 4 letras + 6 números de fecha)
                    string baseRfc = Rfc_Calculado;
                    if (baseRfc == "ERROR" || baseRfc == "INCOMPLETO" || baseRfc == "FECHA_ERR") return "ERROR_BASE";

                    // Sexo (H/M) y Estado (2 dígitos de RENAPO)
                    string s = Sexo.ToUpper().Substring(0, 1);
                    string edo = Clave_Edo_Nac.ToUpper().PadRight(2, 'X').Substring(0, 2);

                    // Consonantes internas del Paterno, Materno y Nombre
                    char consP = ObtenerPrimeraConsonanteInterna(Apellido_Paterno);
                    char consM = ObtenerPrimeraConsonanteInterna(Apellido_Materno);
                    char consN = ObtenerPrimeraConsonanteInterna(Nombre);

                    return $"{baseRfc}{s}{edo}{consP}{consM}{consN}";
                }
                catch
                {
                    return "ERROR";
                }
            }
        }

        private char ObtenerPrimeraConsonanteInterna(string cadena)
        {
            if (string.IsNullOrEmpty(cadena)) return 'X';
            string str = cadena.ToUpper().Trim();
            // Saltamos la primera letra (índice 0) y buscamos la primera consonante
            char cons = str.Substring(1).FirstOrDefault(c => !"AEIOU ".Contains(c));
            return cons != '\0' ? cons : 'X';
        }
    }

    public class CitizenController : Controller
    {
        // Responde a la URL: /Citizen/Index
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
