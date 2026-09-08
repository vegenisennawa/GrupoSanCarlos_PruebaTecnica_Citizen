using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class ApiResponseWrapper
    {
        public int CodigoEstatus { get; set; }
        public string Mensaje { get; set; }
        public List<CitizenViewModel> Respuesta { get; set; } // Aquí está la lista real
    }

    /// <summary>
    /// 
    /// </summary>
    public class LoginController : Controller
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly AppDbContext _context;

        public LoginController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usuario"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Index(string usuario, string password)
        {
            if (!_context.Usuarios.Any())
            {
                var nuevoUsuario = new Usuario
                {
                    NombreUsuario = "admin",
                    // ¡Aquí ocurre la magia de la encriptación!
                    Password = BCrypt.Net.BCrypt.HashPassword("inmobiliaria2026")
                };
                _context.Usuarios.Add(nuevoUsuario);
                _context.SaveChanges();
            }

            var userDb = _context.Usuarios.FirstOrDefault(u => u.NombreUsuario == usuario);

            //if (usuario == "admin" && password == "inmobiliaria2026")
            if (userDb != null && BCrypt.Net.BCrypt.Verify(password, userDb.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario),
                    new Claim(ClaimTypes.Role, "Administrador")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                try
                {
                    string urlServicio = "https://krm.gruposancarlos.com:1443/api/Procesos/RetoCandidato?token=1ef0c880-002c-435b-b734-782c7575a6ad&num_registros=100";
                    HttpResponseMessage response = await _httpClient.GetAsync(urlServicio);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();
                        TempData["DatosCiudadanos"] = jsonString;
                        return RedirectToAction("Index", "Citizen");
                    }
                }
                catch (System.Exception ex)
                {
                    ViewBag.Error = "Error al obtener los datos de la API: " + ex.Message;
                }
            }

            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }
    }
}
