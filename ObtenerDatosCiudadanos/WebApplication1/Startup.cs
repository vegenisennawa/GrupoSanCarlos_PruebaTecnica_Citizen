using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace WebApplication1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // Este método agrega servicios al contenedor de dependencias
        public void ConfigureServices(IServiceCollection services)
        {
            // 1. Configura TempData para que use sesiones del servidor en lugar de cookies
            services.AddControllersWithViews()
                    .AddSessionStateTempDataProvider();

            // 2. Agrega soporte para guardar datos en la memoria del servidor
            services.AddDistributedMemoryCache();

            // 3. Configura las opciones del estado de la sesión
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(15); // Los datos expiran tras 15 min de inactividad
                options.Cookie.HttpOnly = true;                // Mayor seguridad contra scripts maliciosos
                options.Cookie.IsEssential = true;             // Requerido para que funcione aunque el usuario bloquee cookies secundarias
            });
        }

        // Este método configura el pipeline de solicitudes HTTP
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
