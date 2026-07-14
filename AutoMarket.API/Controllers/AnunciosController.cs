using Microsoft.AspNetCore.Mvc;
using AutoMarket.Application.DTOs;
using AutoMarket.Application.Services;

namespace AutoMarket.API.Controllers;

[ApiController]
[Route("api/[controller]")] // Esto hace que la URL sea automáticamente "/api/anuncios"
public class AnunciosController : ControllerBase
{
    private readonly AnuncioService _anuncioService;

    // Inyectamos el servicio para que el controlador pueda darle órdenes
    public AnunciosController(AnuncioService anuncioService)
    {
        _anuncioService = anuncioService;
    }

    [HttpPost]
    public async Task<IActionResult> CrearAnuncio([FromBody] AnuncioCreateDto dto)
    {
        await _anuncioService.CrearAnuncioAsync(dto);
        return Ok("Anuncio creado correctamente.");
    }
}