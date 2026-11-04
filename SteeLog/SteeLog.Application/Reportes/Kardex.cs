using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
namespace SteeLog.Core.Application.Reportes
{
    public sealed class KardexMaterial { public int id { get; set; } public string nombre { get; set; } public string nombre_categoria { get; set; } }
    public sealed class KardexItem
    {
        public int id_movimiento { get; set; } public int? id_material { get; set; }
        public DateTime fecha { get; set; } public string tipo_movimiento { get; set; }
        public decimal cantidad { get; set; } public string origen { get; set; } public string destino { get; set; }
        public decimal saldo { get; set; } public string nombre_material { get; set; }
    }
    public sealed class KardexSnapshot
    {
        public List<KardexItem> Historial { get; set; }
        public string MaterialNombre { get; set; }
        public DateTime FechaInicio { get; set; } public DateTime FechaFin { get; set; }
        public bool MostrarColumnaMaterial { get; set; } public bool MostrarColumnaSaldo { get; set; }
    }
    public interface IKardexPdf { void Generar(string ruta, KardexSnapshot snapshot); }
    public interface IKardexConsulta
    {
        Task<List<KardexMaterial>> MaterialesAsync(CancellationToken token = default);
        Task<List<KardexItem>> HistorialAsync(int? material, DateTime? desde, DateTime? hasta, CancellationToken token = default);
    }
    public sealed class ConsultarKardex
    {
        private readonly Session actor; private readonly IKardexConsulta consulta;
        public ConsultarKardex(Session actor, IKardexConsulta consulta) { this.actor = actor ?? throw new ArgumentNullException(nameof(actor)); this.consulta = consulta ?? throw new ArgumentNullException(nameof(consulta)); }
        public Task<List<KardexMaterial>> MaterialesAsync(CancellationToken token) { actor.Require(Role.Galpon); return consulta.MaterialesAsync(token); }
        public Task<List<KardexItem>> HistorialAsync(int? material, DateTime? desde, DateTime? hasta, CancellationToken token) { actor.Require(Role.Galpon); return consulta.HistorialAsync(material, desde, hasta, token); }
    }
}
