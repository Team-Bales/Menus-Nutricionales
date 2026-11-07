using Menus.Application.Catalogo.Alimentos.ActualizarAlimento;
using Menus.Application.Catalogo.Alimentos.EliminarAlimento;
using Menus.Application.Catalogo.Alimentos.ListarAlimentos;
using Menus.Application.Catalogo.Alimentos.ObtenerAlimento;
using Menus.Application.Catalogo.Alimentos.RegistrarAlimento;
using Menus.Application.Comun;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Catalogo.Alimentos;

/// <summary>Pruebas de los manejadores del #61 contra el banco en memoria.</summary>
public sealed class ManejadoresDeAlimentosTests
{
    private readonly BancoFalso _banco = new();

    private RegistrarAlimentoHandler Registrar(Guid? actor = null) => new(
        _banco.ItemsRepo, _banco.AlimentosRepo, _banco.UnidadDeTrabajo, new ActorFalso(actor ?? BancoFalso.Descriptor));

    private ActualizarAlimentoHandler Actualizar(Guid? actor = null) => new(
        _banco.AlimentosRepo, _banco.UnidadDeTrabajo, new ActorFalso(actor ?? BancoFalso.Descriptor));

    private EliminarAlimentoHandler Eliminar() => new(_banco.ItemsRepo, _banco.AlimentosRepo, _banco.UnidadDeTrabajo);

    // ---- Registrar ----

    [Fact]
    public async Task Registrar_guarda_el_alimento_con_el_actor_como_descriptor_y_confirma_una_vez()
    {
        var comando = new RegistrarAlimentoCommand("  Avena  ", BancoFalso.GrupoCereales, TipoPreparacion.Entrante.Id, NivelCalorico.Bajo.Id);

        var creado = await Registrar().HandleAsync(comando, CancellationToken.None);

        var alimento = Assert.Single(_banco.Items);
        Assert.Equal(creado.Id, alimento.Id);
        Assert.Equal("Avena", alimento.Nombre);
        Assert.Equal(BancoFalso.Descriptor, alimento.NutricionistaId);
        Assert.Equal(TipoPreparacion.Entrante.Id, alimento.TipoPreparacionId);
        Assert.Equal(NivelCalorico.Bajo.Id, alimento.NivelCaloricoId);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_con_un_nombre_que_ya_existe_salvo_mayusculas_lanza_RC01_sin_confirmar()
    {
        _banco.Agregar("Avena");
        var comando = new RegistrarAlimentoCommand("AVENA ", BancoFalso.GrupoCereales, TipoPreparacion.Entrante.Id, NivelCalorico.Bajo.Id);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => Registrar().HandleAsync(comando, CancellationToken.None));

        Assert.Equal("RC-01", ex.Regla);
        Assert.Single(_banco.Items);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- Actualizar ----

    [Fact]
    public async Task Actualizar_reclasifica_el_alimento_y_confirma()
    {
        var alimento = _banco.Agregar("Avena");
        var comando = new ActualizarAlimentoCommand(alimento.Id, BancoFalso.GrupoCereales, TipoPreparacion.Postre.Id, NivelCalorico.Alto.Id);

        var resultado = await Actualizar().HandleAsync(comando, CancellationToken.None);

        Assert.Same(Unidad.Valor, resultado);
        Assert.Equal(TipoPreparacion.Postre.Id, alimento.TipoPreparacionId);
        Assert.Equal(NivelCalorico.Alto.Id, alimento.NivelCaloricoId);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_un_alimento_que_no_existe_lanza_NoEncontrado()
    {
        var comando = new ActualizarAlimentoCommand(Guid.NewGuid(), BancoFalso.GrupoCereales, TipoPreparacion.Postre.Id, NivelCalorico.Alto.Id);

        await Assert.ThrowsAsync<NoEncontradoException>(() => Actualizar().HandleAsync(comando, CancellationToken.None));
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_por_otro_nutricionista_lanza_RS02_sin_confirmar()
    {
        var alimento = _banco.Agregar("Avena");
        var comando = new ActualizarAlimentoCommand(alimento.Id, BancoFalso.GrupoCereales, TipoPreparacion.Postre.Id, NivelCalorico.Alto.Id);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Actualizar(actor: Guid.NewGuid()).HandleAsync(comando, CancellationToken.None));

        Assert.Equal("RS-02", ex.Regla);
        Assert.Equal(TipoPreparacion.PlatoFuerte.Id, alimento.TipoPreparacionId);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- Eliminar ----

    [Fact]
    public async Task Eliminar_un_alimento_sin_uso_lo_quita_y_confirma()
    {
        var alimento = _banco.Agregar("Avena");

        await Eliminar().HandleAsync(new EliminarAlimentoCommand(alimento.Id), CancellationToken.None);

        Assert.Empty(_banco.Items);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Eliminar_un_alimento_que_no_existe_lanza_NoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(
            () => Eliminar().HandleAsync(new EliminarAlimentoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Eliminar_un_ingrediente_de_un_plato_lanza_RR04_sin_quitarlo()
    {
        var alimento = _banco.Agregar("Avena");
        _banco.EnPlatos.Add(alimento.Id);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Eliminar().HandleAsync(new EliminarAlimentoCommand(alimento.Id), CancellationToken.None));

        Assert.Equal("RR-04", ex.Regla);
        Assert.Contains("Avena", ex.Message);
        Assert.Single(_banco.Items);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Eliminar_un_alimento_que_figura_en_un_menu_lanza_RR22_sin_quitarlo()
    {
        var alimento = _banco.Agregar("Avena");
        _banco.EnMenus.Add(alimento.Id);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Eliminar().HandleAsync(new EliminarAlimentoCommand(alimento.Id), CancellationToken.None));

        Assert.Equal("RR-22", ex.Regla);
        Assert.Single(_banco.Items);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- Consultas ----

    [Fact]
    public async Task Listar_devuelve_la_pagina_pedida_con_el_total_filtrado()
    {
        _banco.Agregar("Arroz", nivel: NivelCalorico.Alto.Id);
        _banco.Agregar("Avena", nivel: NivelCalorico.Bajo.Id);
        _banco.Agregar("Berro", nivel: NivelCalorico.Bajo.Id);
        _banco.Agregar("Col", nivel: NivelCalorico.Bajo.Id);

        var resultado = await new ListarAlimentosHandler(_banco.AlimentosRepo).HandleAsync(
            new ListarAlimentosQuery(NivelCaloricoId: NivelCalorico.Bajo.Id, Page: 2, Size: 2),
            CancellationToken.None);

        Assert.Equal(3, resultado.Total);
        Assert.Equal(2, resultado.Page);
        Assert.Equal(2, resultado.Size);
        Assert.Equal("Col", Assert.Single(resultado.Data).Nombre);
    }

    [Fact]
    public async Task Obtener_devuelve_el_dto_del_alimento()
    {
        var alimento = _banco.Agregar("Avena");

        var dto = await new ObtenerAlimentoHandler(_banco.AlimentosRepo).HandleAsync(
            new ObtenerAlimentoQuery(alimento.Id), CancellationToken.None);

        Assert.Equal(alimento.Id, dto.Id);
        Assert.Equal("Avena", dto.Nombre);
        Assert.Equal(alimento.GrupoNutricionalId, dto.GrupoNutricionalId);
        Assert.Equal(alimento.TipoPreparacionId, dto.TipoPreparacionId);
        Assert.Equal(alimento.NivelCaloricoId, dto.NivelCaloricoId);
        Assert.Equal(BancoFalso.Descriptor, dto.NutricionistaId);
    }

    [Fact]
    public async Task Obtener_un_alimento_que_no_existe_lanza_NoEncontrado()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => new ObtenerAlimentoHandler(_banco.AlimentosRepo)
            .HandleAsync(new ObtenerAlimentoQuery(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("alimento", ex.Recurso);
    }
}
