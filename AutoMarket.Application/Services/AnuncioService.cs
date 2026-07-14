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
}