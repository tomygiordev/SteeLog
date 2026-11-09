using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class TransaccionTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExitoRespetaOrdenResultadoYToken(bool sinResultado)
        {
            var uow = new Uow();
            using (var cts = new CancellationTokenSource())
            {
                int resultado = await Ejecutar(uow, sinResultado, cts.Token);
                Assert.Equal(sinResultado ? 0 : 42, resultado);
                Assert.Equal(new[] { "begin", "trabajo", "commit" }, uow.Pasos);
                Assert.Equal(new[] { cts.Token, cts.Token, cts.Token }, uow.Tokens);
            }
        }

        [Theory]
        [InlineData("begin", false, false)]
        [InlineData("begin", false, true)]
        [InlineData("trabajo", false, false)]
        [InlineData("trabajo", false, true)]
        [InlineData("trabajo", true, false)]
        [InlineData("trabajo", true, true)]
        [InlineData("commit", false, false)]
        [InlineData("commit", false, true)]
        [InlineData("commit", true, false)]
        [InlineData("commit", true, true)]
        public async Task FallosConservanCausaYRollbackNoLaReemplaza(string etapa, bool fallaRollback, bool sinResultado)
        {
            var original = new InvalidOperationException("fallo original");
            var uow = new Uow { EtapaFallida = etapa, Error = original, FallaRollback = fallaRollback };
            if (etapa == "commit")
            {
                var error = await Assert.ThrowsAsync<ResultadoEscrituraInciertoException>(() => Ejecutar(uow, sinResultado));
                Assert.Same(original, error.InnerException);
            }
            else
            {
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Ejecutar(uow, sinResultado));
                Assert.Same(original, error);
            }
            Assert.Equal(PasosHastaFallo(etapa), uow.Pasos);
        }

        [Theory]
        [InlineData("antes", false)]
        [InlineData("antes", true)]
        [InlineData("begin", false)]
        [InlineData("begin", true)]
        [InlineData("trabajo", false)]
        [InlineData("trabajo", true)]
        [InlineData("despuesTrabajo", false)]
        [InlineData("despuesTrabajo", true)]
        [InlineData("commit", false)]
        [InlineData("commit", true)]
        public async Task CancelacionSoloEsInciertaDesdeInicioDeCommit(string etapa, bool sinResultado)
        {
            using (var cts = new CancellationTokenSource())
            {
                var original = new OperationCanceledException(cts.Token);
                var uow = new Uow { EtapaFallida = etapa, Error = original };
                if (etapa == "antes") cts.Cancel();
                if (etapa == "despuesTrabajo") uow.DespuesTrabajo = cts.Cancel;
                if (etapa == "commit")
                {
                    var error = await Assert.ThrowsAsync<ResultadoEscrituraInciertoException>(() => Ejecutar(uow, sinResultado, cts.Token));
                    Assert.Same(original, error.InnerException);
                }
                else
                {
                    var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Ejecutar(uow, sinResultado, cts.Token));
                    Assert.Equal(cts.Token, error.CancellationToken);
                    if (etapa != "antes" && etapa != "despuesTrabajo") Assert.Same(original, error);
                }
                Assert.Equal(etapa == "antes" ? new string[0] : PasosHastaFallo(etapa), uow.Pasos);
                foreach (var token in uow.Tokens) Assert.Equal(cts.Token, token);
            }
        }

        private static async Task<int> Ejecutar(Uow uow, bool sinResultado, CancellationToken token = default)
        {
            if (sinResultado)
            {
                await Transaccion.EjecutarAsync(uow, (Func<CancellationToken, Task>)(t => uow.TrabajoAsync(t)), token);
                return 0;
            }
            return await Transaccion.EjecutarAsync(uow, t => uow.TrabajoAsync(t), token);
        }

        private static string[] PasosHastaFallo(string etapa)
        {
            if (etapa == "begin") return new[] { "begin" };
            if (etapa == "commit") return new[] { "begin", "trabajo", "commit", "rollback" };
            return new[] { "begin", "trabajo", "rollback" };
        }

        private sealed class Uow : IUnitOfWork
        {
            public readonly List<string> Pasos = new List<string>();
            public readonly List<CancellationToken> Tokens = new List<CancellationToken>();
            public string EtapaFallida;
            public Exception Error;
            public bool FallaRollback;
            public Action DespuesTrabajo;

            private Task PasoAsync(string etapa, CancellationToken token)
            {
                Pasos.Add(etapa);
                Tokens.Add(token);
                return etapa == EtapaFallida ? Task.FromException(Error) : Task.CompletedTask;
            }

            public Task BeginAsync(CancellationToken token = default) => PasoAsync("begin", token);
            public Task CommitAsync(CancellationToken token = default) => PasoAsync("commit", token);
            public async Task<int> TrabajoAsync(CancellationToken token)
            {
                await PasoAsync("trabajo", token);
                DespuesTrabajo?.Invoke();
                return 42;
            }
            public Task RollbackAsync()
            {
                Pasos.Add("rollback");
                return FallaRollback ? Task.FromException(new Exception("fallo de limpieza")) : Task.CompletedTask;
            }
        }
    }
}
