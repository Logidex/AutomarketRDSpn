using AutoMarket.Core.Entities;
namespace AutoMarket.Core.Interfaces;

public interface IAnuncioRepository
{
    Task AgregarAsync(Anuncio anuncio);
    Task<Anuncio?> ObtenerPorIdAsync(int id);
    Task GuardarCambiosAsync();
    Task ActualizarAsync(Anuncio anuncio);
    Task<IReadOnlyCollection<Anuncio>> ObtenerTodosLosAnuncios();
}