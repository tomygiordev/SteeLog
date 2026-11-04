using System;
namespace SteeLog.Core.Application.Common
{
    public sealed class ResultadoEscrituraInciertoException : Exception
    {
        public ResultadoEscrituraInciertoException(Exception causa)
            : base("Resultado incierto: verificar persistencia antes de reintentar.", causa) { }
    }
}
