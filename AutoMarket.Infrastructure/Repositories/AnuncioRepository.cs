using AutoMarket.Core.Entities;
using AutoMarket.Core.Interfaces;
using AutoMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.Infrastructure.Repositories;

public class AnuncioRepository : IAnuncioRepository
{
    private readonly ApplicationDbContext _context;

    public AnuncioRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AgregarAsync(Anuncio anuncio)
    {
        await _context.Anuncios.AddAsync(anuncio);
    }

    public async Task<Anuncio?> ObtenerPorIdAsync(int Id)
    {
        var vehiculoEncontrado = await _context.Anuncios.FindAsync(Id);
        return vehiculoEncontrado;
    }

    public async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<Anuncio>> ObtenerTodosLosAnuncios()
    {
      return await _context.Anuncios.ToListAsync();
    }
}