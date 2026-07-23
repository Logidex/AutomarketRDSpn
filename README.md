```markdown
# 🚗 AutoMarketRD - Backend API

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-10.0-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![AWS S3](https://img.shields.io/badge/AWS%20S3-Storage-FF9900?style=for-the-badge&logo=amazons3&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-008080?style=for-the-badge)
![Tests](https://img.shields.io/badge/Testing-xUnit%20%26%20Moq-00BFFF?style=for-the-badge)

**AutoMarketRD** es una plataforma de marketplace automotriz diseñada para el mercado de República Dominicana. Permite a vendedores particulares y concesionarios (*dealers*) publicar, administrar y promocionar vehículos en venta con un enfoque de alto rendimiento, filtrado avanzado y arquitectura limpia (*Clean Architecture*).

---

## 📐 Arquitectura del Proyecto

El proyecto está construido siguiendo los principios de **Clean Architecture** (Onion Architecture), asegurando una estricta separación de responsabilidades, alta mantenibilidad y testabilidad:

```text
AutoMarketRD/
├── 🌐 AutoMarket.API            --> Controladores REST, Middlewares, Configuración OpenAPI & Scalar
├── ⚙️ AutoMarket.Application    --> Casos de Uso, Servicios de Aplicación, DTOs e Interfaces
├── 🧠 AutoMarket.Core           --> Entidades de Dominio, Reglas de Negocio, Enums e Interfaces Core
├── 🏗️ AutoMarket.Infrastructure --> EF Core, Repositorios PostgreSQL, Servicio AWS S3 y JWT
└── 🧪 AutoMarket.Tests          --> Pruebas Unitarias e Integradas (xUnit & Moq)

```

---

## 🛠️ Tecnologías y Librerías

* **Framework:** .NET 10.0 (ASP.NET Core Web API)
* **Base de Datos:** PostgreSQL con Entity Framework Core 10.0 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
* **Almacenamiento Multimedia:** AWS S3 (`AWSSDK.S3`) para imágenes de anuncios y logos de dealers
* **Autenticación:** JWT (JSON Web Tokens) + Encriptación de claves con `BCrypt.Net-Next`
* **Documentación Interactiva:** `Scalar.AspNetCore` + OpenAPI 3.0
* **Testing:** xUnit, Moq y Coverlet Collector

---

## 📖 Inventario de Módulos y Métodos

### 🌐 1. Capa API (`AutoMarket.API`)

#### `AnunciosController`

* `POST /api/anuncios` -> **`CrearAnuncio`**: Crea un nuevo anuncio en estado borrador o activo.
* `GET /api/anuncios/{id}` -> **`ObtenerPorId`**: Retorna el detalle completo de un anuncio por ID.
* `GET /api/anuncios` -> **`ObtenerTodosLosAnuncios`**: Devuelve el catálogo general.
* `GET /api/anuncios/buscar` -> **`BuscarAnuncios`**: Búsqueda avanzada con filtros combinados (marca, modelo, año, precio, km, transmisión, etc.) y paginación (`AnuncioSearchDto`).
* `PUT /api/anuncios/{id}` -> **`ActualizarAnuncio`**: Modifica la información técnica del anuncio.
* `PATCH /api/anuncios/{id}/publicar` -> **`Publicar`**: Transiciona el estado del anuncio a publicado.
* `POST /api/anuncios/{id}/imagenes` -> **`SubirImagenes`**: Sube imágenes multipart (`List<IFormFile>`) directamente a AWS S3.

#### `AuthController`

* `POST /api/auth/registrar` -> **`Registrar`**: Registro de usuarios particulares y concesionarios (creando su `PerfilDealer`).
* `POST /api/auth/login` -> **`Login`**: Autentica credenciales y emite el token JWT.

#### `DealersController`

* `GET /api/dealers/{id}/perfil` -> **`ObtenerPerfilPublico`**: Muestra la vitrina del dealer con sus datos de contacto e inventario.
* `PUT /api/dealers/mi-perfil` -> **`ActualizarMiPerfil`**: Actualiza datos de la empresa y logo en S3.

---

### ⚙️ 2. Capa de Aplicación (`AutoMarket.Application`)

#### `AnuncioService`

* `CrearAnuncioAsync(dto, usuarioId)`: Valida y persiste un nuevo anuncio asociado al usuario.
* `SubirImagenesAsync(anuncioId, usuarioId, imagenes)`: Valida formato, tamaño (< 5MB) y propiedad del anuncio antes de almacenar en S3.
* `BuscarAnunciosAsync(searchDto)`: Transforma la consulta web a `AnuncioQueryFilter` y retorna un `PagedResult<AnuncioListadoDto>`.
* `PublicarAnuncioAsync(anuncioId, usuarioId)`: Valida cuotas de publicación y ejecuta la lógica de dominio `.Publicar()`.

#### `AuthService`

* `RegistrarUsuarioAsync(registroDto)`: Valida unicidad de correo, cifra la contraseña con BCrypt y configura el perfil dealer si aplica.
* `LoginAsync(loginDto)`: Valida las credenciales e invoca `ITokenService` para firmar el JWT.

#### `PerfilDealerService`

* `ObtenerPerfilPublicoAsync(dealerId)`: Construye la respuesta DTO del perfil del dealer.
* `ActualizarMiPerfilAsync(usuarioId, dto)`: Procesa y valida el logo del concesionario y actualiza la información institucional.

#### `SuscripcionService`

* `AsignarPlanInicialAsync(dealerId)`: Otorga la suscripción básica al crear la cuenta de dealer.
* `CambiarPlanAsync(dealerId, nuevoNivel, ciclo)`: Actualiza las cuotas y el nivel del SaaS (`Básico`, `Destacado`, `Premium`).

---

### 🧠 3. Capa Core (`AutoMarket.Core`)

#### Entidades Principales

* **`Anuncio`**: Encapsula métodos de dominio como `.AgregarFotos()`, `.Publicar()`, `.ActualizarInfo()` y `.EliminarFoto()`.
* **`Usuario`**: Maneja creación de perfiles dealer (`.CrearPerfilDealer()`) y roles internos.
* **`PerfilDealer`**: Métodos `.ActualizarPerfil()` y `.ActualizarLogo()`.
* **`SuscripcionDealer`**: Reglas de negocio SaaS como `.PermiteNuevosAnuncios()` y `.ActualizarNivel()`.

---

### 🏗️ 4. Capa de Infraestructura (`AutoMarket.Infrastructure`)

* **`AlmacenadorS3`**: Implementa `IAlmacenadorArchivos` gestionando `PutObjectAsync` y `DeleteObjectAsync` en Amazon S3.
* **`TokenService`**: Implementa `ITokenService` generando JWTs firmados con los *Claims* de seguridad.
* **`AnuncioRepository`**: Consultas optimizadas con EF Core, filtrado dinámico y paginación eficiente.
* **`UsuarioRepository`**: Operaciones de persistencia con la tabla `Usuarios` e inserción en cascada de `PerfilDealer`.
* **`SuscripcionRepository`**: Gestión de la persistencia de los planes SaaS.

---

## 🚀 Instalación y Configuración Local

### Prerrequisitos

1. [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. Instancia activa de [PostgreSQL](https://www.postgresql.org/)
3. Bucket de [AWS S3](https://aws.amazon.com/s3/) (o Credenciales para desarrollo)

### Pasos de Configuración

1. **Clonar el repositorio:**
```bash
git clone [https://github.com/tu-usuario/AutoMarketRD.git](https://github.com/tu-usuario/AutoMarketRD.git)
cd AutoMarketRD

```


2. **Configurar las variables en `AutoMarket.API/appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=AutoMarketDb;Username=postgres;Password=tu_password"
  },
  "Jwt": {
    "Secret": "TU_CLAVE_SECRETA_SUPER_SEGURA_CON_MINIMO_32_CARACTERES",
    "Issuer": "AutoMarketAPI",
    "Audience": "AutoMarketWeb"
  },
  "AWS": {
    "BucketName": "tu-bucket-automarket",
    "Region": "us-east-1"
  }
}

```


3. **Ejecutar las Migraciones de Base de Datos:**
```bash
dotnet ef database update --project AutoMarket.Infrastructure --startup-project AutoMarket.API

```


4. **Iniciar la aplicación:**
```bash
dotnet run --project AutoMarket.API

```


5. **Acceder a la Documentación Interactiva con Scalar:**
Abre tu navegador en:
`https://localhost:7157/scalar/v1`

---

## 🧪 Pruebas Unitarias

El proyecto cuenta con una suite completa de pruebas unitarias que cubren casos de éxito y manejo de excepciones de negocio:

```bash
dotnet test

```

Para generar reporte de cobertura con Coverlet:

```bash
dotnet test --collect:"XPlat Code Coverage"

```

---

## 📝 Licencia

Este proyecto es de propiedad privada y desarrollo activo para **AutoMarketRD**.