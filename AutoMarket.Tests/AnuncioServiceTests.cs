using Moq;
using Xunit;
using AutoMarket.Application.Services;
using AutoMarket.Core.Interfaces;
using AutoMarket.Core.Entities;
using AutoMarket.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace AutoMarket.Tests.Services;

public class AnuncioServiceTests
{
    // =========================================================================
    // PRUEBA 8: Obtener Todos los Anuncios (Éxito)
    // =========================================================================
    [Fact]
    public async Task ObtenerTodosLosAnuncios_DebeRetornarListaDeDtos()
    {
        // 1. ARRANGE
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>(); // No se usa aquí, pero el constructor lo pide

        // Creamos una lista simulada con 1 anuncio usando tu constructor blindado
        var listaSimulada = new List<Anuncio>{ new Anuncio(1, "Toyota", "Corolla", "Sedan", "Blanco", "Negro", 2015, 600000, 80000, "Automática", "Gasolina", new List<string> { "Ninguno" }, "Santo Domingo", "Excelente estado")};

        // Le enseñamos al repo a devolver nuestra lista
        mockRepo.Setup(r => r.ObtenerTodosLosAnuncios()).ReturnsAsync(listaSimulada);

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2. ACT
        var resultado = await servicio.ObtenerTodosLosAnuncios();

        // 3. ASSERT
        Assert.NotNull(resultado);
        Assert.Single(resultado); // Verifica que haya exactamente 1 elemento en la lista
        Assert.Equal(600000, resultado.First().Precio); // Verifica que el mapeo del DTO conservó los datos
    }

    // =========================================================================
    // PRUEBA 9: Obtener Por Id - Fallo por no encontrado
    // =========================================================================
    [Fact]
    public async Task ObtenerAnuncioPorIdAsync_NoEncontrado_DebeRetornarNull()
    {
        // 1. ARRANGE
        var idFalso = 999;
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();

        // Simulamos que la base de datos buscó el ID 999 y no encontró nada
        mockRepo.Setup(r => r.ObtenerPorIdAsync(idFalso)).ReturnsAsync((Anuncio?)null);

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2. ACT
        var resultado = await servicio.ObtenerAnuncioPorIdAsync(idFalso);

        // 3. ASSERT
        Assert.Null(resultado); // Si no existe, el servicio debe devolver nulo pacíficamente
    }

    // =========================================================================
    // PRUEBA 10: Obtener Por Id - Éxito al mapear el DTO
    // =========================================================================
    [Fact]
    public async Task ObtenerAnuncioPorIdAsync_Encontrado_DebeRetornarDto()
    {
        // 1. ARRANGE
        var idReal = 5;
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();

        // Creamos el anuncio simulado que la "base de datos" encontrará
        var anuncioEnBD = new Anuncio(1, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string> { "Sunroof" }, "Santiago", "Casi nuevo");

        mockRepo.Setup(r => r.ObtenerPorIdAsync(idReal)).ReturnsAsync(anuncioEnBD);

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2. ACT
        var resultado = await servicio.ObtenerAnuncioPorIdAsync(idReal);

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
        // 1. ARRANGE (Preparar el escenario)
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
            Accesorios = new List<string> { "Aros de magnesio", "Radio Android" }, // Lista correcta
            Ubicacion = "Santo Domingo",
            Descripcion = "Vehículo en excelentes condiciones"
        };

        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>(); // No subimos archivos aquí, pero se inyecta

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2. ACT (Ejecutar la acción)
        // Este método no devuelve nada (es Task, no Task<T>), así que solo lo esperamos.
        await servicio.CrearAnuncioAsync(dto);

        // 3. ASSERT (Comprobar el resultado)
        
        // 3.1 Verificamos que el servicio armó la entidad correctamente y la mandó a agregar.
        // Usamos It.Is<Anuncio> para inspeccionar que la entidad tenga los datos del DTO.
        mockRepo.Verify(r => r.AgregarAsync(It.Is<Anuncio>(a => 
            a.UsuarioId == dto.UsuarioId &&
            a.Marca == dto.Marca &&
            a.Modelo == dto.Modelo &&
            a.Precio == dto.Precio
        )), Times.Once);

        // 3.2 🌟 VALIDACIÓN CRÍTICA: Verificamos que no se olvidó hacer el "Commit" a la base de datos.
        mockRepo.Verify(r => r.GuardarCambiosAsync(), Times.Once);
    }

    // =========================================================================
    // PRUEBA 12: Actualizar - Fallo por no encontrado
    // =========================================================================
    [Fact]
    public async Task ActualizarAsync_AnuncioNoExiste_DebeRetornarNull()
    {
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        
        mockRepo.Setup(r => r.ObtenerPorIdAsync(999)).ReturnsAsync((Anuncio?)null);
        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        var resultado = await servicio.ActualizarAsync(999, 1, new AnuncioUpdateDto());

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
        var idHacker = 99; // 🚨 Alguien más intenta editarlo

        var anuncioEnBD = new Anuncio(idDueñoReal, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");
        
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        mockRepo.Setup(r => r.ObtenerPorIdAsync(idAnuncio)).ReturnsAsync(anuncioEnBD);

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2 & 3. ACT & ASSERT (El nuevo truco: Assert.ThrowsAsync)
        // Le decimos a xUnit: "Ejecuta esto y CONFIRMA que lance un UnauthorizedAccessException"
        var excepcion = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            servicio.ActualizarAsync(idAnuncio, idHacker, new AnuncioUpdateDto())
        );

        Assert.Equal("Acceso denegado: No tienes permiso para modificar un anuncio que no te pertenece.", excepcion.Message);
        mockRepo.Verify(r => r.ActualizarAsync(It.IsAny<Anuncio>()), Times.Never); // Confirmamos que NO se guardó
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
            Marca = "Honda", Modelo = "Civic", TipoVehiculo = "Sedan", ColorExterior = "Azul", // 👈 Cambió a Azul
            ColorInterior = "Gris", Anio = 2022, Precio = 1100000, // 👈 Le bajó el precio
            Kilometraje = 16000, Transmision = "Automática", Combustible = "Gasolina", 
            Accesorios = new List<string>(), Ubicacion = "Santiago", Descripcion = "Actualizado", 
            PublicarAlGuardar = false 
        };

        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        mockRepo.Setup(r => r.ObtenerPorIdAsync(idAnuncio)).ReturnsAsync(anuncioEnBD);

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2. ACT
        var resultado = await servicio.ActualizarAsync(idAnuncio, idDueño, updateDto);

        // 3. ASSERT
        Assert.NotNull(resultado);
        mockRepo.Verify(r => r.ActualizarAsync(It.Is<Anuncio>(a => a.ColorExterior == "Azul" && a.Precio == 1100000)), Times.Once);
    }

    // =========================================================================
    // PRUEBA 15: Publicar - Fallo por no encontrado
    // =========================================================================
    [Fact]
    public async Task PublicarAnuncioAsync_AnuncioNoExiste_DebeRetornarFalso()
    {
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        
        mockRepo.Setup(r => r.ObtenerPorIdAsync(999)).ReturnsAsync((Anuncio?)null);
        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        var resultado = await servicio.PublicarAnuncioAsync(999, 1);

        Assert.False(resultado);
    }

    // =========================================================================
    // PRUEBA 16: Publicar - Fallo por seguridad (No es el dueño)
    // =========================================================================
    [Fact]
    public async Task PublicarAnuncioAsync_NoEsElDueno_DebeLanzarExcepcion()
    {
        var idAnuncio = 5;
        var idDueñoReal = 1;
        var idHacker = 99;

        var anuncioEnBD = new Anuncio(idDueñoReal, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");
        
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        mockRepo.Setup(r => r.ObtenerPorIdAsync(idAnuncio)).ReturnsAsync(anuncioEnBD);

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // Exigimos que el servicio explote y lance el 403
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            servicio.PublicarAnuncioAsync(idAnuncio, idHacker)
        );

        mockRepo.Verify(r => r.ActualizarAsync(It.IsAny<Anuncio>()), Times.Never);
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
        
        // 🚨 LA CORRECCIÓN: Cumplimos con la regla de negocio del Core agregando 5 fotos
        anuncioEnBD.AgregarFotos(new List<string> 
        { 
            "url1.jpg", "url2.jpg", "url3.jpg", "url4.jpg", "url5.jpg" 
        });
        
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        mockRepo.Setup(r => r.ObtenerPorIdAsync(idAnuncio)).ReturnsAsync(anuncioEnBD);

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2. ACT
        var resultado = await servicio.PublicarAnuncioAsync(idAnuncio, idDueño);

        // 3. ASSERT
        Assert.True(resultado);
        // Validamos que el método se llamó 1 vez y que el estado interno del anuncio cambió
        mockRepo.Verify(r => r.ActualizarAsync(It.Is<Anuncio>(a => a.Estado == "Publicado")), Times.Once);
    }

    // =========================================================================
    // PRUEBA 18: Subir Imágenes - Fallo por no encontrado
    // =========================================================================
    [Fact]
    public async Task SubirImagenesAsync_AnuncioNoExiste_DebeLanzarKeyNotFoundException()
    {
        var dto = new AnuncioImagenUploadDto { AnuncioId = 999, UsuarioId = 1, Imagenes = new List<IFormFile>() };
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        
        mockRepo.Setup(r => r.ObtenerPorIdAsync(dto.AnuncioId)).ReturnsAsync((Anuncio?)null);
        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => servicio.SubirImagenesAsync(dto));
    }

    // =========================================================================
    // PRUEBA 19: Subir Imágenes - Fallo por seguridad (No es el dueño)
    // =========================================================================
    [Fact]
    public async Task SubirImagenesAsync_NoEsElDueno_DebeLanzarUnauthorizedAccessException()
    {
        var idDueñoReal = 1;
        var dto = new AnuncioImagenUploadDto { AnuncioId = 5, UsuarioId = 99, Imagenes = new List<IFormFile>() }; // Usuario 99 intenta subir fotos
        
        var anuncioEnBD = new Anuncio(idDueñoReal, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");
        
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        mockRepo.Setup(r => r.ObtenerPorIdAsync(dto.AnuncioId)).ReturnsAsync(anuncioEnBD);

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.SubirImagenesAsync(dto));
    }

    // =========================================================================
    // PRUEBA 20: Subir Imágenes - Fallo por tamaño (> 5MB)
    // =========================================================================
    [Fact]
    public async Task SubirImagenesAsync_ImagenMuyGrande_DebeLanzarArgumentException()
    {
        // 1. ARRANGE
        var idDueño = 1;
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        var anuncioEnBD = new Anuncio(idDueño, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");
        mockRepo.Setup(r => r.ObtenerPorIdAsync(5)).ReturnsAsync(anuncioEnBD);

        // Simulamos un archivo gigantesco (6 Megabytes)
        var mockArchivoPesado = new Mock<IFormFile>();
        mockArchivoPesado.Setup(f => f.Length).Returns(6 * 1024 * 1024); 

        var dto = new AnuncioImagenUploadDto { AnuncioId = 5, UsuarioId = idDueño, Imagenes = new List<IFormFile> { mockArchivoPesado.Object } };
        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2 & 3. ACT & ASSERT
        var excepcion = await Assert.ThrowsAsync<ArgumentException>(() => servicio.SubirImagenesAsync(dto));
        Assert.Equal("Imagen excede el tamaño máximo", excepcion.Message);
    }

    // =========================================================================
    // PRUEBA 21: Subir Imágenes - Fallo por formato incorrecto
    // =========================================================================
    [Fact]
    public async Task SubirImagenesAsync_FormatoInvalido_DebeLanzarArgumentException()
    {
        var idDueño = 1;
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        var anuncioEnBD = new Anuncio(idDueño, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");
        mockRepo.Setup(r => r.ObtenerPorIdAsync(5)).ReturnsAsync(anuncioEnBD);

        // Simulamos un archivo con tamaño permitido, pero que es un PDF
        var mockArchivoPdf = new Mock<IFormFile>();
        mockArchivoPdf.Setup(f => f.Length).Returns(2 * 1024 * 1024);
        mockArchivoPdf.Setup(f => f.ContentType).Returns("application/pdf"); // 🚨 Formato no permitido

        var dto = new AnuncioImagenUploadDto { AnuncioId = 5, UsuarioId = idDueño, Imagenes = new List<IFormFile> { mockArchivoPdf.Object } };
        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        var excepcion = await Assert.ThrowsAsync<ArgumentException>(() => servicio.SubirImagenesAsync(dto));
        Assert.Equal("Formato no permitido", excepcion.Message);
    }

    // =========================================================================
    // PRUEBA 22: Subir Imágenes - Éxito total (Victoria de jefe)
    // =========================================================================
    [Fact]
    public async Task SubirImagenesAsync_ImagenValida_DebeSubirYGuardarRuta()
    {
        var idDueño = 1;
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();
        var anuncioEnBD = new Anuncio(idDueño, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2022, 1200000, 15000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo");
        mockRepo.Setup(r => r.ObtenerPorIdAsync(5)).ReturnsAsync(anuncioEnBD);

        // Simulamos un archivo perfecto
        var mockImagenValida = new Mock<IFormFile>();
        mockImagenValida.Setup(f => f.Length).Returns(2 * 1024 * 1024); // 2 MB
        mockImagenValida.Setup(f => f.ContentType).Returns("image/jpeg");
        mockImagenValida.Setup(f => f.FileName).Returns("foto_frontal.jpg");
        
        // Simulamos que el archivo se puede abrir para leer sus bytes
        mockImagenValida.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

        var dto = new AnuncioImagenUploadDto { AnuncioId = 5, UsuarioId = idDueño, Imagenes = new List<IFormFile> { mockImagenValida.Object } };
        
        // Simulamos que el servicio de AWS S3 guarda el archivo y nos devuelve una URL pública
        var urlS3Simulada = "https://automarket-s3.aws.com/foto_frontal_abcd123.jpg";
        mockArchivos.Setup(a => a.GuardarArchivoAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/jpeg"))
                    .ReturnsAsync(urlS3Simulada);

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2. ACT
        await servicio.SubirImagenesAsync(dto);

        // 3. ASSERT
        // Verificamos que se ordenó actualizar el anuncio en la base de datos...
        // ¡Y que la entidad ahora tiene la URL de Amazon S3 en su lista de fotos!
        mockRepo.Verify(r => r.ActualizarAsync(It.Is<Anuncio>(a => a.Fotos.Contains(urlS3Simulada))), Times.Once);
    }

    // =========================================================================
    // PRUEBA 23: Búsqueda sin filtros (Paginación por defecto)
    // =========================================================================
    [Fact]
    public async Task BuscarAnunciosAsync_SinFiltros_DebeRetornarPaginaCorrecta()
    {
        // 1. ARRANGE
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();

        // Un DTO vacío asume los valores por defecto: Página 1, 20 Anuncios
        var dtoBusqueda = new AnuncioSearchDto(); 

        var listaSimulada = new List<Anuncio>
        {
            new Anuncio(1, "Toyota", "Corolla", "Sedan", "Blanco", "Negro", 2015, 600000, 80000, "Automática", "Gasolina", new List<string>(), "Santo Domingo", "Nítido"),
            new Anuncio(2, "Honda", "Civic", "Sedan", "Rojo", "Gris", 2018, 850000, 60000, "Automática", "Gasolina", new List<string>(), "Santiago", "Casi nuevo")
        };
        var totalRegistrosEnBD = 50; // Simulamos que hay 50 carros en total en la base de datos

        // 🚨 EL TRUCO DE LA TUPLA: Retornamos la lista y el total entre paréntesis
        mockRepo.Setup(r => r.BuscarPaginadoAsync(It.IsAny<AnuncioQueryFilter>()))
                .ReturnsAsync((listaSimulada, totalRegistrosEnBD));

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2. ACT
        var resultado = await servicio.BuscarAnunciosAsync(dtoBusqueda);

        // 3. ASSERT
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Items.Count); // Devolvió los 2 carros simulados
        Assert.Equal(50, resultado.TotalRegistros); // Mantuvo el total general
        Assert.Equal(1, resultado.PaginaActual);
        Assert.Equal(20, resultado.CantidadPorPagina);
        
        // Verifica que calculó matemáticamente las páginas (50 / 20 = 2.5 -> Techo de 3)
        Assert.Equal(3, resultado.TotalPaginas); 
    }

    // =========================================================================
    // PRUEBA 24: Búsqueda con filtros (Traducción exacta al Core)
    // =========================================================================
    [Fact]
    public async Task BuscarAnunciosAsync_ConFiltros_DebeMapearFiltrosAlRepositorio()
    {
        // 1. ARRANGE
        var mockRepo = new Mock<IAnuncioRepository>();
        var mockArchivos = new Mock<IAlmacenadorArchivos>();

        // El usuario busca un Toyota de máximo 700,000 pesos en la página 2
        var dtoBusqueda = new AnuncioSearchDto
        {
            Marca = "Toyota",
            PrecioMaximo = 700000,
            PaginaActual = 2,
            CantidadAnuncios = 15
        };

        // Devolvemos una tupla vacía, porque aquí no nos importan los resultados, 
        // nos importa verificar que el FILTRO llegó correctamente al repositorio.
        mockRepo.Setup(r => r.BuscarPaginadoAsync(It.IsAny<AnuncioQueryFilter>()))
                .ReturnsAsync((new List<Anuncio>(), 0));

        var servicio = new AnuncioService(mockRepo.Object, mockArchivos.Object);

        // 2. ACT
        await servicio.BuscarAnunciosAsync(dtoBusqueda);

        // 3. ASSERT
        // Usamos It.Is<AnuncioQueryFilter> para inspeccionar si el traductor hizo su trabajo
        mockRepo.Verify(r => r.BuscarPaginadoAsync(It.Is<AnuncioQueryFilter>(f => 
            f.Marca == "Toyota" &&
            f.PrecioMaximo == 700000 &&
            f.PaginaActual == 2 &&
            f.CantidadPorPagina == 15
        )), Times.Once);
    }
}