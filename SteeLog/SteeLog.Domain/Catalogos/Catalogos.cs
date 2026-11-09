using System;
namespace SteeLog.Domain.Catalogos
{
    public static class NombreCatalogo
    {
        /// <summary>Devuelve un mensaje amigable si el nombre no es válido; null si es válido.</summary>
        public static string Validar(string nombre, int maximo, string entidadConArticulo)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "Debes escribir un nombre.";
            if (nombre.Trim().Length > maximo) return "El nombre de " + entidadConArticulo + " no puede superar los " + maximo + " caracteres.";
            return null;
        }
    }
    public sealed class Categoria
    {
        public const int MaxNombre = 50;
        public static string ValidarNombre(string nombre) => NombreCatalogo.Validar(nombre, MaxNombre, "la categoría");
        public int Id { get; }
        public string Nombre { get; }
        public bool Recortable { get; }
        public Categoria(int id, string nombre, bool recortable)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            var errorNombre = ValidarNombre(nombre); if (errorNombre != null) throw new ArgumentException(errorNombre, nameof(nombre));
            Id = id; Nombre = nombre.Trim(); Recortable = recortable;
        }
    }
    public sealed class Obra
    {
        public const int MaxNombre = 100;
        public static string ValidarNombre(string nombre) => NombreCatalogo.Validar(nombre, MaxNombre, "la obra");
        public int Id { get; }
        public string Nombre { get; }
        public bool Activa { get; }
        public Obra(int id, string nombre, bool activa)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            var errorNombre = ValidarNombre(nombre); if (errorNombre != null) throw new ArgumentException(errorNombre, nameof(nombre));
            Id = id; Nombre = nombre.Trim(); Activa = activa;
        }
    }
    public sealed class Proveedor
    {
        public const int MaxNombre = 50;
        public static string ValidarNombre(string nombre) => NombreCatalogo.Validar(nombre, MaxNombre, "el proveedor");
        public int Id { get; }
        public string Nombre { get; }
        public Proveedor(int id, string nombre)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            var errorNombre = ValidarNombre(nombre); if (errorNombre != null) throw new ArgumentException(errorNombre, nameof(nombre));
            Id = id; Nombre = nombre.Trim();
        }
    }
    public sealed class Flete
    {
        public const int MaxNombre = 50;
        public static string ValidarNombre(string nombre) => NombreCatalogo.Validar(nombre, MaxNombre, "el flete");
        public int Id { get; }
        public string Nombre { get; }
        public bool Activo { get; }
        public Flete(int id, string nombre, bool activo)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            var errorNombre = ValidarNombre(nombre); if (errorNombre != null) throw new ArgumentException(errorNombre, nameof(nombre));
            Id = id; Nombre = nombre.Trim(); Activo = activo;
        }
    }
    public sealed class Ubicacion
    {
        public const int MaxNombre = 100;
        public static string ValidarNombre(string nombre) => NombreCatalogo.Validar(nombre, MaxNombre, "la ubicación");
        public int Id { get; }
        public string Nombre { get; }
        public Ubicacion(int id, string nombre)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            var errorNombre = ValidarNombre(nombre); if (errorNombre != null) throw new ArgumentException(errorNombre, nameof(nombre));
            Id = id; Nombre = nombre.Trim();
        }
    }
}
