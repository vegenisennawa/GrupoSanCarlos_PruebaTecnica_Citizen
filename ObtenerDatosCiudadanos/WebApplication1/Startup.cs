using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using WebApplication1.Data;

namespace WebApplication1
{
    /// <summary>
    /// Método de inicio
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Este método agrega servicios al contenedor de dependencias
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Configura TempData para que use sesiones del servidor en lugar de cookies
            services.AddControllersWithViews()
                    .AddSessionStateTempDataProvider();

            // Agrega soporte para guardar datos en la memoria del servidor
            services.AddDistributedMemoryCache();

            // Configura las opciones del estado de la sesión
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(15); // Los datos expiran tras 15 min de inactividad
                options.Cookie.HttpOnly = true;                 // Mayor seguridad contra scripts maliciosos
                options.Cookie.IsEssential = true;              // Requerido para que funcione aunque el usuario bloquee cookies secundarias
            });

            // Agrega reintentos automáticos y protección contra caídas.
            services.AddHttpClient("SanCarlosAPI").AddStandardResilienceHandler();

            // Uso de cookies para protección del sistema.
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.LoginPath = "/Login/Index"; // A dónde mandamos al usuario si no está logueado
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Tiempo de sesión
                options.AccessDeniedPath = "/Home/Error";
            });

            // Conexión a la base de datos por defecto.
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
        }

        /// <summary>
        /// Este método configura el pipeline de solicitudes HTTP
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            // 4. ACTIVA EL MIDDLEWARE DE SESIÓN (Debe ir exactamente aquí, antes de Authorization y Endpoints)
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Login}/{action=Index}/{id?}");
            });
        }
    }
}
