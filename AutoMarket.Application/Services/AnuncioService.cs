using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AutoMarket.Application.DTOs;
using AutoMarket.Core.Entities;
using AutoMarket.Core.Interfaces;

namespace AutoMarket.Application.Services;

public class AnuncioService
{
    private readonly IAnuncioRepository _repository;
    private readonly IAlmacenadorArchivos _almacenadorArchivos;

    // Inyección de dependencias: El servicio exige el contrato para poder funcionar
    public AnuncioService(IAnuncioRepository repository, IAlmacenadorArchivos almacenadorArchivos)
    {
        _repository = repository;
        _almacenadorArchivos = almacenadorArchivos;
    }

    public async Task CrearAnuncioAsync(AnuncioCreateDto dto)
    {
        var nuevoAnuncio = new Anuncio(
            marca: dto.Marca,
            modelo: dto.Modelo,
            tipoVehiculo: dto.TipoVehiculo,
            colorExterior: dto.ColorExterior,
            colorInterior: dto.ColorInterior,
            anio: dto.Anio,
            precio: dto.Precio,
            kilometraje: dto.Kilometraje,
            transmision: dto.Transmision,
            combustible: dto.Combustible,
            accesorios: dto.Accesorios,
            ubicacion: dto.Ubicacion,
            descripcion: dto.Descripcion
        );

        await _repository.AgregarAsync(nuevoAnuncio);
        await _repository.GuardarCambiosAsync();
    }

    public async Task<AnuncioDto?> ObtenerAnuncioPorIdAsync(int id)
    {
        var anuncio = await _repository.ObtenerPorIdAsync(id);

        if (anuncio == null) return null;

        return new AnuncioDto
        {
            Id = anuncio.Id,
            NombreAnuncio = anuncio.NombreAnuncio,
            Marca = anuncio.Marca,
            Modelo = anuncio.Modelo,
            TipoVehiculo = anuncio.TipoVehiculo,
            ColorExterior = anuncio.ColorExterior,
            ColorInterior = anuncio.ColorInterior,
            Anio = anuncio.Anio,
            Precio = anuncio.Precio,
            Kilometraje = anuncio.Kilometraje,
            Transmision = anuncio.Transmision,
            Combustible = anuncio.Combustible,
            Accesorios = anuncio.Accesorios,
            Ubicacion = anuncio.Ubicacion,
            Descripcion = anuncio.Descripcion,
            Estado = anuncio.Estado,
            Fotos = anuncio.Fotos.ToList()
        };
    }

    public async Task<IReadOnlyCollection<AnuncioListadoDto>> ObtenerTodosLosAnuncios()
    {
        IEnumerable<Anuncio> entidades = await _repository.ObtenerTodosLosAnuncios();

        return entidades.Select(e => new AnuncioListadoDto
        {
            Id = e.Id,
            NombreAnuncio = e.NombreAnuncio,
            Precio = e.Precio,
            Ubicacion = e.Ubicacion,
            Estado = e.Estado,
            // Enviamos solo la primera foto para la miniatura de la tarjeta
            Fotos = e.Fotos.Take(1).ToList(),
        }).ToList();

    }

    public async Task<AnuncioUpdateDto?> ActualizarAsync(AnuncioUpdateDto updateAnuncio)
    {
        var anuncio = await _repository.ObtenerPorIdAsync(updateAnuncio.Id);

        if (anuncio == null) return null;

        anuncio.ActualizarInfo(
            updateAnuncio.Marca,
            updateAnuncio.Modelo,
            updateAnuncio.TipoVehiculo,
            updateAnuncio.ColorExterior,
            updateAnuncio.ColorInterior,
            updateAnuncio.Anio,
            updateAnuncio.Precio,
            updateAnuncio.Kilometraje,
            updateAnuncio.Transmision,
            updateAnuncio.Combustible,
            updateAnuncio.Accesorios,
            updateAnuncio.Ubicacion,
            updateAnuncio.Descripcion,
            updateAnuncio.PublicarAlGuardar
        );

        await _repository.ActualizarAsync(anuncio);

        return updateAnuncio;
    }

    public async Task<bool> PublicarAnuncioAsync(int id)
    {
        var anuncio = await _repository.ObtenerPorIdAsync(id);

        if (anuncio == null) return false;

        anuncio.MarcarComoPublicado();

        await _repository.ActualizarAsync(anuncio);
        return true;
    }

    public async Task SubirImagenesAsync(AnuncioImagenUploadDto dto)
    {
        var _anuncio = await _repository.ObtenerPorIdAsync(dto.AnuncioId);
        if (_anuncio == null) throw new KeyNotFoundException("El anuncio no existe");

        var rutasGuardadas = new List<string>();

        foreach (var imagen in dto.Imagenes)
        {
            if (imagen.Length > 5 * 1024 * 1024)
                throw new ArgumentException("Imagen excede el tamaño máximo");

            if (imagen.ContentType != "image/png" && imagen.ContentType != "image/jpeg")
                throw new ArgumentException("Formato no permitido");

            var extension = Path.GetExtension(imagen.FileName);
            var nombreUnico = $"{Guid.NewGuid()}{extension}";

            // 🚨 LA MAGIA: Abrimos el flujo de bytes del archivo y se lo mandamos a S3
            using (var stream = imagen.OpenReadStream())
            {
                var urlPublicaAws = await _almacenadorArchivos.GuardarArchivoAsync(stream, nombreUnico, imagen.ContentType);
                rutasGuardadas.Add(urlPublicaAws); // Guarda la URL completa: https://...
            }
        }

        _anuncio.AgregarFotos(rutasGuardadas);

        await _repository.ActualizarAsync(_anuncio);
    }
}