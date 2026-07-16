namespace AutoMarket.Core.Entities;

public class Anuncio
{
    public int Id { get; private set; }
    public string NombreAnuncio => $"{Marca} {Modelo} {Anio}";
    public string Marca { get; private set; } = null!;
    public string Modelo { get; private set; } = null!;
    public string TipoVehiculo { get; private set; } = null!;
    public string ColorExterior { get; private set; } = null!;
    public string ColorInterior { get; private set; } = null!;
    public int Anio { get; private set; }
    public decimal Precio { get; private set; }
    public int Kilometraje { get; private set; }
    public string Transmision { get; private set; } = null!;
    public string Combustible { get; private set; } = null!;

    // Lista persistida mediante Value Converter (ver paso abajo)
    public List<string> Accesorios { get; private set; } = new();
    public string Ubicacion { get; private set; } = null!;
    public string Descripcion { get; private set; } = null!;
    public string Estado { get; private set; } = null!;
    public bool PublicarAlGuardar { get; private set; }

    // EF Core mapeará esto automáticamente si usas el estándar de nombres _propiedad
    private readonly List<string> _fotos = new();
    public IReadOnlyCollection<string> Fotos => _fotos.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected Anuncio() { }

    public Anuncio(
        string marca,
        string modelo,
        string tipoVehiculo,
        string colorExterior,
        string colorInterior,
        int anio,
        decimal precio,
        int kilometraje,
        string transmision,
        string combustible,
        List<string> accesorios,
        string ubicacion,
        string descripcion)
    {
        if (string.IsNullOrWhiteSpace(marca) || string.IsNullOrWhiteSpace(modelo))
            throw new ArgumentException("La marca y el modelo son obligatorios.");

        if (precio <= 0)
            throw new ArgumentException("El precio debe ser mayor a cero.");

        if (anio < 1900 || anio > DateTime.UtcNow.Year + 1)
            throw new ArgumentException("Año de fabricación inválido.");

        Marca = marca;
        Modelo = modelo;
        TipoVehiculo = tipoVehiculo;
        ColorExterior = colorExterior;
        ColorInterior = colorInterior;
        Anio = anio;
        Precio = precio;
        Kilometraje = kilometraje;
        Transmision = transmision;
        Combustible = combustible;
        Accesorios = accesorios ?? new List<string>();
        Ubicacion = ubicacion;
        Descripcion = descripcion;
        Estado = "Borrador";
        CreatedAt = DateTime.UtcNow;
    }

    public void AgregarFotos(List<string> rutasFotos)
    {
        if (rutasFotos.Count < 5 || rutasFotos.Count > 10)
            throw new ArgumentException("El anuncio debe tener entre 5 a 10 fotos según las reglas de publicación.");

        if (_fotos.Count + rutasFotos.Count > 10)
        {
            throw new InvalidOperationException($"No se pueden agregar estas imágenes. El anuncio ya tiene {_fotos.Count} fotos y el límite máximo total es de 10.");
        }

        _fotos.AddRange(rutasFotos);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publicar()
    {
        if (_fotos.Count < 5)
            throw new InvalidOperationException("Imposible publicar: no se ha cumplido el requisito mínimo de fotos.");

        Estado = "Publicado";
        UpdatedAt = DateTime.UtcNow;
    }

    public void ActualizarInfo(
        string marca,
        string modelo,
        string tipoVehiculo,
        string colorExterior,
        string colorInterior,
        int anio,
        decimal precio,
        int kilometraje,
        string transmision,
        string combustible,
        List<string> accesorios,
        string ubicacion,
        string descripcion,
        bool publicarAlGuardar)
    {
        if (string.IsNullOrWhiteSpace(marca) || string.IsNullOrWhiteSpace(modelo))
            throw new ArgumentException("La marca y el modelo son obligatorios.");

        if (precio <= 0)
            throw new ArgumentException("El precio debe ser mayor a cero.");

        if (anio < 1900 || anio > DateTime.UtcNow.Year + 1)
            throw new ArgumentException("Año de fabricación inválido.");

        Marca = marca;
        Modelo = modelo;
        TipoVehiculo = tipoVehiculo;
        ColorExterior = colorExterior;
        ColorInterior = colorInterior;
        Anio = anio;
        Precio = precio;
        Kilometraje = kilometraje;
        Transmision = transmision;
        Combustible = combustible;
        Accesorios = accesorios ?? new List<string>();
        Ubicacion = ubicacion;
        Descripcion = descripcion;
        Estado = publicarAlGuardar ? "Publicado" : "Borrador";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarcarComoPublicado()
    {
        if (Estado == "Publicado") throw new InvalidOperationException("El anuncio ya está publicado.");
        Estado = "Publicado";
        UpdatedAt = DateTime.UtcNow;
    }
}
