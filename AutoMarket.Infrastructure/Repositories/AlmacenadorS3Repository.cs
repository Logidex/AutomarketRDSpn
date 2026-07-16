using Amazon.S3;
using Amazon.S3.Model;
using AutoMarket.Application.Services;
using Microsoft.Extensions.Configuration;

namespace AutoMarket.Infrastructure.Services;

public class AlmacenadorS3 : IAlmacenadorArchivos
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public AlmacenadorS3(IConfiguration configuration)
    {
        var s3Options = configuration.GetSection("AWS");

        // Inicializamos el cliente con tus llaves de acceso
        _s3Client = new AmazonS3Client(
            s3Options["AccessKey"],
            s3Options["SecretKey"],
            Amazon.RegionEndpoint.GetBySystemName(s3Options["Region"])
        );
        _bucketName = s3Options["BucketName"]!;
    }

    public async Task<string> GuardarArchivoAsync(Stream stream, string nombreArchivo, string contentType)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"uploads/{nombreArchivo}", // Lo guarda organizadito en una carpeta interna
            InputStream = stream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(putRequest);

        // Retorna la URL pública directa de AWS S3 para que se guarde en tu base de datos PostgreSQL
        return $"https://{_bucketName}.s3.{_s3Client.Config.RegionEndpoint.SystemName}://{nombreArchivo}";
    }

    public async Task EliminarArchivoAsync(string rutaArchivo)
    {
        if (string.IsNullOrEmpty(rutaArchivo)) return;

        try
        {
            // 1. Extraemos el nombre del archivo (Key) a partir de la URL completa de S3
            var uri = new Uri(rutaArchivo);
            // uri.AbsolutePath devolverá algo como "/uploads/nombre-de-archivo.jpg"
            // Le quitamos la barra inicial '/' para obtener el Key exacto de AWS S3: "uploads/nombre-de-archivo.jpg"
            var key = uri.AbsolutePath.TrimStart('/');

            // 2. Creamos la petición de eliminación correcta
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            // 3. Enviamos la orden de eliminación a Amazon S3
            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
        catch (Exception)
        {
            // En servicios de infraestructura como el borrado de archivos huérfanos, 
            // puedes registrar el error en un log para que no rompa el flujo principal si el archivo ya no existía en S3.
            throw;
        }
    }

}
