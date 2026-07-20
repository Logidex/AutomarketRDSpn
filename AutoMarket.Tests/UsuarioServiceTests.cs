using Moq;
using Xunit;
using AutoMarket.Application.DTOs;
using AutoMarket.Application.Services;
using AutoMarket.Core.Interfaces;
using AutoMarket.Application.Interfaces;
using AutoMarket.Application.DTOs.Usuario;
using AutoMarket.Core.Entities;

namespace AutoMarket.Tests.Services;

public class UsuarioServiceTests
{
    [Fact]
    public async Task RegistrarUsuarioAsync_SiEmailYaExiste_DebeRetornarFalso()
    {
        // 1. ARRANGE (Preparar el escenario)

        var dto = new RegistroDto
        {
            Nombre = "Erick",
            Apellido = "Hipolito",
            Email = "erick@test.com",
            Password = "MiPasswordSeguro123",
            Rol = "Comprador"
        };

        var mockRepo = new Mock<IUsuarioRepository>();

        mockRepo.Setup(r => r.ExisteEmailAsync(dto.Email)).ReturnsAsync(true);

        var mockTokenService = new Mock<ITokenService>();

        var servicio = new AuthService(mockRepo.Object, mockTokenService.Object);

        // 2. ACT (Ejecutar la accion)

        var resultado = await servicio.RegistrarUsuarioAsync(dto);

        // 3. ASSERT (Comprobar el resultado)

        Assert.False(resultado.Exito);
        Assert.Equal("El correo electrónico ya está registrado.", resultado.Mensaje);
    }

    [Fact]
    public async Task RegistrarUsuarioAsync_DatosValidosComprador_DebeRetornarExito()
    {
        // ==========================================
        // 1. ARRANGE (Preparar el escenario)
        // ==========================================
        var dto = new RegistroDto
        {
            Nombre = "Juan",
            Apellido = "Perez",
            Email = "nuevo@test.com",
            Password = "MiPasswordSeguro123",
            Rol = "Comprador" // Ojo aquí, es Comprador, no Dealer
        };

        var mockRepo = new Mock<IUsuarioRepository>();
        var mockTokenService = new Mock<ITokenService>();

        // 🚨 LA CLAVE: Le decimos al repo falso que devuelva FALSE (el email NO existe)
        mockRepo.Setup(r => r.ExisteEmailAsync(dto.Email)).ReturnsAsync(false);

        var servicio = new AuthService(mockRepo.Object, mockTokenService.Object);

        // ==========================================
        // 2. ACT (Ejecutar la acción)
        // ==========================================
        var resultado = await servicio.RegistrarUsuarioAsync(dto);

        // ==========================================
        // 3. ASSERT (Comprobar el resultado)
        // ==========================================
        // 3.1 Verificamos lo que retorna el método
        Assert.True(resultado.Exito);
        Assert.Equal("Usuario registrado exitosamente", resultado.Mensaje);

        // 3.2 🌟 TRUCO PRO DE MOQ: Verificar que se llamó a la base de datos
        mockRepo.Verify(r => r.CrearUsuarioAsync(It.IsAny<Usuario>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarUsuarioAsync_DealerFaltanDatos_DebeRetornarFalso()
    {
        // ==========================================
        // 1. ARRANGE (Preparar el escenario)
        // ==========================================
        var dto = new RegistroDto
        {
            Nombre = "Carlos",
            Apellido = "Santana",
            Email = "dealer_falso@test.com",
            Password = "MiPasswordSeguro123",
            Rol = "Dealer",
            NombreAgencia = "", // 🚨 Dato faltante 1 (Vacío)
            AgenciaRNC = null   // 🚨 Dato faltante 2 (Nulo)
        };

        var mockRepo = new Mock<IUsuarioRepository>();
        var mockTokenService = new Mock<ITokenService>();

        // El correo es válido (no existe previamente)
        mockRepo.Setup(r => r.ExisteEmailAsync(dto.Email)).ReturnsAsync(false);

        var servicio = new AuthService(mockRepo.Object, mockTokenService.Object);

        // ==========================================
        // 2. ACT (Ejecutar la acción)
        // ==========================================
        var resultado = await servicio.RegistrarUsuarioAsync(dto);

        // ==========================================
        // 3. ASSERT (Comprobar el resultado)
        // ==========================================
        // 3.1 Verificamos que el sistema lo haya rechazado correctamente
        Assert.False(resultado.Exito);
        Assert.Equal("Los datos de la agencia y el RNC son obligatorios para cuentas tipo Dealer.", resultado.Mensaje);

        // 3.2 🌟 TRUCO PRO DE MOQ (La Inversa): 
        // Verificamos que el repositorio NUNCA haya intentado guardar un usuario en la BD.
        mockRepo.Verify(r => r.CrearUsuarioAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarUsuarioAsync_DatosValidosDealer_DebeRetornarExito()
    {
        // ==========================================
        // 1. ARRANGE (Preparar el escenario)
        // ==========================================
        var dto = new RegistroDto
        {
            Nombre = "Roberto",
            Apellido = "Gomez",
            Email = "dealer_real@test.com",
            Password = "MiPasswordSeguro123",
            Rol = "Dealer", // 👈 Rol Dealer
            NombreAgencia = "AutoMotors RD", // ✅ Datos completos
            AgenciaRNC = "130-456789-1",
            UbicacionAgencia = "Santo Domingo",
            TelefonoAgencia = "809-555-5555"
        };

        var mockRepo = new Mock<IUsuarioRepository>();
        var mockTokenService = new Mock<ITokenService>();

        // El correo no existe, luz verde para avanzar
        mockRepo.Setup(r => r.ExisteEmailAsync(dto.Email)).ReturnsAsync(false);

        var servicio = new AuthService(mockRepo.Object, mockTokenService.Object);

        // ==========================================
        // 2. ACT (Ejecutar la acción)
        // ==========================================
        var resultado = await servicio.RegistrarUsuarioAsync(dto);

        // ==========================================
        // 3. ASSERT (Comprobar el resultado)
        // ==========================================
        // 3.1 Verificamos que el registro fue exitoso
        Assert.True(resultado.Exito);
        Assert.Equal("Usuario registrado exitosamente", resultado.Mensaje);

        // 3.2 🌟 TRUCO AVANZADO DE MOQ: Inspeccionar el objeto antes de guardar
        // No solo verificamos que se llamó a CrearUsuarioAsync 1 vez, 
        // sino que exigimos que el objeto Usuario que se intentó guardar TENGA un PerfilDealer asignado.
        mockRepo.Verify(r => r.CrearUsuarioAsync(It.Is<Usuario>(u =>
            u.Rol == "Dealer" &&
            u.PerfilDealer != null &&
            u.PerfilDealer.NombreAgencia == "AutoMotors RD"
        )), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_EmailInexistente_DebeRetornarFalso()
    {
        // ==========================================
        // 1. ARRANGE (Preparar el escenario)
        // ==========================================
        var dto = new LoginDto
        {
            Email = "correo_fantasma@test.com",
            Password = "CualquierPassword123"
        };

        var mockRepo = new Mock<IUsuarioRepository>();
        var mockTokenService = new Mock<ITokenService>();

        // 🚨 LA CLAVE: Simulamos que la base de datos busca el correo y no encuentra nada (retorna null)
        // Nota: Usamos (Usuario?)null para ayudar a Moq a entender el tipo de dato que está devolviendo.
        mockRepo.Setup(r => r.ObtenerPorEmailAsync(dto.Email)).ReturnsAsync((Usuario?)null);

        var servicio = new AuthService(mockRepo.Object, mockTokenService.Object);

        // ==========================================
        // 2. ACT (Ejecutar la acción)
        // ==========================================
        var resultado = await servicio.LoginAsync(dto);

        // ==========================================
        // 3. ASSERT (Comprobar el resultado)
        // ==========================================
        // 3.1 Verificamos la tupla de respuesta
        Assert.False(resultado.Exito);
        Assert.Equal("Credenciales incorrectas.", resultado.Mensaje);

        // 3.2 Verificamos que el token sea explícitamente nulo
        Assert.Null(resultado.Token);

        // 3.3 🌟 TRUCO PRO: Nos aseguramos de que el sistema NUNCA intentó generar un token
        // Si el usuario no existe, el flujo debe cortarse de inmediato.
        mockTokenService.Verify(t => t.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_PasswordIncorrecto_DebeRetornarFalso()
    {
        // ==========================================
        // 1. ARRANGE (Preparar el escenario)
        // ==========================================
        // 1.1 El atacante envía un email válido pero una clave incorrecta
        var dto = new LoginDto
        {
            Email = "erick@test.com",
            Password = "ClaveEquivocada" // 🚨 Clave mala
        };

        // 1.2 Creamos un usuario "real" como si viniera de la base de datos.
        // Le asignamos el hash de una contraseña totalmente DIFERENTE.
        var passwordReal = "ClaveVerdadera123";
        var usuarioEnBaseDeDatos = new Usuario(
            nombre: "Erick",
            apellido: "Hipolito",
            email: dto.Email,
            passwordHash: BCrypt.Net.BCrypt.HashPassword(passwordReal), // 🔒 El hash original
            rol: "Comprador",
            telefonoPersonal: null
        );

        var mockRepo = new Mock<IUsuarioRepository>();
        var mockTokenService = new Mock<ITokenService>();

        // 1.3 El repo encuentra el correo y devuelve al usuario
        mockRepo.Setup(r => r.ObtenerPorEmailAsync(dto.Email)).ReturnsAsync(usuarioEnBaseDeDatos);

        var servicio = new AuthService(mockRepo.Object, mockTokenService.Object);

        // ==========================================
        // 2. ACT (Ejecutar la acción)
        // ==========================================
        var resultado = await servicio.LoginAsync(dto);

        // ==========================================
        // 3. ASSERT (Comprobar el resultado)
        // ==========================================
        // El servicio debió usar BCrypt.Verify, darse cuenta del error y rechazarlo
        Assert.False(resultado.Exito);
        Assert.Equal("Credenciales incorrectas.", resultado.Mensaje);
        Assert.Null(resultado.Token);

        // Nuevamente, nos aseguramos de que el atacante no obtuvo un token
        mockTokenService.Verify(t => t.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_CredencialesCorrectas_DebeRetornarExitoYToken()
    {
        // ==========================================
        // 1. ARRANGE (Preparar el escenario)
        // ==========================================
        var passwordCrudo = "ClaveSecreta123";

        // El usuario envía las credenciales correctas
        var dto = new LoginDto
        {
            Email = "erick@test.com",
            Password = passwordCrudo
        };

        // Creamos el usuario simulado con el hash correcto
        var usuarioEnBaseDeDatos = new Usuario(
            nombre: "Erick",
            apellido: "Hipolito",
            email: dto.Email,
            passwordHash: BCrypt.Net.BCrypt.HashPassword(passwordCrudo), // 🔒 Coinciden perfectamente
            rol: "Comprador",
            telefonoPersonal: null
        );

        // Inventamos un token falso para simular la respuesta
        var tokenFalso = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.UnTokenFalsoParaPruebas.FirmaFalsa";

        var mockRepo = new Mock<IUsuarioRepository>();
        var mockTokenService = new Mock<ITokenService>();

        // Instrucción 1: Cuando pregunten por el correo, devuelve el usuario
        mockRepo.Setup(r => r.ObtenerPorEmailAsync(dto.Email)).ReturnsAsync(usuarioEnBaseDeDatos);

        // Instrucción 2: Cuando te pidan generar un token, devuelve nuestra cadena inventada
        mockTokenService.Setup(t => t.GenerarToken(It.IsAny<Usuario>())).Returns(tokenFalso);

        var servicio = new AuthService(mockRepo.Object, mockTokenService.Object);

        // ==========================================
        // 2. ACT (Ejecutar la acción)
        // ==========================================
        var resultado = await servicio.LoginAsync(dto);

        // ==========================================
        // 3. ASSERT (Comprobar el resultado)
        // ==========================================
        Assert.True(resultado.Exito);
        Assert.Equal("Inicio de sesión exitoso.", resultado.Mensaje);

        // Verificamos que el token que devolvió el servicio es exactamente el que generó el TokenService
        Assert.Equal(tokenFalso, resultado.Token);

        // Verificamos matemáticamente que el ITokenService fue invocado exactamente una vez
        mockTokenService.Verify(t => t.GenerarToken(It.IsAny<Usuario>()), Times.Once);
    }
}