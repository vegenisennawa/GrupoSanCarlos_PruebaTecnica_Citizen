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

## 🪪 Cálculo de RFC y CURP (Personas Físicas)
Se implementó un motor de reglas para cumplir con las normativas vigentes, incluyendo diccionarios para omitir preposiciones (DE, LA, DEL, etc.) y un filtro de palabras inconvenientes. Al no contar con los registros originales del SAT/RENAPO, el sistema genera las homoclaves de manera automática.

**Ejemplo del algoritmo en acción:**
* **Datos:** Juana Paulina Águila Hernández (Femenino (Mujer), Jalisco, Nacimiento: 20-12-1988)
* **RFC Calculado:** `AUHJ881220XXX`
* **CURP Calculada:** `AUHJ881220MJCGRNXX`

**Desglose de Reglas (Ejemplo CURP):**
* **A:** Primera letra del apellido paterno.
* **U:** Primera vocal interna del apellido paterno.
* **H:** Primera letra del apellido materno.
* **J:** Primera letra del primer nombre.
* **881220:** Año (88), Mes (12) y Día (20) de nacimiento.
* **M:** Género (Mujer).
* **JC:** Clave del estado de nacimiento (Jalisco).
* **G:** Primera consonante interna del apellido paterno.
* **R:** Primera consonante interna del apellido materno.
* **N:** Primera consonante interna del primer nombre.
* **XX:** Homoclave generada por el sistema (2 caracteres alfanuméricos).

**Desglose de Reglas (Ejemplo RFC):**
* **A:** Primera letra del apellido paterno.
* **U:** Primera vocal interna del apellido paterno.
* **H:** Primera letra del apellido materno.
* **J:** Primera letra del primer nombre.
* **881220:** Año (88), Mes (12) y Día (20) de nacimiento.
* **XXX:** Homoclave generada por el sistema (3 caracteres alfanuméricos).