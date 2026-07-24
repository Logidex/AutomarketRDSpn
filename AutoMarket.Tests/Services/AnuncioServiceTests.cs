using Moq;
using Xunit;
using AutoMarket.Application.Services;
using AutoMarket.Core.Interfaces;
using AutoMarket.Core.Entities;
using AutoMarket.Application.DTOs;
using Microsoft.AspNetCore.Http;
using AutoMarket.Core.Exceptions;

namespace AutoMarket.Tests.Services;

public class AnuncioServiceTests
{
    // Mocks centralizados para evitar repetir código en cada prueba
    private readonly Mock<IAnuncioRepository> _mockRepo;
    private readonly Mock<IAlmacenadorArchivos> _mockArchivos;
    private readonly Mock<IUsuarioRepository> _mockUsuarioRepo; // 🌟 Nueva dependencia agregada
    private readonly AnuncioService _servicio;

    // El constructor corre automáticamente ANTES de cada prueba individual
    public AnuncioServiceTests()
    {
        _mockRepo = new Mock<IAnuncioRepository>();
        _mockArchivos = new Mock<IAlmacenadorArchivos>();
        _mockUsuarioRepo = new Mock<IUsuarioRepository>(); // Inicializamos el nuevo Mock

        // Instanciamos el servicio UNA SOLA VEZ pasándole los 3 parámetros requeridos
        _servicio = new AnuncioService(_mockRepo.Object, _mockArchivos.Object, _mockUsuarioRepo.Object);
    }

    // =========================================================================
    // PRUEBA 8: Obtener Todos los Anuncios (Éxito)
    // =========================================================================
    [Fact]
    public async Task ObtenerTodosLosAnuncios_DebeRetornarListaDeDtos()
    {
        // 1. ARRANGE
        var listaSimulada = new List<Anuncio>
        {
            new Anuncio(1, "Toyota", "Corolla", "Sedan", "Blanco", "Negro", 2015, 600000, 80000, "Automática", "Gasolina", new List<string> { "Ninguno" }, "Santo Domingo", "Excelente estado")
        };

        // Configuramos el mock centralizado
        _mockRepo.Setup(r => r.ObtenerTodosLosAnuncios()).ReturnsAsync(listaSimulada);

        // 2. ACT (Usamos el _servicio centralizado)
        var resultado = await _servicio.ObtenerTodosLosAnuncios();

        // 3. ASSERT
        Assert.NotNull(resultado);
        Assert.Single(resultado);
        Assert.Equal(600000, resultado.First().Precio);
    }

    // =========================================================================
    // PRUEBA 9: Obtener Por Id - Fallo por no encontrado
    // =========================================================================
    [Fact]
    public async Task ObtenerAnuncioPorIdAsync_NoEncontrado_DebeRetornarNull()
    {
        // 1. ARRANGE
        var idFalso = 999;
        _mockRepo.Setup(r => r.ObtenerPorIdAsync(idFalso)).ReturnsAsync((Anuncio?)null);

        // 2. ACT
        var resultado = await _servicio.ObtenerAnuncioPorIdAsync(idFalso);

        // 3. ASSERT
        Assert.Null(resultado);
    }

    // =========================================================================
    // PRUEBA 10: Obtener Por Id - Éxito al mapear el DTO
    // =========================================================================
    [Fact]
    public async Task ObtenerAnuncioPorIdAsync_Encontrado_DebeRetornarDto()
    {
        // 1. ARRANGE
        var idReal = 5;
        var anuncioEnBD = new Anuncio(1, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string> { "Sunroof" }, "Santiago", "Casi nuevo");

        _mockRepo.Setup(r => r.ObtenerPorIdAsync(idReal)).ReturnsAsync(anuncioEnBD);

        // 2. ACT
        var resultado = await _servicio.ObtenerAnuncioPorIdAsync(idReal);

        // 3. ASSERT
        Assert.NotNull(resultado);
        Assert.Equal("Honda", resultado.Marca);
        Assert.Equal("Civic", resultado.Modelo);
        Assert.Equal(2022, resultado.Anio);
    }

    // =========================================================================
    // PRUEBA 11: Crear Anuncio - Éxito al instanciar y guardar
    // =========================================================================
    [Fact]
    public async Task CrearAnuncioAsync_DatosValidos_DebeGuardarEnRepositorio()
    {
        // 1. ARRANGE
        var dto = new AnuncioCreateDto
        {
            UsuarioId = 10,
            Marca = "Toyota",
            Modelo = "Corolla",
            TipoVehiculo = "Sedan",
            ColorExterior = "Blanco",
            ColorInterior = "Negro",
            Anio = 2020,
            Precio = 800000,
            Kilometraje = 45000,
            Transmision = "Automática",
            Combustible = "Gasolina",
            Accesorios = new List<string> { "Aros de magnesio", "Radio Android" },
            Ubicacion = "Santo Domingo",
            Descripcion = "Vehículo en excelentes condiciones"
        };

        var usuarioValido = CrearUsuarioSimulado(10, esDealer: false);
        _mockUsuarioRepo.Setup(r => r.ObtenerDealerConPerfilPorIdAsync(10)).ReturnsAsync(usuarioValido);

        // 2. ACT
        await _servicio.CrearAnuncioAsync(dto);

        // 3. ASSERT
        _mockRepo.Verify(r => r.AgregarAsync(It.Is<Anuncio>(a =>
            a.UsuarioId == dto.UsuarioId &&
            a.Marca == dto.Marca &&
            a.Modelo == dto.Modelo &&
            a.Precio == dto.Precio
        )), Times.Once);

        _mockRepo.Verify(r => r.GuardarCambiosAsync(), Times.Once);
    }

    // =========================================================================
    // PRUEBA 12: Actualizar - Fallo por no encontrado
    // =========================================================================
    [Fact]
    public async Task ActualizarAsync_AnuncioNoExiste_DebeRetornarNull()
    {
        // 1. ARRANGE
        _mockRepo.Setup(r => r.ObtenerPorIdAsync(999)).ReturnsAsync((Anuncio?)null);

        // 2. ACT
        var resultado = await _servicio.ActualizarAsync(999, 1, new AnuncioUpdateDto());

        // 3. ASSERT
        Assert.Null(resultado);
    }

    // =========================================================================
    // PRUEBA 13: Actualizar - Fallo por seguridad (Intento de Hackeo 🕵️)
    // =========================================================================
    [Fact]
    public async Task ActualizarAsync_NoEsElDueno_DebeLanzarExcepcion()
    {
        // 1. ARRANGE
        var idAnuncio = 5;
        var idDueñoReal = 1;
        var idHacker = 99;

        var anuncioEnBD = new Anuncio(idDueñoReal, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");

        _mockRepo.Setup(r => r.ObtenerPorIdAsync(idAnuncio)).ReturnsAsync(anuncioEnBD);

        // 2 & 3. ACT & ASSERT
        var excepcion = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _servicio.ActualizarAsync(idAnuncio, idHacker, new AnuncioUpdateDto())
        );

        Assert.Equal("Acceso denegado: No tienes permiso para modificar un anuncio que no te pertenece.", excepcion.Message);
        _mockRepo.Verify(r => r.ActualizarAsync(It.IsAny<Anuncio>()), Times.Never);
    }

    // =========================================================================
    // PRUEBA 14: Actualizar - Éxito al modificar (Es el dueño real)
    // =========================================================================
    [Fact]
    public async Task ActualizarAsync_EsElDueno_DebeActualizarYRetornarDto()
    {
        // 1. ARRANGE
        var idAnuncio = 5;
        var idDueño = 1;

        var anuncioEnBD = new Anuncio(idDueño, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");

        var updateDto = new AnuncioUpdateDto
        {
            Marca = "Honda",
            Modelo = "Civic",
            TipoVehiculo = "Sedan",
            ColorExterior = "Azul", // 👈 Cambió a Azul
            ColorInterior = "Gris",
            Anio = 2022,
            Precio = 1100000, // 👈 Le bajó el precio
            Kilometraje = 16000,
            Transmision = "Automática",
            Combustible = "Gasolina",
            Accesorios = new List<string>(),
            Ubicacion = "Santiago",
            Descripcion = "Actualizado",
            PublicarAlGuardar = false
        };

        _mockRepo.Setup(r => r.ObtenerPorIdAsync(idAnuncio)).ReturnsAsync(anuncioEnBD);

        // 2. ACT
        var resultado = await _servicio.ActualizarAsync(idAnuncio, idDueño, updateDto);

        // 3. ASSERT
        Assert.NotNull(resultado);
        _mockRepo.Verify(r => r.ActualizarAsync(It.Is<Anuncio>(a => a.ColorExterior == "Azul" && a.Precio == 1100000)), Times.Once);
    }

    // =========================================================================
    // PRUEBA 15: Publicar - Fallo por no encontrado
    // =========================================================================
    [Fact]
    public async Task PublicarAnuncioAsync_AnuncioNoExiste_DebeRetornarFalso()
    {
        // 1. ARRANGE
        _mockRepo.Setup(r => r.ObtenerPorIdAsync(999)).ReturnsAsync((Anuncio?)null);

        // 2. ACT
        var resultado = await _servicio.PublicarAnuncioAsync(999, 1);

        // 3. ASSERT
        Assert.False(resultado);
    }

    // =========================================================================
    // PRUEBA 16: Publicar - Fallo por seguridad (No es el dueño)
    // =========================================================================
    [Fact]
    public async Task PublicarAnuncioAsync_NoEsElDueno_DebeLanzarExcepcion()
    {
        // 1. ARRANGE
        var idAnuncio = 5;
        var idDueñoReal = 1;
        var idHacker = 99;

        var anuncioEnBD = new Anuncio(idDueñoReal, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");

        _mockRepo.Setup(r => r.ObtenerPorIdAsync(idAnuncio)).ReturnsAsync(anuncioEnBD);

        // 2 & 3. ACT & ASSERT
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _servicio.PublicarAnuncioAsync(idAnuncio, idHacker)
        );

        _mockRepo.Verify(r => r.ActualizarAsync(It.IsAny<Anuncio>()), Times.Never);
    }

    // =========================================================================
    // PRUEBA 17: Publicar - Éxito al cambiar estado a publicado
    // =========================================================================
    [Fact]
    public async Task PublicarAnuncioAsync_EsElDueno_DebePublicarYRetornarTrue()
    {
        // 1. ARRANGE
        var idAnuncio = 5;
        var idDueño = 1;

        var anuncioEnBD = new Anuncio(idDueño, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");

        // Regla de negocio: agregamos 5 fotos
        anuncioEnBD.AgregarFotos(new List<string>
        {
            "url1.jpg", "url2.jpg", "url3.jpg", "url4.jpg", "url5.jpg"
        });

        _mockRepo.Setup(r => r.ObtenerPorIdAsync(idAnuncio)).ReturnsAsync(anuncioEnBD);

        // 2. ACT
        var resultado = await _servicio.PublicarAnuncioAsync(idAnuncio, idDueño);

        // 3. ASSERT
        Assert.True(resultado);
        _mockRepo.Verify(r => r.ActualizarAsync(It.Is<Anuncio>(a => a.Estado == "Publicado")), Times.Once);
    }

    // =========================================================================
    // PRUEBA 18: Subir Imágenes - Fallo por no encontrado
    // =========================================================================
    [Fact]
    public async Task SubirImagenesAsync_AnuncioNoExiste_DebeLanzarKeyNotFoundException()
    {
        // 1. ARRANGE
        var dto = new AnuncioImagenUploadDto { AnuncioId = 999, UsuarioId = 1, Imagenes = new List<IFormFile>() };

        _mockRepo.Setup(r => r.ObtenerPorIdAsync(dto.AnuncioId)).ReturnsAsync((Anuncio?)null);

        // 2 & 3. ACT & ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _servicio.SubirImagenesAsync(dto));
    }

    // =========================================================================
    // PRUEBA 19: Subir Imágenes - Fallo por seguridad (No es el dueño)
    // =========================================================================
    [Fact]
    public async Task SubirImagenesAsync_NoEsElDueno_DebeLanzarUnauthorizedAccessException()
    {
        // 1. ARRANGE
        var idDueñoReal = 1;
        var dto = new AnuncioImagenUploadDto { AnuncioId = 5, UsuarioId = 99, Imagenes = new List<IFormFile>() };

        var anuncioEnBD = new Anuncio(idDueñoReal, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");

        _mockRepo.Setup(r => r.ObtenerPorIdAsync(dto.AnuncioId)).ReturnsAsync(anuncioEnBD);

        // 2 & 3. ACT & ASSERT
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _servicio.SubirImagenesAsync(dto));
    }

    // =========================================================================
    // PRUEBA 20: Subir Imágenes - Fallo por tamaño (> 5MB)
    // =========================================================================
    [Fact]
    public async Task SubirImagenesAsync_ImagenMuyGrande_DebeLanzarArgumentException()
    {
        // 1. ARRANGE
        var idDueño = 1;
        var anuncioEnBD = new Anuncio(idDueño, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");
        _mockRepo.Setup(r => r.ObtenerPorIdAsync(5)).ReturnsAsync(anuncioEnBD);

        // Simulamos un archivo de 6 Megabytes
        var mockArchivoPesado = new Mock<IFormFile>();
        mockArchivoPesado.Setup(f => f.Length).Returns(6 * 1024 * 1024);

        var dto = new AnuncioImagenUploadDto { AnuncioId = 5, UsuarioId = idDueño, Imagenes = new List<IFormFile> { mockArchivoPesado.Object } };

        // 2 & 3. ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.SubirImagenesAsync(dto));
    }

    // =========================================================================
    // PRUEBA 21: Crear Anuncio - Éxito para particular con 0 anuncios
    // =========================================================================
    [Fact]
    public async Task CrearAnuncioAsync_UsuarioParticularConCeroAnuncios_DebeCrearAnuncio()
    {
        // Arrange
        // 🌟 FIX: Agregamos Anio = 2022 para pasar la validación
        var dto = new AnuncioCreateDto { UsuarioId = 1, Marca = "Toyota", Modelo = "Civic", Precio = 500000, Anio = 2022 };
        
        var usuarioParticular = CrearUsuarioSimulado(id: 1, esDealer: false); 

        _mockUsuarioRepo.Setup(repo => repo.ObtenerDealerConPerfilPorIdAsync(1))
            .ReturnsAsync(usuarioParticular);
            
        _mockRepo.Setup(repo => repo.ContarAnunciosPorUsuarioAsync(1))
            .ReturnsAsync(0); 

        // Act
        await _servicio.CrearAnuncioAsync(dto);

        // Assert
        _mockRepo.Verify(repo => repo.AgregarAsync(It.IsAny<Anuncio>()), Times.Once);
    }

    // =========================================================================
    // PRUEBA 22: Crear Anuncio - Fallo por límite alcanzado (Particular con 1 anuncio)
    // =========================================================================
    [Fact]
    public async Task CrearAnuncioAsync_UsuarioParticularConUnAnuncio_DebeLanzarBusinessRuleException()
    {
        // Arrange
        var dto = new AnuncioCreateDto { UsuarioId = 1, Marca = "Honda", Modelo = "Accord" };

        var usuarioParticular = CrearUsuarioSimulado(id: 1, esDealer: false);

        _mockUsuarioRepo.Setup(repo => repo.ObtenerDealerConPerfilPorIdAsync(1))
            .ReturnsAsync(usuarioParticular);

        _mockRepo.Setup(repo => repo.ContarAnunciosPorUsuarioAsync(1))
            .ReturnsAsync(1); // Ya tiene el límite consumido

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _servicio.CrearAnuncioAsync(dto));

        Assert.Equal("Has alcanzado el límite de 1 anuncio gratuito. Mejora tu cuenta a Dealer para publicar más inventario.", excepcion.Message);
        _mockRepo.Verify(repo => repo.AgregarAsync(It.IsAny<Anuncio>()), Times.Never);
    }

    // =========================================================================
    // PRUEBA 23: Crear Anuncio - Éxito para Dealer (Bypass del límite)
    // =========================================================================
    [Fact]
    public async Task CrearAnuncioAsync_UsuarioDealerConUnAnuncio_DebeCrearAnuncio()
    {
        // Arrange
        // 🌟 FIX: Agregamos Anio = 2023 para pasar la validación
        var dto = new AnuncioCreateDto { UsuarioId = 2, Marca = "Ford", Modelo = "CRV", Precio = 900000, Anio = 2023 };
        
        var usuarioDealer = CrearUsuarioSimulado(id: 2, esDealer: true);

        _mockUsuarioRepo.Setup(repo => repo.ObtenerDealerConPerfilPorIdAsync(2))
            .ReturnsAsync(usuarioDealer);
            
        // Act
        await _servicio.CrearAnuncioAsync(dto);

        // Assert
        _mockRepo.Verify(repo => repo.ContarAnunciosPorUsuarioAsync(It.IsAny<int>()), Times.Never);
        _mockRepo.Verify(repo => repo.AgregarAsync(It.IsAny<Anuncio>()), Times.Once);
    }

    // =========================================================================
    // HELPER: Crear Entidades Encapsuladas para Tests
    // =========================================================================
    private Usuario CrearUsuarioSimulado(int id, bool esDealer)
    {
        // 1. Bypasseamos el constructor privado (usado por EF Core) usando Reflection
        var usuario = (Usuario)Activator.CreateInstance(typeof(Usuario), nonPublic: true)!;

        // 2. Seteamos el ID (Buscando si tu propiedad se llama 'Id' o 'UsuarioId')
        var propId = typeof(Usuario).GetProperty("Id") ?? typeof(Usuario).GetProperty("UsuarioId");
        propId?.SetValue(usuario, id);

        // 3. Si es dealer, instanciamos su Perfil y lo inyectamos a la fuerza
        if (esDealer)
        {
            var perfil = (PerfilDealer)Activator.CreateInstance(typeof(PerfilDealer), nonPublic: true)!;
            typeof(Usuario).GetProperty("PerfilDealer")?.SetValue(usuario, perfil);
        }

        return usuario;
    }
}