namespace AutoMarket.Core.Entities;

public class Usuario
{
    public int UsuarioId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Apellido { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool EmailConfirmado { get; private set; }
    public string Rol { get; private set; } = null!;
    public List<Anuncio> _anuncios = new();
    public IReadOnlyCollection<string> Anuncios => (IReadOnlyCollection<string>)_anuncios.AsReadOnly();
    
}