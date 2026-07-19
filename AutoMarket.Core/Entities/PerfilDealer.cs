namespace AutoMarket.Core.Entities;

public class PerfilDealer
{
    public int UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public string NombreAgencia { get; private set; } = null!;
    public string AgenciaRNC { get; private set; } = null!;
    public string? LogoUrl { get; private set; }
    public string? Horarios { get; private set; }
    public string Ubicacion { get; private set; } = null!;
    public string TelefonoAgencia { get; private set; } = null!;

    // ==========================================
    // 1. CONSTRUCTOR PARA EF CORE
    // ==========================================
    private PerfilDealer() { }

    // ==========================================
    // 2. CONSTRUCTOR DE DOMINIO MODIFICADO
    // ==========================================
    public PerfilDealer(
    int usuarioId,
    string nombreAgencia,
    string agenciaRNC,
    string ubicacion,
    string telefonoAgencia,
    string? logoUrl = null,
    string? horarios = null)
    {

        if (usuarioId <= 0)
            throw new ArgumentException("El UsuarioId debe ser válido.", nameof(usuarioId));

        if (string.IsNullOrWhiteSpace(nombreAgencia))
            throw new ArgumentException("El nombre de la agencia es obligatorio.", nameof(nombreAgencia));

        if (string.IsNullOrWhiteSpace(agenciaRNC))
            throw new ArgumentException("El RNC de la agencia es obligatorio.", nameof(agenciaRNC));

        if (string.IsNullOrWhiteSpace(ubicacion))
            throw new ArgumentException("La ubicación es obligatoria.", nameof(ubicacion));

        if (string.IsNullOrWhiteSpace(telefonoAgencia))
            throw new ArgumentException("El teléfono es obligatorio.", nameof(telefonoAgencia));

        UsuarioId = usuarioId;
        NombreAgencia = nombreAgencia;
        AgenciaRNC = agenciaRNC;
        Ubicacion = ubicacion;
        TelefonoAgencia = telefonoAgencia;
        LogoUrl = logoUrl;
        Horarios = horarios;
    }
}