using System.Diagnostics.CodeAnalysis;
using AutoMarket.Application.DTOs;
using AutoMarket.Core.Entities;
using AutoMarket.Core.Interfaces;

namespace AutoMarket.Application.Services;

public class AnuncioService
{
    private readonly IAnuncioRepository _repository;

    // Inyección de dependencias: El servicio exige el contrato para poder funcionar
    public AnuncioService(IAnuncioRepository repository)
    {
        _repository = repository;
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
            Estado = anuncio.Estado.ToString(),
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
            Estado = e.Estado.ToString(),
            // Enviamos solo la primera foto para la miniatura de la tarjeta
            Fotos = e.Fotos.Take(1).ToList(),
        }).ToList();

    }
}