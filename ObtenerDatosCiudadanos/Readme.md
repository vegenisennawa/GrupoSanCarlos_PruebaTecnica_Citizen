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

**Notas sobre el cálculo de la homoclave en la CURP:** <br>
La RENAPO maneja una serie de fórmulas para calcular la homoclave:
* **a) Carácter 17 (Siglo):** Se asigna de forma determinista evaluando el año de nacimiento (numérico '0'-'9' para nacidos antes del 2000, y alfabético 'A'-'Z' para nacidos del 2000 en adelante).
    * **0:** Homoclave de siglo (Nacimiento antes del año 2000).
* **b) Carácter 18 (Dígito Verificador):** Se implementó el algoritmo matemático oficial de Módulo 10, el cual asigna un valor posicional decreciente (del 18 al 2) a cada uno de los primeros 17 caracteres, sumando los productos y calculando la diferencia del residuo de 10.

**Cálculo Matemático del Dígito Verificador (CURP):**
Para obtener el dígito `4`, el sistema asigna un valor a cada carácter (A-Z = 10-36, 0-9 = 0-9) y lo multiplica por un peso posicional descendente (del 18 al 2):

```text
A(10)x18 = 180  | U(31)x17 = 527 | H(17)x16 = 272 | J(19)x15 = 285
8(8)x14 = 112   | 8(8)x13 = 104  | 1(1)x12 = 12   | 2(2)x11 = 22
2(2)x10 = 20    | 0(0)x9 = 0     | M(22)x8 = 176  | J(19)x7 = 133
C(12)x6 = 72    | G(16)x5 = 80   | R(28)x4 = 112  | N(23)x3 = 69
0(0)x2 = 0
```

*Sumatoria total:* **2176**. Se calcula el residuo de la división entre 10 (`2176 % 10 = 6`). Finalmente, se resta el residuo a 10 (`10 - 6 = 4`). El dígito verificador es **4**.

CURP: **AUHJ881220MJCGRN04**

**Desglose de Reglas (Ejemplo RFC):**
* **A:** Primera letra del apellido paterno.
* **U:** Primera vocal interna del apellido paterno.
* **H:** Primera letra del apellido materno.
* **J:** Primera letra del primer nombre.
* **881220:** Año (88), Mes (12) y Día (20) de nacimiento.
* **XXX:** Homoclave generada por el sistema (3 caracteres alfanuméricos).

**Notas sobre el cálculo de la homoclave en el RFC:** <br>
El SAT maneja una serie de equivalencias y fórmulas para calcular la homoclave:

* **a) Asignación de valores numéricos a las letras:** A diferencia de una conversión de código ASCII tradicional, el SAT utiliza una tabla de equivalencias estricta (Anexo 22) donde se omiten intencionalmente las decenas cerradas (salta del 19 al 21, y del 29 al 32). Por lo tanto, se implementó un Dictionary (EquivalenciasSAT) para mapear cada carácter (A-Z, Ñ, & y espacios) a su valor exacto de dos dígitos en tiempo constante (O(1)), garantizando que letras como la 'A' valgan 11, la 'J' valga 21 y la 'Z' valga 39 de acuerdo con la ley.
    
    El nombre `AGUILA HERNANDEZ JUANA PAULINA` se traduce a la siguiente cadena (siempre anteponiendo un `0` inicial por norma del SAT):
    
    ```text
    0 (Base)
    A = 11 | G = 17 | U = 34 | I = 19 | L = 23 | A = 11 | (Espacio) = 00 
    H = 18 | E = 15 | R = 29 | N = 25 | A = 11 | N = 25 | D = 14 | E = 15 | Z = 39 | (Espacio) = 00 
    J = 21 | U = 34 | A = 11 | N = 25 | A = 11 | (Espacio) = 00 
    P = 27 | A = 11 | U = 34 | L = 23 | I = 19 | N = 25 | A = 11
    
    Cadena final: 
    0111734192311001815292511251415390021341125110027113423192511
    ```

* **b) Multiplicación cruzada:** Cuando el nombre se traduce en una serie de números se van multiplicando los números de la misma cadena por pares, yendo de izquierda a derecha, para así irlos sumando.
    
    Luego, se multiplica cada par de dígitos por el número inmediato a su derecha de forma sucesiva y se suman los resultados de toda la cadena:

    ```text
    (01x1) + (11x1) + (11x7) + (17x3) + (73x4) + (34x1) + (41x9) + (19x2) + 
    (92x3) + (23x1) + (31x1) + (11x0) + (10x0) + (00x1) + (01x8) + (18x1) + 
    (81x5) + (15x2) + (52x9) + (29x2) + (92x5) + (25x1) + (51x1) + (11x2) + 
    (12x5) + (25x1) + (51x4) + (14x1) + (41x5) + (15x3) + (53x9) + (39x0) + 
    (90x0) + (00x2) + (02x1) + (21x3) + (13x4) + (34x1) + (41x1) + (11x2) + 
    (12x5) + (25x1) + (51x1) + (11x0) + (10x0) + (00x2) + (02x7) + (27x1) + 
    (71x1) + (11x3) + (13x4) + (34x2) + (42x3) + (23x1) + (31x9) + (19x2) + 
    (92x5) + (25x1) + (51x1) 
    = 4562
    ```

* **c) Reducción (Módulo 34):** Del número resultante de la sumatoria se toman los últimos 3 dígitos, para luego dividirlos entre 34 y obtener tanto el cociente como el residuo.
    
    Como la sumatoria total del paso anterior fue **`4562`**, tomamos los últimos 3 dígitos (**`562`**) y aplicamos las operaciones matemáticas:
    * **Cociente:** `562 / 34 = 16`
    * **Residuo:** `562 % 34 = 18`

* **d) Traducción:** El SAT tiene una tabla oficial de 34 caracteres: **"123456789ABCDEFGHIJKLMNPQRSTUVWXYZ"**.
    * El primer carácter de la homoclave corresponde a la posición del cociente (`16`). En la tabla oficial, la posición 16 es la letra **`H`**.
    * El segundo carácter de la homoclave corresponde a la posición del residuo (`18`). En la tabla oficial, la posición 18 es la letra **`J`**.

* **e) Dígito verificador (Módulo 11):** El tercer carácter de la homoclave (posición 13 del RFC) es un *checksum* diseñado para validar la integridad de la cadena completa. Aunque para esta entrega base se asignó un "0" estático para optimizar tiempos de desarrollo, la arquitectura está preparada para implementar el algoritmo oficial, el cual consta de 4 pasos:
    1. **Mapeo:** Asignar un valor numérico a cada uno de los 12 caracteres previos (0-9 mantienen su valor, A-Z valen del 10 al 35).
    2. **Multiplicación por pesos:** Multiplicar cada valor por un peso posicional descendente (del 13 al 2) y sumar todos los resultados.
    3. **Residuo:** Obtener el residuo de la sumatoria dividida entre 11 (`suma % 11`).
    4. **Asignación:** Se calcula la diferencia `11 - residuo`. Si el resultado es 11, el dígito verificador es `0`; si el resultado es 10, es `A`; para cualquier otro caso (del 1 al 9), el dígito es exactamente el número resultante.

RFC: **AUHJ881220HJ0**

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