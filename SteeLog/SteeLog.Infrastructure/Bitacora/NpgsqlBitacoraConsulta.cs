using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Bitacora
{
    public sealed class ParametroSqlBitacora
    {
        public string Nombre { get; set; }
        public object Valor { get; set; }
        public NpgsqlDbType Tipo { get; set; }
    }

    public sealed class SqlBitacora
    {
        public string Texto { get; set; }
        public List<ParametroSqlBitacora> Parametros { get; } = new List<ParametroSqlBitacora>();
    }

    /// <summary>Arma la consulta de la bitácora: el texto del usuario viaja siempre como parámetro.</summary>
    public static class ConsultaBitacoraSql
    {
        public static SqlBitacora Construir(BitacoraFiltro filtro)
        {
            filtro = filtro ?? new BitacoraFiltro();
            var sql = new SqlBitacora();
            var condiciones = new List<string>();
            if (filtro.Desde.HasValue)
            {
                condiciones.Add("fecha >= @desde");
                sql.Parametros.Add(new ParametroSqlBitacora { Nombre = "desde", Tipo = NpgsqlDbType.TimestampTz, Valor = ALocalAUtc(filtro.Desde.Value.Date) });
            }
            if (filtro.Hasta.HasValue)
            {
                condiciones.Add("fecha < @hasta"); // hasta el final del día elegido
                sql.Parametros.Add(new ParametroSqlBitacora { Nombre = "hasta", Tipo = NpgsqlDbType.TimestampTz, Valor = ALocalAUtc(filtro.Hasta.Value.Date.AddDays(1)) });
            }
            Igual(condiciones, sql, "usuario", filtro.Usuario);
            Igual(condiciones, sql, "entidad", filtro.Entidad);
            Igual(condiciones, sql, "accion", filtro.Accion);
            if (!string.IsNullOrWhiteSpace(filtro.Texto))
            {
                condiciones.Add("(descripcion ILIKE @texto OR entidad_id ILIKE @texto OR datos_antes::text ILIKE @texto OR datos_despues::text ILIKE @texto)");
                sql.Parametros.Add(new ParametroSqlBitacora { Nombre = "texto", Tipo = NpgsqlDbType.Text, Valor = "%" + EscaparLike(filtro.Texto.Trim()) + "%" });
            }
            var sb = new StringBuilder("SELECT id, fecha, usuario, rol, equipo, version_app, origen, accion, entidad, entidad_id, descripcion, datos_antes::text, datos_despues::text FROM public.bitacora_auditoria");
            if (condiciones.Count > 0) sb.Append(" WHERE ").Append(string.Join(" AND ", condiciones));
            // Se pide una fila de más para saber si el resultado quedó truncado.
            sb.Append(" ORDER BY fecha DESC, id DESC LIMIT @limite");
            sql.Parametros.Add(new ParametroSqlBitacora { Nombre = "limite", Tipo = NpgsqlDbType.Integer, Valor = filtro.LimiteEfectivo + 1 });
            sql.Texto = sb.ToString();
            return sql;
        }

        private static void Igual(List<string> condiciones, SqlBitacora sql, string columna, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return;
            condiciones.Add(columna + " = @" + columna);
            sql.Parametros.Add(new ParametroSqlBitacora { Nombre = columna, Tipo = NpgsqlDbType.Text, Valor = valor });
        }

        public static string EscaparLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        private static DateTime ALocalAUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
    }

    public sealed class NpgsqlBitacoraConsulta : IBitacoraConsulta
    {
        private const string TablaInexistente = "42P01";
        private readonly PostgresConnectionFactory factory;
        public NpgsqlBitacoraConsulta(PostgresConnectionFactory factory) { this.factory = factory ?? throw new ArgumentNullException(nameof(factory)); }

        public async Task<BitacoraResultado> BuscarAsync(BitacoraFiltro filtro, CancellationToken token = default)
        {
            filtro = filtro ?? new BitacoraFiltro();
            var resultado = new BitacoraResultado();
            try
            {
                using (var c = factory.Create())
                {
                    await c.OpenAsync(token).ConfigureAwait(false);
                    var sql = ConsultaBitacoraSql.Construir(filtro);
                    using (var cmd = new NpgsqlCommand(sql.Texto, c))
                    {
                        foreach (var p in sql.Parametros) cmd.Parameters.Add(new NpgsqlParameter(p.Nombre, p.Tipo) { Value = p.Valor });
                        using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                            while (await r.ReadAsync(token).ConfigureAwait(false))
                                resultado.Registros.Add(new BitacoraRegistro
                                {
                                    Id = r.GetInt64(0), Fecha = ALocal(r.GetDateTime(1)), Usuario = Txt(r, 2), Rol = Txt(r, 3), Equipo = Txt(r, 4),
                                    VersionApp = Txt(r, 5), Origen = Txt(r, 6), Accion = Txt(r, 7), Entidad = Txt(r, 8), EntidadId = Txt(r, 9),
                                    Descripcion = Txt(r, 10), DatosAntes = Txt(r, 11), DatosDespues = Txt(r, 12)
                                });
                    }
                }
            }
            catch (PostgresException ex) when (ex.SqlState == TablaInexistente)
            {
                return new BitacoraResultado { Instalada = false };
            }
            if (resultado.Registros.Count > filtro.LimiteEfectivo)
            {
                resultado.Truncado = true;
                resultado.Registros.RemoveRange(filtro.LimiteEfectivo, resultado.Registros.Count - filtro.LimiteEfectivo);
            }
            return resultado;
        }

        public async Task<BitacoraOpciones> OpcionesAsync(CancellationToken token = default)
        {
            var opciones = new BitacoraOpciones();
            try
            {
                using (var c = factory.Create())
                {
                    await c.OpenAsync(token).ConfigureAwait(false);
                    opciones.Usuarios = await DistintosAsync(c, "usuario", token).ConfigureAwait(false);
                    opciones.Entidades = await DistintosAsync(c, "entidad", token).ConfigureAwait(false);
                    opciones.Acciones = await DistintosAsync(c, "accion", token).ConfigureAwait(false);
                }
            }
            catch (PostgresException ex) when (ex.SqlState == TablaInexistente)
            {
                return new BitacoraOpciones { Instalada = false };
            }
            return opciones;
        }

        // La columna sale de constantes internas, nunca del usuario.
        private static async Task<List<string>> DistintosAsync(NpgsqlConnection c, string columna, CancellationToken token)
        {
            var lista = new List<string>();
            using (var cmd = new NpgsqlCommand("SELECT DISTINCT " + columna + " FROM public.bitacora_auditoria WHERE " + columna + " IS NOT NULL AND " + columna + " <> '' ORDER BY 1", c))
            using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                while (await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(r.GetString(0));
            return lista;
        }

        private static string Txt(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetValue(i).ToString();
        private static DateTime ALocal(DateTime d) => d.Kind == DateTimeKind.Local ? d : DateTime.SpecifyKind(d, DateTimeKind.Utc).ToLocalTime();
    }
}
