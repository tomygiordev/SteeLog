using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SteeLog.Core.Application.Respaldos
{
    /// <summary>Configuración local de las copias (JSON en la carpeta de datos local o en la raíz demo).</summary>
    public interface IConfiguracionRespaldos
    {
        ConfiguracionRespaldo Cargar();
        void Guardar(ConfiguracionRespaldo configuracion);
    }

    /// <summary>pg_dump / pg_restore oficiales. No lanza por fallos de las herramientas: los informa en el resultado.</summary>
    public interface IMotorRespaldo
    {
        Task<EstadoHerramientas> DetectarAsync(CancellationToken token);
        /// <summary>Genera el archivo (formato custom) en la ruta indicada.</summary>
        Task<ResultadoMotor> CrearAsync(string archivoDestino, IProgress<ProgresoRespaldo> progreso, CancellationToken token);
        /// <summary>pg_restore --list: valida que el archivo se pueda leer y cuenta objetos.</summary>
        Task<ResultadoMotor> ListarAsync(string archivo, CancellationToken token);
        Task<ResultadoMotor> RestaurarAsync(string archivo, IProgress<ProgresoRespaldo> progreso, CancellationToken token);
    }

    public interface IBaseRespaldo
    {
        /// <summary>No lanza: si no conecta, Conectada=false con el motivo.</summary>
        Task<InfoBase> ConsultarAsync(CancellationToken token);
        /// <summary>Filas de cada tabla (esquema public). Las tablas inexistentes se omiten.</summary>
        Task<IReadOnlyDictionary<string, long>> ContarFilasAsync(IReadOnlyList<string> tablas, CancellationToken token);
        /// <summary>True si hay un pg_restore de esta base esperando un bloqueo de otra sesión (para no esperar sin límite).</summary>
        Task<bool> RestauracionEsperaBloqueoAsync(CancellationToken token);
        /// <summary>Cierra las conexiones agrupadas de este proceso (antes de restaurar).</summary>
        void LiberarConexiones();
    }

    /// <summary>Archivos de la carpeta de copias. Los Id son nombres de archivo, nunca rutas.</summary>
    public interface IAlmacenRespaldos
    {
        void AsegurarCarpeta(string carpeta);
        /// <summary>Lanza si no se puede crear ni escribir en la carpeta.</summary>
        void ComprobarEscritura(string carpeta);
        IReadOnlyList<ArchivoRespaldo> Listar(string carpeta);
        FichaRespaldo LeerFicha(string carpeta, string id);
        /// <summary>Ruta completa del .dump si el Id es válido y existe dentro de la carpeta; si no, null.</summary>
        string RutaDe(string carpeta, string id);
        string NuevaRutaTemporal(string carpeta);
        /// <summary>Mueve el temporal a su nombre definitivo (nunca pisa otro archivo) y devuelve ese nombre.</summary>
        string Publicar(string temporal, string carpeta, DateTime fechaLocal, TipoRespaldo tipo);
        void EscribirFicha(string carpeta, string id, FichaRespaldo ficha);
        void Eliminar(string carpeta, string id);
        void BorrarSiExiste(string ruta);
        bool ExisteArchivo(string ruta);
        long TamanoDe(string ruta);
        string CalcularSha256(string ruta, CancellationToken token);
        void Copiar(string origen, string destino, CancellationToken token);
        long? EspacioLibre(string carpeta);
        /// <summary>Evita operaciones simultáneas (también entre instancias). Null si ya hay otra en curso.</summary>
        IDisposable Bloquear(string carpeta);
    }
}
