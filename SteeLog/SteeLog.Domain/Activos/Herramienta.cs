using System;
using System.Text.RegularExpressions;
namespace SteeLog.Domain.Activos
{
    public sealed class Herramienta
    {
        public int Id { get; private set; } public string Nombre { get; private set; } public string Estado { get; private set; }
        public int? ObraId { get; private set; } public string Observaciones { get; private set; }
        public Herramienta(int id,string nombre,string estado,int? obraId,string observaciones)
        { if(id<0) throw new ArgumentOutOfRangeException(nameof(id)); if(id==0 && estado != "Óptima" && estado != "Regular" && estado != "Mal estado") throw new ArgumentException("Estado de herramienta inválido."); Id=id; Aplicar(nombre,estado,obraId,observaciones); }
        public void Editar(string nombre,string estado,int? obraId,string observaciones)
        {
            if(estado != Estado && estado != "Óptima" && estado != "Regular" && estado != "Mal estado") throw new ArgumentException("Estado de herramienta inválido.");
            Aplicar(nombre,estado,obraId,observaciones);
        }
        private void Aplicar(string nombre,string estado,int? obraId,string observaciones)
        {
            if(string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length>255 || string.IsNullOrWhiteSpace(estado) || estado.Length>50 || (obraId.HasValue && obraId.Value<=0) || (observaciones != null && observaciones.Length>500)) throw new ArgumentException("Datos de la herramienta inválidos.");
            Nombre=nombre.Trim(); Estado=estado; ObraId=obraId; Observaciones=observaciones;
        }
        public void AsignarIdentidad(int id)
        {
            if(Id!=0 || id<=0) throw new InvalidOperationException("Identidad de herramienta inválida.");
            string nombre=Regex.IsMatch(Nombre,@"^\d+") ? Nombre : id.ToString()+Nombre;
            if(nombre.Length>255) throw new ArgumentException("El nombre con su identificador supera 255 caracteres.");
            Id=id; Nombre=nombre;
        }
        public void EnviarAGalpon()
        { if(!ObraId.HasValue) throw new InvalidOperationException("La herramienta ya se encuentra en el galpón."); ObraId=null; }
    }
}
