using System;

namespace SteeLog.Modelos
{
    public class Recorte
    {
        public int id { get; set; }
        public int id_material { get; set; }
        public string nombre_material { get; set; }
        public string nombre_categoria { get; set; }
        public decimal ancho { get; set; }
        public decimal altura { get; set; }
        public decimal medida { get; set; }
        public decimal cantidad { get; set; }
        public string estado { get; set; }
        public DateTime fecha_creacion { get; set; }

        public int Id { get => id; set => id = value; }
        public int IdMaterial { get => id_material; set => id_material = value; }
        public string NombreMaterial { get => nombre_material; set => nombre_material = value; }
        public string NombreCategoria { get => nombre_categoria; set => nombre_categoria = value; }
        public decimal Ancho { get => ancho; set => ancho = value; }
        public decimal Altura { get => altura; set => altura = value; }
        public decimal Medida { get => medida; set => medida = value; }
        public decimal Cantidad { get => cantidad; set => cantidad = value; }
        public string Estado { get => estado; set => estado = value; }
        public DateTime FechaCreacion { get => fecha_creacion; set => fecha_creacion = value; }
    }
}
