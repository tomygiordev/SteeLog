using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;

namespace SteeLog.Core.Application.Movimientos
{
    public sealed class RemitoLectura
    {
        public int IdMovimiento { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime FechaArchivo { get; set; }
        public string NroRemito { get; set; }
        public string TipoMovimiento { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string Observaciones { get; set; }
        public decimal CostoFlete { get; set; }
        public decimal PrecioTotal { get; set; }
        public List<RemitoLinea> Lineas { get; set; } = new List<RemitoLinea>();
    }
    public sealed class RemitoLinea
    {
        public string Material { get; set; }
        public decimal Cantidad { get; set; }
        public bool EsRecorte { get; set; }
        public string MedidaRecorte { get; set; }
        public bool EsInsumo { get; set; }
        public bool EsCompraEventual { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
    public interface IRemitoConsulta
    {
        Task<RemitoLectura> ObtenerAsync(int idMovimiento, CancellationToken token = default);
    }
    public interface IRemitoPdf
    {
        string SugerirNombreArchivo(RemitoLectura remito);
        void Generar(string rutaDestino, RemitoLectura remito);
    }
    public sealed class ConsultarRemito
    {
        private readonly Session session;
        private readonly IRemitoConsulta consulta;
        public ConsultarRemito(Session session, IRemitoConsulta consulta)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.consulta = consulta ?? throw new ArgumentNullException(nameof(consulta));
        }
        public Task<RemitoLectura> ObtenerAsync(int idMovimiento, CancellationToken token = default)
        {
            session.Require(Role.Galpon);
            return consulta.ObtenerAsync(idMovimiento, token);
        }
    }
}
