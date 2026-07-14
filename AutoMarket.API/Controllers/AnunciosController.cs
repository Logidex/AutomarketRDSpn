using Microsoft.AspNetCore.Mvc;
using AutoMarket.Application.DTOs;
using AutoMarket.Application.Services;
using AutoMarket.Core.Entities;

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

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var anuncioDto = await _anuncioService.ObtenerAnuncioPorIdAsync(id);

        if (anuncioDto == null)
        {
            return NotFound(new { mensaje = $"El vehículo con ID {id} no fue encontrado en la base de datos." });
        }

        return Ok(anuncioDto);
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodosLosAnuncios()
    {
        var anuncios = await _anuncioService.ObtenerTodosLosAnuncios();

        return Ok(anuncios);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarAnuncio(int id, [FromBody] AnuncioUpdateDto updateDto)
    {
        var resultado = await _anuncioService.ActualizarAsync(updateDto);
        if (resultado == null) return NotFound($"El vehículo con ID {id} no fue encontrado.");
        return Ok(resultado);

    }

    [HttpPatch("{id}/publicar")]
    public async Task<IActionResult> Publicar(int id)
    {
        var publicado = await _anuncioService.PublicarAnuncioAsync(id);
        if (!publicado) return NotFound($"No se encontró el anuncio {id}.");
       return Ok(publicado);
    }
}