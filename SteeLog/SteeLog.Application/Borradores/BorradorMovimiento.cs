using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace SteeLog.Core.Application.Borradores
{
    public sealed class BorradorMovimiento
    {
        private readonly IDraftStore store;
        private readonly Func<DateTime> ahora;
        public BorradorMovimiento(IDraftStore store, Func<DateTime> ahora)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.ahora = ahora ?? throw new ArgumentNullException(nameof(ahora));
        }
        public void Guardar(MovimientoDraft draft)
        {
            if (draft == null) return;
            draft.FechaGuardado = ahora();
            draft.Version = Guid.NewGuid().ToString("N");
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(MovimientoDraft)).WriteObject(stream, draft);
                store.Guardar(stream.ToArray());
            }
        }
        public MovimientoDraft Leer()
        {
            var bytes = store.Leer();
            if (bytes == null || bytes.Length == 0) return null;
            MovimientoDraft draft;
            try
            {
                using (var stream = new MemoryStream(bytes))
                    draft = (MovimientoDraft)new DataContractJsonSerializer(typeof(MovimientoDraft)).ReadObject(stream);
            }
            catch
            {
                store.Eliminar();
                return null;
            }
            if (draft == null || ahora() - draft.FechaGuardado > TimeSpan.FromHours(24))
            {
                store.Eliminar();
                return null;
            }
            return draft;
        }
        public void Eliminar() => store.Eliminar();
        public string VersionActual()
        {
            var draft = Leer();
            if (draft == null) return null;
            return draft.Version ?? "legacy:" + draft.FechaGuardado.Ticks;
        }
        public void EliminarSiCoincide(string version)
        {
            if (version != null && string.Equals(version, VersionActual(), StringComparison.Ordinal)) store.Eliminar();
        }
    }
}
