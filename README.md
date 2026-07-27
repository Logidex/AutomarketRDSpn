# Arquitectura de AutoMarketRD

## Información general

- **Proyecto:** AutoMarketRD
- **Tipo:** API REST para marketplace de vehículos
- **Arquitectura:** Semi-Clean Architecture por capas
- **Framework:** ASP.NET Core / .NET 10
- **Base de datos:** PostgreSQL
- **ORM:** Entity Framework Core
- **Autenticación:** JWT Bearer
- **Almacenamiento de imágenes:** Amazon S3 (Abstraído)
- **Notificaciones:** SMTP Client nativo (C#)
- **Documentación de API:** OpenAPI y Scalar
- **Pruebas:** xUnit y Moq

## Propósito

AutoMarketRD es una API REST orientada a un marketplace de vehículos.
Permite a usuarios particulares y dealers registrarse, autenticarse,
crear y publicar anuncios de vehículos, subir imágenes, administrar
perfiles de dealer, manejar planes de suscripción y recibir contactos (leads)
directamente en sus bandejas de entrada mediante un sistema de notificaciones.

## Alcance actual

El sistema incluye:

- Registro e inicio de sesión de usuarios.
- Autenticación y Autorización basada en JWT.
- Usuarios compradores, dealers y administradores.
- Creación, consulta, edición y publicación de anuncios.
- Búsqueda paginada de anuncios.
- Carga de imágenes asociadas a anuncios.
- Perfil público y actualización de perfil para dealers.
- Almacenamiento de archivos desacoplado.
- Gestión de suscripciones y planes SaaS para dealers.
- Monitoreo en segundo plano de morosidad de suscripciones.
- **Captación de leads (contactos) anónimos y autenticados.**
- **Envío de alertas por correo electrónico vía servidor SMTP.**
- Pruebas unitarias para servicios principales (Cobertura 100% en flujos críticos).

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
Entidades, reglas de negocio, contratos de repositorios y notificaciones
        ^
        |
AutoMarket.Infrastructure
EF Core, PostgreSQL, Repositorios, JWT, S3, SMTP, Migraciones
```

## Regla de dependencias

Las dependencias deben dirigirse hacia las capas internas.

- `Core` no debe depender de ASP.NET Core, Entity Framework Core, PostgreSQL, Amazon S3, SMTP ni JWT.
- `Application` coordina casos de uso y depende de `Core`.
- `Infrastructure` contiene implementaciones técnicas de contratos definidos en las capas internas.
- `API` recibe solicitudes HTTP y delega las operaciones a servicios de `Application`.
- `Tests` valida las reglas y servicios del sistema de forma aislada.

## Estructura de la solución

```text
AutoMarketRD.sln
│
├── AutoMarket.API/
│   ├── Controllers/
│   │   ├── AnunciosController.cs
│   │   ├── AuthController.cs
│   │   ├── DealersController.cs
│   │   └── LeadsController.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── AutoMarket.Application/
│   ├── DTOs/
│   │   ├── Anuncio/
│   │   ├── Dealer/
│   │   ├── Lead/
│   │   └── Usuario/
│   ├── Interfaces/
│   └── Services/
│
├── AutoMarket.Core/
│   ├── Entities/
│   │   ├── Anuncio.cs
│   │   ├── Lead.cs
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
    ├── Controllers/
    └── Services/
```

## Responsabilidad de capas

### AutoMarket.Core

Representa el núcleo del negocio. Contiene las entidades, reglas de
dominio, enums, filtros, excepciones y contratos que no dependen de
tecnologías externas.

Entidades principales:

- `Usuario`: representa una cuenta del sistema y permite crear perfiles de dealer o administradores internos.
- `Anuncio`: representa una publicación de vehículo y contiene operaciones como actualizar información, publicar, agregar y eliminar fotos.
- `PerfilDealer`: representa la información pública y comercial de un dealer.
- `SuscripcionDealer`: representa el plan y estado de la suscripción de un dealer.
- `Lead`: representa una intención de compra o contacto hacia un anuncio específico.

Contratos principales:

- `IUsuarioRepository`
- `IAnuncioRepository`
- `ISuscripcionRepository`
- `IAlmacenadorArchivos`
- `ILeadRepository`: Contrato para guardar y consultar mensajes.
- `IEmailSenderService`: Contrato de dominio para enviar correos, sin atarse a un proveedor.

### AutoMarket.Application

Coordina los casos de uso de la aplicación. Aplica las reglas de negocio
utilizando entidades, repositorios y servicios externos.

Servicios principales:

| Servicio | Responsabilidad |
|---|---|
| `AuthService` | Registro, validación de credenciales e inicio de sesión |
| `AnuncioService` | Creación, consulta, actualización, publicación e imágenes |
| `PerfilDealerService` | Consulta y actualización del perfil de comercial |
| `SuscripcionService` | Asignación y cambio de planes de suscripción |
| `LeadService` | Orquestación de captación de leads y disparador de correos |

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
- `LeadCreateDto`
- `LeadDto`

### AutoMarket.Infrastructure

Contiene las implementaciones vinculadas a tecnologías específicas.

Componentes principales:

| Componente | Responsabilidad |
|---|---|
| `ApplicationDbContext` | Configuración y acceso a BD con EF Core |
| `UsuarioRepository` | Implementación de `IUsuarioRepository` |
| `AnuncioRepository` | Implementación de `IAnuncioRepository` |
| `SuscripcionRepository` | Implementación de `ISuscripcionRepository` |
| `LeadRepository` | Implementación de `ILeadRepository` |
| `TokenService` | Generación de tokens JWT |
| `AlmacenadorS3` | Carga y eliminación de archivos en Amazon S3 |
| `SuscripcionMonitorService` | Proceso Background (`IHostedService`) para morosos |
| `SmtpEmailSenderService` | Implementación real de envío de correos vía `System.Net.Mail` |

### AutoMarket.API

Capa de presentación y punto de entrada HTTP. Configura inyección de
dependencias, autenticación, documentación de API y rutas.

Controllers principales:

| Controller | Responsabilidad |
|---|---|
| `AuthController` | Autenticación y registro |
| `AnunciosController` | Gestión del catálogo de vehículos |
| `DealersController` | Gestión de agencias |
| `LeadsController` | Recepción pública de mensajes y lectura privada protegida |

### AutoMarket.Tests

Pruebas unitarias aisladas utilizando xUnit y Moq. El objetivo es validar
reglas de negocio sin depender de la base de datos ni servicios externos.

Pruebas actuales:

- `AnuncioServiceTests`
- `UsuarioServiceTests`
- `PerfilDealerServiceTests`
- `SuscripcionServiceTests`
- `LeadServiceTests`: Valida orquestación y resiliencia si el servidor SMTP falla.
- `LeadsControllerTests`: Valida políticas de acceso HTTP (`AllowAnonymous` vs `Authorize`).

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

### Motor de Ventas y Captación de Leads (RF-005)

Permite a compradores anónimos contactar vendedores de vehículos.

- Los visitantes no necesitan cuenta para enviar mensajes (`[AllowAnonymous]`).
- Los dealers necesitan estar logueados para ver su bandeja (`[Authorize]`).
- Se dispara un correo HTML en tiempo real al dueño del vehículo usando `SmtpClient`.
- **Resiliencia:** Si el correo falla temporalmente, el Lead se guarda en BD para no perder al cliente.

### Perfil de dealer

Permite consultar públicamente el perfil de un dealer y que el dealer
autenticado actualice su propia información, incluyendo su logo.

Reglas de negocio:

- Solo un usuario dealer puede actualizar su perfil.
- El logo debe cumplir validaciones de extensión y tamaño.
- Los archivos se almacenan mediante la abstracción `IAlmacenadorArchivos`.

### Suscripciones SaaS

Administra los planes de los dealers y sus restricciones de publicación.

- Un dealer no puede tener más de una suscripción activa.
- No se puede cambiar al mismo plan actual.
- Una suscripción cancelada no puede cambiar de plan.
- Un servicio en segundo plano (`SuscripcionMonitorService`) realiza barridos diarios automatizados.

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
- Entidad Lead y relaciones

## Seguridad

- La autenticación se basa en JWT Bearer.
- Los endpoints que modifican recursos requieren un usuario autenticado.
- El identificador del usuario autenticado se obtiene desde los claims del token.
- La capa Application valida que el usuario sea dueño del anuncio o perfil que intenta modificar.
- Las contraseñas deben almacenarse como hash usando BCrypt.
- Las credenciales de PostgreSQL, JWT, Amazon S3 y SMTP deben configurarse por variables de entorno o secretos; nunca deben subirse al repositorio.

## Integraciones externas

| Integración | Uso |
|---|---|
| PostgreSQL | Persistencia relacional general |
| Amazon S3 | Almacenamiento de imágenes de anuncios y logos |
| Servidor SMTP | Envío de correos electrónicos transaccionales (ej. Gmail) |
| JWT | Autenticación stateless |
| Scalar / OpenAPI | Exploración y documentación de endpoints |

## Decisiones arquitectónicas

### ADR-001: Semi-Clean Architecture

Mantiene límites claros sin abstracciones innecesarias que resten velocidad de desarrollo.

### ADR-002: Repositorios como contratos

Aislan a EF Core de la lógica de aplicación. Los repositorios se definen como interfaces en Core y se implementan en Infrastructure.

### ADR-003: Servicios Externos Desacoplados

`IAlmacenadorArchivos` e `IEmailSenderService` permiten cambiar a AWS SES, SendGrid o Cloudinary en el futuro tocando solo un archivo en Infrastructure.

### ADR-004: Background Services

`SuscripcionMonitorService` se ejecuta como proceso automático sin depender de Cron Jobs externos ni solicitudes HTTP.

## 🚀 Roadmap hacia Producción

### Fase 1: Seguridad y Preparación Frontend

- [ ] **Configurar CORS:** Habilitar políticas de orígenes cruzados en `Program.cs` para el consumo desde el cliente web (React).
- [ ] **Limpieza de Secretos:** Migrar la conexión PostgreSQL, secretos JWT y contraseñas SMTP a User Secrets / Variables de Entorno.
- [ ] **Rate Limiting:** Implementar limitador de peticiones en los endpoints públicos (`LeadsController`) para evitar ataques de Spam.

### Fase 2: Módulo Backoffice (Admin Supremo)

- [ ] **Data Seeder:** Script de inicialización para crear el usuario Administrador primario.
- [ ] **Admin Controller:** Endpoints protegidos para la moderación forzada de anuncios y suspensión de perfiles.

### Fase 3: Retención UX y Analíticas

- [ ] **Favoritos:** Entidad y relación N:M para guardar vehículos.
- [ ] **Comparador:** Endpoint optimizado para cruzar especificaciones técnicas de múltiples vehículos.
- [ ] **Paginación Global:** Refactorizar listados para implementar el modelo `PagedResult`.

### Fase 4: Monetización

- [ ] **Pasarela de Pagos:** Integración con proveedor (Stripe / Local) para el cobro real de suscripciones.
- [ ] **Webhooks:** Recepción de eventos del banco para detonar la activación del plan.