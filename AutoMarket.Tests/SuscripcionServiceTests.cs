using Moq;
using Xunit;
using AutoMarket.Application.Services;
using AutoMarket.Core.Interfaces;
using AutoMarket.Core.Entities;
using AutoMarket.Core.Entities.Enums;
using AutoMarket.Core.Exceptions;

namespace AutoMarket.Tests.Services;

public class SuscripcionServiceTests
{
    private readonly Mock<ISuscripcionRepository> _mockRepo;
    private readonly SuscripcionService _servicio;

    public SuscripcionServiceTests()
    {
        _mockRepo = new Mock<ISuscripcionRepository>();
        _servicio = new SuscripcionService(_mockRepo.Object);
    }

    // =========================================================================
    // HELPER: Crear Entidades Encapsuladas para Tests
    // =========================================================================
    private SuscripcionDealer CrearSuscripcionSimulada(int perfilDealerId, PlanNivel nivel, EstadoSuscripcion estado)
    {
        var suscripcion = new SuscripcionDealer(perfilDealerId, nivel, CicloFacturacion.Mensual);
        
        var propEstado = typeof(SuscripcionDealer).GetProperty("Estado");
        propEstado?.SetValue(suscripcion, estado);

        return suscripcion;
    }

    // =========================================================================
    // PRUEBA 01: Asignar Plan Inicial - Fallo (Ya tiene suscripción)
    // =========================================================================
    [Fact]
    public async Task AsignarPlanInicialAsync_SuscripcionExistente_DebeLanzarBusinessRuleException()
    {
        // Arrange
        int perfilId = 1;
        var suscripcionExistente = CrearSuscripcionSimulada(perfilId, PlanNivel.Basico, EstadoSuscripcion.Activa);
        
        _mockRepo.Setup(r => r.ObtenerPorDealerIdAsync(perfilId))
            .ReturnsAsync(suscripcionExistente);

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _servicio.AsignarPlanInicialAsync(perfilId, PlanNivel.Basico, CicloFacturacion.Mensual));

        Assert.Equal("El dealer ya posee una suscripción registrada.", excepcion.Message);
        _mockRepo.Verify(r => r.AgregarAsync(It.IsAny<SuscripcionDealer>()), Times.Never);
    }

    // =========================================================================
    // PRUEBA 02: Asignar Plan Inicial - Éxito
    // =========================================================================
    [Fact]
    public async Task AsignarPlanInicialAsync_SinSuscripcionPrevia_DebeAgregarSuscripcion()
    {
        // Arrange
        int perfilId = 2;
        _mockRepo.Setup(r => r.ObtenerPorDealerIdAsync(perfilId))
            .ReturnsAsync((SuscripcionDealer?)null);

        // Act
        await _servicio.AsignarPlanInicialAsync(perfilId, PlanNivel.Basico, CicloFacturacion.Mensual);

        // Assert
        _mockRepo.Verify(r => r.AgregarAsync(It.Is<SuscripcionDealer>(s => 
            s.PerfilDealerId == perfilId && s.Nivel == PlanNivel.Basico)), Times.Once);
    }

    // =========================================================================
    // PRUEBA 03: Cambiar Plan - Fallo (Regla 1: El Fantasma)
    // =========================================================================
    [Fact]
    public async Task CambiarPlanAsync_SuscripcionNoExiste_DebeLanzarKeyNotFoundException()
    {
        // Arrange
        int perfilId = 3;
        _mockRepo.Setup(r => r.ObtenerPorDealerIdAsync(perfilId))
            .ReturnsAsync((SuscripcionDealer?)null);

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _servicio.CambiarPlanAsync(perfilId, PlanNivel.Pro));

        Assert.Equal("No se encontró una suscripción activa para este dealer.", excepcion.Message);
    }

    // =========================================================================
    // PRUEBA 04: Cambiar Plan - Fallo (Regla 2: El Cobro Doble)
    // =========================================================================
    [Fact]
    public async Task CambiarPlanAsync_MismoPlan_DebeLanzarBusinessRuleException()
    {
        // Arrange
        int perfilId = 4;
        var suscripcionActual = CrearSuscripcionSimulada(perfilId, PlanNivel.Pro, EstadoSuscripcion.Activa);

        _mockRepo.Setup(r => r.ObtenerPorDealerIdAsync(perfilId))
            .ReturnsAsync(suscripcionActual);

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _servicio.CambiarPlanAsync(perfilId, PlanNivel.Pro)); 

        Assert.Equal("El dealer ya se encuentra suscrito a este plan. No se requiere actualización.", excepcion.Message);
        _mockRepo.Verify(r => r.ActualizarAsync(It.IsAny<SuscripcionDealer>()), Times.Never);
    }

    // =========================================================================
    // PRUEBA 05: Cambiar Plan - Fallo (Regla 3: El Moroso)
    // =========================================================================
    [Fact]
    public async Task CambiarPlanAsync_SuscripcionCancelada_DebeLanzarBusinessRuleException()
    {
        // Arrange
        int perfilId = 5;
        var suscripcionMorosa = CrearSuscripcionSimulada(perfilId, PlanNivel.Basico, EstadoSuscripcion.Cancelada);

        _mockRepo.Setup(r => r.ObtenerPorDealerIdAsync(perfilId))
            .ReturnsAsync(suscripcionMorosa);

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _servicio.CambiarPlanAsync(perfilId, PlanNivel.Elite));

        Assert.Equal("La suscripción está cancelada. Debe adquirir una nueva en lugar de cambiar de plan.", excepcion.Message);
    }

    // =========================================================================
    // PRUEBA 06: Cambiar Plan - Éxito (Mutación Segura)
    // =========================================================================
    [Fact]
    public async Task CambiarPlanAsync_DatosValidos_DebeActualizarNivelYGuardar()
    {
        // Arrange
        int perfilId = 6;
        var suscripcionValida = CrearSuscripcionSimulada(perfilId, PlanNivel.Basico, EstadoSuscripcion.Activa);

        _mockRepo.Setup(r => r.ObtenerPorDealerIdAsync(perfilId))
            .ReturnsAsync(suscripcionValida);

        // Act
        await _servicio.CambiarPlanAsync(perfilId, PlanNivel.Pro);

        // Assert
        Assert.Equal(PlanNivel.Pro, suscripcionValida.Nivel);
        _mockRepo.Verify(r => r.ActualizarAsync(suscripcionValida), Times.Once);
    }
}