using System;
namespace SteeLog.Domain.Catalogos
{
    public sealed class Categoria
    {
        public int Id { get; }
        public string Nombre { get; }
        public bool Recortable { get; }
        public Categoria(int id, string nombre, bool recortable)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 50) throw new ArgumentException("Nombre de categoria inválido.", nameof(nombre));
            Id = id; Nombre = nombre.Trim(); Recortable = recortable;
        }
    }
    public sealed class Obra
    {
        public int Id { get; }
        public string Nombre { get; }
        public bool Activa { get; }
        public Obra(int id, string nombre, bool activa)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100) throw new ArgumentException("Nombre de obra inválido.", nameof(nombre));
            Id = id; Nombre = nombre.Trim(); Activa = activa;
        }
    }
    public sealed class Proveedor
    {
        public int Id { get; }
        public string Nombre { get; }
        public Proveedor(int id, string nombre)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 50) throw new ArgumentException("Nombre de proveedor inválido.", nameof(nombre));
            Id = id; Nombre = nombre.Trim();
        }
    }
    public sealed class Flete
    {
        public int Id { get; }
        public string Nombre { get; }
        public bool Activo { get; }
        public Flete(int id, string nombre, bool activo)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 50) throw new ArgumentException("Nombre de flete inválido.", nameof(nombre));
            Id = id; Nombre = nombre.Trim(); Activo = activo;
        }
    }
    public sealed class Ubicacion
    {
        public int Id { get; }
        public string Nombre { get; }
        public Ubicacion(int id, string nombre)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100) throw new ArgumentException("Nombre de ubicacion inválido.", nameof(nombre));
            Id = id; Nombre = nombre.Trim();
        }
    }
}
