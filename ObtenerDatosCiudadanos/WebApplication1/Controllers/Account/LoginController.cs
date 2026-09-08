using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace WebApplication1.Controllers
{

    // Esta clase representa la estructura completa del JSON de la imagen
    public class ApiResponseWrapper
    {
        public int CodigoEstatus { get; set; }
        public string Mensaje { get; set; }
        public List<CitizenViewModel> Respuesta { get; set; } // Aquí está la lista real
    }

    public class LoginController : Controller
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string usuario, string password)
        {
            try
            {
                string urlServicio = "https://krm.gruposancarlos.com:1443/api/Procesos/RetoCandidato?token=1ef0c880-002c-435b-b734-782c7575a6ad&num_registros=100";

                HttpResponseMessage response = await _httpClient.GetAsync(urlServicio);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();

                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // SOLUCIÓN: Deserializamos al contenedor raíz, NO a la lista directamente
                    var resultadoApi = JsonSerializer.Deserialize<ApiResponseWrapper>(jsonString, opciones);

                    // De aquí puedes extraer la lista si necesitas validar algo antes de redirigir
                    List<CitizenViewModel> listaCiudadanos = resultadoApi?.Respuesta ?? new List<CitizenViewModel>();

                    // Guardamos el JSON crudo en TempData para pasarlo al siguiente controlador
                    TempData["DatosCiudadanos"] = jsonString;

                    return RedirectToAction("Index", "Citizen");
                }
                else
                {
                    ViewBag.Error = $"Error en el servicio: {response.StatusCode}";
                    return View();
                }
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = $"No se pudo conectar al servicio: {ex.Message}";
                return View();
            }
        }
    }
}
