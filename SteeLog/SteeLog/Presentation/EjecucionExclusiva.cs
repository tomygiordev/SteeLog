using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;

namespace SteeLog.Presentation
{
    public sealed class EjecucionExclusiva
    {
        private readonly Action<bool> setOcupada;
        private readonly Action<Exception> mostrarError;
        private int ocupado;

        public EjecucionExclusiva(Action<bool> setOcupada, Action<Exception> mostrarError)
        {
            this.setOcupada = setOcupada;
            this.mostrarError = mostrarError;
        }

        public async Task<T> Ejecutar<T>(Func<Task<T>> operacion, Action<ResultadoEscrituraInciertoException> onResultadoIncierto = null)
        {
            if (Interlocked.CompareExchange(ref ocupado, 1, 0) != 0) return default;
            try { setOcupada(true); return await operacion(); }
            catch (OperationCanceledException) { return default; }
            catch (ResultadoEscrituraInciertoException e)
            {
                if (onResultadoIncierto != null) onResultadoIncierto(e); else mostrarError(e);
                return default;
            }
            catch (Exception e) { mostrarError(e); return default; }
            finally { Interlocked.Exchange(ref ocupado, 0); setOcupada(false); }
        }
    }
}
