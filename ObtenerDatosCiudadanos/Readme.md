# Proyecto de Consulta de Ciudadanos - Grupo San Carlos

**Desarrolladora:** Juana Paulina Águila Hernández

## 🚀 Notas de la Migración
Durante la sesión presencial en las oficinas, el proyecto se inició utilizando **.NET 5**. Para esta entrega final, la arquitectura fue migrada y refactorizada exitosamente a **.NET 8**, utilizando Visual Studio 2022.

## 🔐 Arquitectura de Seguridad (Login)
* **Base de Datos:** El inicio de sesión está conectado a una tabla en SQL Server (Entity Framework Core) para validar las credenciales reales.
* **Autenticación Nativa:** Se implementó seguridad mediante *Cookies* y *Claims* de ASP.NET Core para proteger el acceso a los controladores (decorador `[Authorize]`).
* **Encriptación Estándar:** Las contraseñas en la base de datos están protegidas con **Bcrypt**. Se eligió este algoritmo porque su factor de trabajo obliga al procesador a tomarse un tiempo adicional (mitigando ataques de fuerza bruta), y su sistema de "salting" automático garantiza que los hashes sean distintos incluso si dos usuarios tienen la misma contraseña.

**Usuario:** admin <br>
**Contraseña:** inmobiliaria2026

## 🌐 Flujo de Datos
Al momento de que el usuario pasa la validación del login, el sistema ejecuta internamente el consumo de la API de Grupo San Carlos. La respuesta JSON se transfiere de forma segura en memoria (`TempData`) hacia la vista final, garantizando que los datos ciudadanos solo se carguen si la autenticación fue exitosa.

## 🌐 Consumo de API
Para evitar el agotamiento de sockets (*socket exhaustion*), la conexión se gestiona mediante `IHttpClientFactory`. Además, se implementó el paquete nativo `Microsoft.Extensions.Http.Resilience` de .NET 8, el cual configura automáticamente políticas de resiliencia avanzadas (patrones de *Retry* y *Circuit Breaker*) para proteger la aplicación ante posibles caídas o intermitencias del servicio externo, sin necesidad de programar políticas manuales complejas.