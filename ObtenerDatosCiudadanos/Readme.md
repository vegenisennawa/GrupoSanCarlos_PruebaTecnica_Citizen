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

Se debe de tomar en cuenta que, si la persona solo tiene un apellido:
* En la CURP se coloca una X en la primera letra del apellido materno.
* En el RFC, las letras 3 y 4 corresponden a la primera y segunda letra del primer nombre (o único nombre).

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

## ⚙️ Operaciones CRUD y Persistencia de Datos
* **Mapeo de Datos Estricto (Strong Typing):** Se homologó la estructura del modelo en C# con las columnas de la tabla en SQL Server. Se garantizó que la información se almacene con su tipo de dato nativo más cercano a la realidad (implementando *Value Converters* en Entity Framework para transformar las fechas al tipo `DATE` estricto en SQL).
* **Borrado Lógico (Soft Delete):** Por políticas de historial, auditoría y seguridad, el sistema no ejecuta eliminaciones físicas en la base de datos. En su lugar, se implementó un indicador de estado (`Activo` tipo `bit`) que inactiva el registro, ocultándolo de la vista principal pero preservando la integridad de los datos.
* **Validación de Estado:** El sistema evalúa dinámicamente si un registro existe físicamente en SQL Server o si solo reside en la memoria temporal de la API. Si la información no ha sido persistida en la base de datos, la interfaz inactiva la funcionalidad de "Eliminar", previniendo transacciones nulas.

## ✨ Interfaz y Experiencia de Usuario (UI/UX)
* **Validación Front-End:** Se blindaron las vistas mediante validaciones nativas de HTML5 y expresiones regulares (Regex) en tiempo real para garantizar la captura de correos electrónicos y teléfonos con formato válido antes de procesar cualquier solicitud.
* **Modales Modernos:** Se integró la librería **SweetAlert2** para manejar las confirmaciones de eliminación de registros, reemplazando las alertas genéricas del navegador por cuadros de diálogo estéticos, responsivos y profesionales.

## 📝 Preguntas de Criterio Técnico

**1. Autenticación:** *¿Qué método o tecnología de autenticación elegiste implementar en .NET 8 y cuáles fueron los motivos técnicos para seleccionarlo sobre otras alternativas en un esquema MVC?*

**Respuesta:** 
Elegí implementar la autenticación nativa de ASP.NET Core basada en **Cookies y Claims**. En un esquema MVC arquitectónicamente tradicional (donde el servidor es quien renderiza las vistas), el uso de Cookies es el estándar más seguro, nativo y eficiente para mantener el estado de la sesión, a diferencia de tecnologías como JWT (JSON Web Tokens), las cuales son ideales para arquitecturas desacopladas (SPAs o APIs RESTful puras) pero añaden una complejidad innecesaria en la gestión del lado del cliente para MVC. Adicionalmente, el resguardo de credenciales se delegó a **Bcrypt** para aprovechar su sistema de "salting" y mitigación de ataques por fuerza bruta.

---

**2. Consumo de API en Producción:** *En un escenario de producción con más de 100,000 registros, ¿cómo optimizarías el consumo de la API y el cálculo masivo de RFC/CURP para no congelar la interfaz de usuario?*

**Respuesta:**
Para garantizar una experiencia fluida en la UI y no saturar la memoria del servidor, implementaría las siguientes estrategias combinadas:

*   **Manejo de carga en segundo plano:** Delegar el consumo masivo de la API y los cálculos matemáticos pesados (RFC/CURP) a tareas asíncronas (como un *BackgroundService* o WebWorkers), evitando así bloquear el hilo principal de la interfaz de usuario.
*   **Paginación:** Implementar paginación desde el servidor (*Server-Side Pagination*) para traer y renderizar los datos en bloques pequeños bajo demanda, en lugar de intentar cargar 100,000 nodos en el DOM simultáneamente.
*   **Caché de información:** Implementar una capa de caché (por ejemplo, *MemoryCache* o Redis) para almacenar temporalmente los resultados y servir consultas recurrentes más rápido.
*   **Evitar redundancia mediante deltas:** Validar datos clave utilizando una **fecha de última actualización**. De esta forma, las sincronizaciones posteriores solo calcularían e insertarían los registros nuevos o modificados, evitando el recálculo masivo e innecesario de toda la base de datos.