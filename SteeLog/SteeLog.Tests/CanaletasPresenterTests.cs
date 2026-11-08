using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Presentation;
using Xunit;

public sealed class CanaletasPresenterTests
{
    [Fact]
    public async Task FalloDeRecargaTrasOperacionConfirmadaSeAdvierteSinRepetirEscritura()
    {
        int recargas = 0;
        var avisos = new List<Exception>();
        await CanaletasPresenter.RefrescarTrasOperacionAsync(() => { recargas++; throw new IOException("sin lectura"); }, () => true, avisos.Add);
        Assert.Equal(1, recargas);
        Assert.Equal("sin lectura", Assert.Single(avisos).Message);
    }

    [Fact]
    public async Task RecargaCanceladaOConVistaCerradaNoAdvierte()
    {
        var avisos = new List<Exception>();
        await CanaletasPresenter.RefrescarTrasOperacionAsync(() => throw new OperationCanceledException(), () => true, avisos.Add);
        await CanaletasPresenter.RefrescarTrasOperacionAsync(() => throw new IOException(), () => false, avisos.Add);
        Assert.Empty(avisos);
    }
}
