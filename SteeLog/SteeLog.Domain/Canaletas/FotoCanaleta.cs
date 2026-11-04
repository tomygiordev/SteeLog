using System;
namespace SteeLog.Domain.Canaletas
{
    public sealed class FotoCanaleta
    {
        public string ObjectPath { get; }
        public string NombreArchivo { get; }
        public string ContentType { get; }
        public long TamanioBytes { get; }
        public int Orden { get; }
        public FotoCanaleta(string objectPath, string nombreArchivo, string contentType, long tamanioBytes, int orden)
        {
            if (string.IsNullOrWhiteSpace(objectPath)) throw new ArgumentException("Falta la clave de la foto.");
            if (string.IsNullOrWhiteSpace(nombreArchivo)) throw new ArgumentException("Falta el nombre de la foto.");
            if (contentType != "image/jpeg" && contentType != "image/png" && contentType != "image/webp") throw new ArgumentException("Tipo de foto inválido.");
            if (tamanioBytes <= 0 || tamanioBytes > 8388608) throw new ArgumentException("Tamaño de foto inválido.");
            if (orden <= 0) throw new ArgumentException("Orden de foto inválido.");
            ObjectPath = objectPath; NombreArchivo = nombreArchivo; ContentType = contentType; TamanioBytes = tamanioBytes; Orden = orden;
        }
    }
}
