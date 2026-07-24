# Arquitectura de AutoMarketRD

## Información general

- **Proyecto:** AutoMarketRD
- **Tipo:** API REST para marketplace de vehículos
- **Arquitectura:** Semi-Clean Architecture por capas
- **Framework:** ASP.NET Core / .NET 10
- **Base de datos:** PostgreSQL
- **ORM:** Entity Framework Core
- **Autenticación:** JWT Bearer
- **Almacenamiento de imágenes:** Amazon S3
- **Documentación de API:** OpenAPI y Scalar
- **Pruebas:** xUnit y Moq

## Propósito

AutoMarketRD es una API REST orientada a un marketplace de vehículos.
Permite a usuarios particulares y dealers registrarse, autenticarse,
crear y publicar anuncios de vehículos, subir imágenes, administrar
perfiles de dealer y manejar planes de suscripción para dealers.

## Alcance actual

El sistema incluye:

- Registro e inicio de sesión de usuarios
- Autenticación basada en JWT
- Usuarios compradores, dealers y administradores
- Creación, consulta, edición y publicación de anuncios
- Búsqueda paginada de anuncios
- Carga de imágenes asociadas a anuncios
- Perfil público y actualización de perfil para dealers
- Almacenamiento de archivos en Amazon S3
- Gestión de suscripciones y planes para dealers
- Monitoreo en segundo plano de suscripciones
- Pruebas unitarias para servicios principales

## Arquitectura general

El proyecto utiliza una arquitectura semi-Clean Architecture. La solución
se organiza en capas para separar responsabilidades y reducir el
acoplamiento entre la lógica de negocio, la capa HTTP y los detalles de
infraestructura.

```text
Cliente web / móvil
        |
        v
AutoMarket.API
Controllers, JWT, middleware, configuración HTTP
        |
        v
AutoMarket.Application
Servicios, casos de uso, DTOs, interfaces de aplicación
        |
        v
AutoMarket.Core
Entidades, reglas de negocio, contratos de repositorios
        ^
        |
AutoMarket.Infrastructure
EF Core, PostgreSQL, repositorios, JWT, S3, migraciones
```

## Regla de dependencias

Las dependencias deben dirigirse hacia las capas internas.

```text
API ----------> Application ----------> Core
Infrastructure -----------------------> Core
Infrastructure -----------------------> Application
Tests --------> Application / Core / Infrastructure
```

- `Core` no debe depender de ASP.NET Core, Entity Framework Core,
  PostgreSQL, Amazon S3 ni JWT.
- `Application` coordina casos de uso y depende de `Core`.
- `Infrastructure` contiene implementaciones técnicas de contratos
  definidos en las capas internas.
- `API` recibe solicitudes HTTP y delega las operaciones a servicios
  de Application.
- `Tests` valida las reglas y servicios del sistema de forma aislada.

## Estructura de la solución

```text
AutoMarketRD.sln
│
├── AutoMarket.API/
│   ├── Controllers/
│   │   ├── AnunciosController.cs
│   │   ├── AuthController.cs
│   │   └── DealersController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
│
├── AutoMarket.Application/
│   ├── DTOs/
│   │   ├── Anuncio/
│   │   ├── Dealer/
│   │   └── Usuario/
│   ├── Interfaces/
│   └── Services/
│
├── AutoMarket.Core/
│   ├── Entities/
│   │   ├── Anuncio.cs
│   │   ├── Usuario.cs
│   │   ├── PerfilDealer.cs
│   │   └── SuscripcionDealer.cs
│   ├── Entities/Enums/
│   ├── Entities/Exceptions/
│   ├── Entities/Filters/
│   └── Interfaces/
│
├── AutoMarket.Infrastructure/
│   ├── BackgroundServices/
│   ├── Data/
│   ├── Migrations/
│   ├── Repositories/
│   └── Services/
│
└── AutoMarket.Tests/
    └── Services/
```

## Responsabilidad de capas

### AutoMarket.Core

Representa el núcleo del negocio. Contiene las entidades, reglas de
dominio, enums, filtros, excepciones y contratos que no dependen de
tecnologías externas.

Entidades principales:

- `Usuario`: representa una cuenta del sistema y permite crear perfiles
  de dealer o administradores internos.
- `Anuncio`: representa una publicación de vehículo y contiene
  operaciones como actualizar información, publicar, agregar y eliminar fotos.
- `PerfilDealer`: representa la información pública y comercial de un dealer.
- `SuscripcionDealer`: representa el plan y estado de la suscripción de un dealer.

Contratos principales:

- `IUsuarioRepository`
- `IAnuncioRepository`
- `ISuscripcionRepository`
- `IAlmacenadorArchivos`

### AutoMarket.Application

Coordina los casos de uso de la aplicación. Contiene DTOs, interfaces de
servicios y servicios que aplican las reglas de negocio utilizando
entidades y repositorios.

Servicios principales:

| Servicio | Responsabilidad |
|---|---|
| `AuthService` | Registro, validación de credenciales e inicio de sesión |
| `AnuncioService` | Creación, consulta, búsqueda, actualización, publicación e imágenes |
| `PerfilDealerService` | Consulta y actualización del perfil de dealer |
| `SuscripcionService` | Asignación y cambio de planes de suscripción |

DTOs principales:

- `RegistroDto`
- `LoginDto`
- `AnuncioCreateDto`
- `AnuncioUpdateDto`
- `AnuncioDto`
- `AnuncioListadoDto`
- `AnuncioSearchDto`
- `PagedResult`
- `PerfilDealerPublicoDto`
- `PerfilDealerUpdateDto`

### AutoMarket.Infrastructure

Contiene las implementaciones vinculadas a tecnologías específicas.

Componentes principales:

| Componente | Responsabilidad |
|---|---|
| `ApplicationDbContext` | Configuración y acceso a la base de datos con EF Core |
| `UsuarioRepository` | Implementación de `IUsuarioRepository` |
| `AnuncioRepository` | Implementación de `IAnuncioRepository` |
| `SuscripcionRepository` | Implementación de `ISuscripcionRepository` |
| `TokenService` | Generación de tokens JWT |
| `AlmacenadorS3` | Carga y eliminación de archivos en Amazon S3 |
| `SuscripcionMonitorService` | Proceso en segundo plano para monitorear suscripciones |
| `Migrations` | Historial versionado de cambios de esquema de base de datos |

### AutoMarket.API

Es la capa de presentación y el punto de entrada HTTP. Configura
inyección de dependencias, autenticación, documentación de API y rutas.

Controllers principales:

| Controller | Responsabilidad |
|---|---|
| `AuthController` | Registro e inicio de sesión |
| `AnunciosController` | Gestión completa de anuncios y carga de imágenes |
| `DealersController` | Consulta pública y actualización del perfil de dealer |

### AutoMarket.Tests

Contiene pruebas unitarias de los servicios de aplicación utilizando
xUnit y Moq. El objetivo es validar reglas de negocio sin depender de
la base de datos ni servicios externos.

Pruebas actuales:

- `AnuncioServiceTests`
- `UsuarioServiceTests`
- `PerfilDealerServiceTests`
- `SuscripcionServiceTests`

## Módulos funcionales

### Autenticación y usuarios

Permite registrar usuarios, validar que un correo no esté repetido e
iniciar sesión. Cuando las credenciales son válidas, se genera un token JWT.

Flujo:

```text
POST /auth/registrar o POST /auth/login
        |
        v
AuthController
        |
        v
IAuthService / AuthService
        |
        v
IUsuarioRepository
        |
        v
UsuarioRepository + ApplicationDbContext
        |
        v
PostgreSQL
```

### Anuncios

Gestiona la publicación de vehículos. Incluye creación, consulta,
actualización, publicación, búsqueda paginada y carga de imágenes.

Reglas de negocio documentadas:

- Solo el propietario puede actualizar o publicar su anuncio.
- Un usuario particular tiene límites de anuncios.
- Un dealer puede publicar según las reglas de su suscripción.
- Las imágenes deben cumplir las validaciones de tamaño y formato.
- Un anuncio debe existir antes de actualizarlo, publicarlo o subir imágenes.

### Perfil de dealer

Permite consultar públicamente el perfil de un dealer y que el dealer
autenticado actualice su propia información, incluyendo su logo.

Reglas de negocio:

- Solo un usuario dealer puede actualizar su perfil.
- El logo debe cumplir validaciones de extensión y tamaño.
- Los archivos se almacenan mediante la abstracción `IAlmacenadorArchivos`.

### Suscripciones

Administra los planes de los dealers y sus restricciones.

Reglas de negocio:

- Un dealer no puede tener más de una suscripción activa asignada por
  el flujo inicial.
- No se puede cambiar al mismo plan actual.
- Una suscripción cancelada no puede cambiar de plan.
- El plan controla si el dealer puede publicar nuevos anuncios.
- Un servicio en segundo plano monitorea el estado de las suscripciones.

## Flujo: publicar un anuncio

```text
1. El usuario envía una petición autenticada.
2. AnunciosController obtiene el identificador del usuario desde el JWT.
3. AnuncioService busca el anuncio mediante IAnuncioRepository.
4. El servicio verifica que el usuario sea el propietario.
5. Se aplican las reglas de publicación del dominio.
6. El repositorio actualiza el anuncio mediante EF Core.
7. La API devuelve la respuesta HTTP correspondiente.
```

## Persistencia

La persistencia utiliza Entity Framework Core y PostgreSQL mediante
`ApplicationDbContext`.

Las migraciones registran cambios de estructura, incluyendo:

- Migración inicial
- Cambio para publicar al guardar
- Soporte de fotos en anuncios
- Sistema de usuarios
- Campos para perfil de dealer
- Suscripción SaaS para dealers

## Seguridad

- La autenticación se basa en JWT Bearer.
- Los endpoints que modifican recursos requieren un usuario autenticado.
- El identificador del usuario autenticado se obtiene desde los claims
  del token.
- La capa Application valida que el usuario sea dueño del anuncio o
  perfil que intenta modificar.
- Las contraseñas deben almacenarse como hash usando BCrypt.
- Las credenciales de PostgreSQL, JWT y Amazon S3 deben configurarse
  por variables de entorno o secretos; nunca deben subirse al repositorio.

## Integraciones externas

| Integración | Uso |
|---|---|
| PostgreSQL | Persistencia relacional de usuarios, anuncios y suscripciones |
| Amazon S3 | Almacenamiento de imágenes de anuncios y logos de dealers |
| JWT | Autenticación stateless para solicitudes HTTP |
| Scalar / OpenAPI | Exploración y documentación de endpoints |

## Decisiones arquitectónicas

### ADR-001: Arquitectura por capas

Se utiliza una arquitectura por capas con principios de Clean Architecture
para separar el núcleo de negocio de los detalles de infraestructura.

La solución no busca aplicar abstracciones innecesarias. Por ello se
considera una semi-Clean Architecture: mantiene límites claros, pero
prioriza simplicidad y velocidad de desarrollo cuando una abstracción
adicional no aporta valor.

### ADR-002: Repositorios como contratos

Los repositorios se definen mediante interfaces en Core y se implementan
en Infrastructure. Esto evita que los servicios de Application dependan
directamente de Entity Framework Core.

### ADR-003: Almacenamiento de archivos desacoplado

La aplicación depende de `IAlmacenadorArchivos`, mientras que
`AlmacenadorS3` implementa el almacenamiento concreto en Amazon S3.
Esto permite reemplazar S3 por otra solución sin modificar los casos de uso.

### ADR-004: Servicio en segundo plano

`SuscripcionMonitorService` se ejecuta como servicio de fondo para
procesar reglas relacionadas con el estado de las suscripciones, sin
depender de una solicitud HTTP.

## Pruebas

Las pruebas unitarias se enfocan en los servicios de Application.

Ejemplos de escenarios cubiertos:

- Registro con correo existente.
- Registro correcto de comprador y dealer.
- Login con credenciales válidas e inválidas.
- Creación de anuncio para usuario particular y dealer.
- Restricción de anuncios para usuario particular.
- Actualización y publicación solo por el propietario.
- Validación de imágenes por tamaño.
- Actualización de perfil de dealer.
- Asignación y cambio de plan de suscripción.
- Restricciones para suscripciones canceladas.

## Mejoras pendientes

- Agregar middleware global para convertir excepciones de negocio en
  respuestas HTTP uniformes.
- Separar DTOs de entrada (`Request`) y salida (`Response`) si el
  proyecto crece.
- Agregar pruebas de integración para controllers, autenticación y EF Core.
- Incorporar paginación, filtros y ordenamiento documentados en OpenAPI.
- Agregar política de autorización por roles.
- Documentar variables de entorno con un archivo `.env.example`.
- Definir estrategia de renovación o expiración de tokens JWT.