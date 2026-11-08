using System;
using System.ComponentModel;

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

        [Browsable(false)] public int Id { get => id; set => id = value; }
        [Browsable(false)] public int IdMaterial { get => id_material; set => id_material = value; }
        [Browsable(false)] public string NombreMaterial { get => nombre_material; set => nombre_material = value; }
        [Browsable(false)] public string NombreCategoria { get => nombre_categoria; set => nombre_categoria = value; }
        [Browsable(false)] public decimal Ancho { get => ancho; set => ancho = value; }
        [Browsable(false)] public decimal Altura { get => altura; set => altura = value; }
        [Browsable(false)] public decimal Medida { get => medida; set => medida = value; }
        [Browsable(false)] public decimal Cantidad { get => cantidad; set => cantidad = value; }
        [Browsable(false)] public string Estado { get => estado; set => estado = value; }
        [Browsable(false)] public DateTime FechaCreacion { get => fecha_creacion; set => fecha_creacion = value; }
    }
}
