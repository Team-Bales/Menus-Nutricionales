namespace Menus.Application.Tests.IA;

using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using Menus.Application.IA;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Pruebas para los contratos de IA añadidos en #324:
/// <see cref="BloqueDeContexto"/>, <see cref="ResultadoDeContexto"/>, <see cref="ContextoDeCaso"/>,
/// <see cref="TransformadorDePaciente"/>, <see cref="EstadoConsentimientoIA"/>, <see cref="IProveedorDeContexto"/>
/// y el registro <see cref="DependencyInjection.AddIA"/>.
/// </summary>
public sealed class ContratosIA324Tests
{
    #region BloqueDeContexto

    [Fact]
    public void BloqueDeContexto_ConstruccionValida_AsignaCampos()
    {
        var cifras = new Dictionary<string, double> { { "calorias", 2000.0 }, { "proteinas", 80.5 } };
        var bloque = new BloqueDeContexto("valoracion", 1, "Resumen de prueba", cifras);

        Assert.Equal("valoracion", bloque.Fuente);
        Assert.Equal(1, bloque.Version);
        Assert.Equal("Resumen de prueba", bloque.Resumen);
        Assert.Equal(2, bloque.Cifras.Count);
        Assert.Equal(2000.0, bloque.Cifras["calorias"]);
        Assert.Equal(80.5, bloque.Cifras["proteinas"]);
    }

    [Fact]
    public void BloqueDeContexto_FuenteVacia_Lanza()
    {
        Assert.Throws<ArgumentException>(() => new BloqueDeContexto("", 1, "Resumen", null));
        Assert.Throws<ArgumentException>(() => new BloqueDeContexto("   ", 1, "Resumen", null));
        Assert.Throws<ArgumentException>(() => new BloqueDeContexto(null!, 1, "Resumen", null));
    }

    [Fact]
    public void BloqueDeContexto_VersionMenorQue1_Lanza()
    {
        Assert.Throws<ArgumentException>(() => new BloqueDeContexto("fuente", 0, "Resumen", null));
        Assert.Throws<ArgumentException>(() => new BloqueDeContexto("fuente", -1, "Resumen", null));
    }

    [Fact]
    public void BloqueDeContexto_ResumenNulo_Lanza()
    {
        Assert.Throws<ArgumentNullException>(() => new BloqueDeContexto("fuente", 1, null!, null));
    }

    [Fact]
    public void BloqueDeContexto_CifrasNull_NormalizaAVacio()
    {
        var bloque = new BloqueDeContexto("fuente", 1, "Resumen", null);
        Assert.Empty(bloque.Cifras);
    }

    [Fact]
    public void BloqueDeContexto_CifrasConNaN_Lanza()
    {
        var cifras = new Dictionary<string, double> { { "a", double.NaN } };
        Assert.Throws<ArgumentException>(() => new BloqueDeContexto("fuente", 1, "Resumen", cifras));
    }

    [Fact]
    public void BloqueDeContexto_CifrasConInfinity_Lanza()
    {
        var cifras = new Dictionary<string, double> { { "a", double.PositiveInfinity } };
        Assert.Throws<ArgumentException>(() => new BloqueDeContexto("fuente", 1, "Resumen", cifras));
        cifras = new Dictionary<string, double> { { "a", double.NegativeInfinity } };
        Assert.Throws<ArgumentException>(() => new BloqueDeContexto("fuente", 1, "Resumen", cifras));
    }

    [Fact]
    public void BloqueDeContexto_CifrasClaveNula_Lanza()
    {
        // Dictionary<string, double> no permite claves nulas, la excepción se lanza al agregar
        var cifras = new Dictionary<string, double>();
        Assert.Throws<ArgumentNullException>(() => cifras.Add(null!, 1.0));
    }

    [Fact]
    public void BloqueDeContexto_CopiaDefensiva_MutarOriginalNoCambiaBloque()
    {
        var cifras = new Dictionary<string, double> { { "a", 1.0 } };
        var bloque = new BloqueDeContexto("fuente", 1, "Resumen", cifras);
        cifras["a"] = 999.0;
        Assert.Equal(1.0, bloque.Cifras["a"]);
    }

    [Fact]
    public void BloqueDeContexto_ToString_NoExponeResumenNiCifras()
    {
        var bloque = new BloqueDeContexto("valoracion", 2, "Resumen secreto", new Dictionary<string, double> { { "secreto", 42.0 } });
        var toString = bloque.ToString();

        Assert.Contains("Fuente = valoracion", toString);
        Assert.Contains("Version = 2", toString);
        Assert.Contains("[redacted]", toString);
        Assert.DoesNotContain("Resumen secreto", toString);
        Assert.DoesNotContain("42", toString);
    }

    #endregion

    #region ResultadoDeContexto

    [Fact]
    public void ResultadoDeContexto_ConstruccionValida_AsignaCampos()
    {
        var bloque = new BloqueDeContexto("fuente", 1, "Resumen", null);
        var advertencias = new List<string> { "Adv1", "Adv2" };
        var resultado = new ResultadoDeContexto(bloque, advertencias);

        Assert.Same(bloque, resultado.Bloque);
        Assert.Equal(2, resultado.Advertencias.Count);
        Assert.Equal("Adv1", resultado.Advertencias[0]);
    }

    [Fact]
    public void ResultadoDeContexto_BloqueNull_Valido()
    {
        var resultado = new ResultadoDeContexto(null, new[] { "Adv" });
        Assert.Null(resultado.Bloque);
        Assert.Single(resultado.Advertencias);
    }

    [Fact]
    public void ResultadoDeContexto_AdvertenciasNull_NormalizaAVacio()
    {
        var bloque = new BloqueDeContexto("fuente", 1, "Resumen", null);
        var resultado = new ResultadoDeContexto(bloque, null);
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public void ResultadoDeContexto_CopiaDefensiva_MutarOriginalNoCambiaResultado()
    {
        var advertencias = new List<string> { "A1" };
        var resultado = new ResultadoDeContexto(null, advertencias);
        advertencias.Add("A2");
        Assert.Single(resultado.Advertencias);
    }

    [Fact]
    public void ResultadoDeContexto_ToString_NoExponeAdvertencias()
    {
        var resultado = new ResultadoDeContexto(null, new[] { "Advertencia secreta" });
        var toString = resultado.ToString();

        Assert.Contains("Bloque = null", toString);
        Assert.Contains("Advertencias = 1", toString);
        Assert.DoesNotContain("Advertencia secreta", toString);
    }

    #endregion

    #region ContextoDeCaso y PacienteSnapshot

    [Fact]
    public void ContextoDeCaso_UnPaciente_CodigoEsElPaciente()
    {
        var paciente = new PacienteSnapshot
        {
            Id = Guid.NewGuid(),
            Nombre = "Juan",
            Edad = 30,
            SalaId = Guid.NewGuid(),
            ConsentimientoIA = EstadoConsentimientoIA.NoRegistrado
        };
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));

        Assert.Equal("el paciente", contexto.ObtenerCodigoPaciente(paciente.Id));
    }

    [Fact]
    public void ContextoDeCaso_VariosPacientes_CodigosP1P2Estables()
    {
        var id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var p1 = new PacienteSnapshot { Id = id1, Nombre = "A", Edad = 30, SalaId = Guid.NewGuid() };
        var p2 = new PacienteSnapshot { Id = id2, Nombre = "B", Edad = 40, SalaId = Guid.NewGuid() };

        var contexto = new ContextoDeCaso(new[] { p1, p2 }, DateOnly.FromDateTime(DateTime.Today));

        Assert.Equal("P1", contexto.ObtenerCodigoPaciente(id1));
        Assert.Equal("P2", contexto.ObtenerCodigoPaciente(id2));
    }

    [Fact]
    public void ContextoDeCaso_VariosPacientes_OrdenEstablePorGuid()
    {
        // GUIDs ordenados lexicográficamente
        var idA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var idB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var pA = new PacienteSnapshot { Id = idA, Nombre = "A", Edad = 30, SalaId = Guid.NewGuid() };
        var pB = new PacienteSnapshot { Id = idB, Nombre = "B", Edad = 40, SalaId = Guid.NewGuid() };

        // Crear en orden inverso para verificar que el mapa ordena por GUID
        var contexto = new ContextoDeCaso(new[] { pB, pA }, DateOnly.FromDateTime(DateTime.Today));

        Assert.Equal("P1", contexto.ObtenerCodigoPaciente(idA)); // idA < idB lexicográficamente
        Assert.Equal("P2", contexto.ObtenerCodigoPaciente(idB));
    }

    [Fact]
    public void ContextoDeCaso_PacienteNoEnContexto_RetornaNull()
    {
        var paciente = new PacienteSnapshot { Id = Guid.NewGuid(), Nombre = "A", Edad = 30, SalaId = Guid.NewGuid() };
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));

        Assert.Null(contexto.ObtenerCodigoPaciente(Guid.NewGuid()));
    }

    [Fact]
    public void ContextoDeCaso_ListaVacia_Lanza()
    {
        Assert.Throws<ArgumentException>(() => new ContextoDeCaso(Array.Empty<PacienteSnapshot>(), DateOnly.FromDateTime(DateTime.Today)));
    }

    [Fact]
    public void ContextoDeCaso_ToString_NoExponeIdsNiMapa()
    {
        var paciente = new PacienteSnapshot { Id = Guid.NewGuid(), Nombre = "Juan", Edad = 30, SalaId = Guid.NewGuid() };
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var toString = contexto.ToString();

        Assert.Contains("Pacientes = 1", toString);
        Assert.DoesNotContain(paciente.Id.ToString(), toString);
        Assert.DoesNotContain("Juan", toString);
    }

    [Fact]
    public void ContextoDeCaso_IdsYMapa_TienenJsonIgnore()
    {
        var paciente = new PacienteSnapshot { Id = Guid.NewGuid(), Nombre = "A", Edad = 30, SalaId = Guid.NewGuid() };
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today), valoracionId: Guid.NewGuid());

        var json = JsonSerializer.Serialize(contexto, new JsonSerializerOptions { WriteIndented = true });
        Assert.DoesNotContain("ValoracionId", json);
        Assert.DoesNotContain("MapaCodigosPaciente", json);
    }

    #endregion

    #region TransformadorDePaciente - Rangos de edad

    [Theory]
    [InlineData(0, "0-12")]
    [InlineData(12, "0-12")]
    [InlineData(13, "13-17")]
    [InlineData(17, "13-17")]
    [InlineData(18, "18-39")]
    [InlineData(39, "18-39")]
    [InlineData(40, "40-64")]
    [InlineData(64, "40-64")]
    [InlineData(65, "65+")]
    [InlineData(100, "65+")]
    public void TransformadorDePaciente_RangosEdad_Correctos(int edad, string rangoEsperado)
    {
        var paciente = CrearPacienteBasico(edad: edad);
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 10);

        Assert.Equal(rangoEsperado, resultado.RangoEdad);
    }

    [Fact]
    public void TransformadorDePaciente_EdadNegativa_Lanza()
    {
        var paciente = CrearPacienteBasico(edad: -1);
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        Assert.Throws<ArgumentException>(() => TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 10));
    }

    #endregion

    #region TransformadorDePaciente - Fechas relativas

    [Fact]
    public void TransformadorDePaciente_FechaHoy_ExpresionHoy()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var paciente = CrearPacienteBasico(fechaIngreso: hoy);
        var contexto = new ContextoDeCaso(new[] { paciente }, hoy);
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 10);

        var fechaIngreso = resultado.FechasRelativas.Single(f => f.Tipo == "ingreso");
        Assert.Equal("hoy", fechaIngreso.Expresion);
    }

    [Fact]
    public void TransformadorDePaciente_FechaPasada_ExpresionHaceNDias()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var hace3 = hoy.AddDays(-3);
        var paciente = CrearPacienteBasico(fechaIngreso: hace3);
        var contexto = new ContextoDeCaso(new[] { paciente }, hoy);
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 10);

        var fechaIngreso = resultado.FechasRelativas.Single(f => f.Tipo == "ingreso");
        Assert.Equal("hace 3 días", fechaIngreso.Expresion);
    }

    [Fact]
    public void TransformadorDePaciente_FechaFutura_ExpresionDentroDeNDias()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var dentro5 = hoy.AddDays(5);
        var paciente = CrearPacienteBasico(fechaAltaEstimada: dentro5);
        var contexto = new ContextoDeCaso(new[] { paciente }, hoy);
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 10);

        var fechaAlta = resultado.FechasRelativas.Single(f => f.Tipo == "altaEstimada");
        Assert.Equal("dentro de 5 días", fechaAlta.Expresion);
    }

    [Fact]
    public void TransformadorDePaciente_SinFechas_ListaVacia()
    {
        var paciente = CrearPacienteBasico();
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 10);

        Assert.Empty(resultado.FechasRelativas);
    }

    #endregion

    #region TransformadorDePaciente - Consentimiento y privacidad

    [Fact]
    public void TransformadorDePaciente_ConsentimientoSi_ValorPasaSinAdvertencia()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.Si,
            dietas: new Dictionary<string, string> { { "Diabetes", "terapeutica" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 1); // count=1 < K

        var dieta = resultado.Dietas.Single();
        Assert.Equal("terapeutica:Diabetes", dieta.ValorFinal);
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public void TransformadorDePaciente_ConsentimientoNoRegistrado_TratadoComoNo()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.NoRegistrado,
            dietas: new Dictionary<string, string> { { "Diabetes", "terapeutica" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 1); // count=1 < K

        var dieta = resultado.Dietas.Single();
        Assert.Equal("dato clínico no detallado por privacidad", dieta.ValorFinal);
        Assert.Contains("Se generalizó información clínica poco frecuente por privacidad.", resultado.Advertencias);
    }

    [Fact]
    public void TransformadorDePaciente_ConsentimientoNo_GeneralizaSiCountMenorK()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.No,
            dietas: new Dictionary<string, string> { { "Diabetes", "terapeutica" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 4); // count=4 < K=5

        var dieta = resultado.Dietas.Single();
        Assert.Equal("dato clínico no detallado por privacidad", dieta.ValorFinal);
        Assert.Contains("Se generalizó información clínica poco frecuente por privacidad.", resultado.Advertencias);
    }

    [Fact]
    public void TransformadorDePaciente_SinConsentimiento_CountIgualK_NoGeneraliza()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.No,
            dietas: new Dictionary<string, string> { { "Diabetes", "terapeutica" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 5); // count=5 == K

        var dieta = resultado.Dietas.Single();
        Assert.Equal("terapeutica:Diabetes", dieta.ValorFinal);
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public void TransformadorDePaciente_SinConsentimiento_CountMayorK_NoGeneraliza()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.No,
            dietas: new Dictionary<string, string> { { "Diabetes", "terapeutica" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 10); // count=10 > K

        var dieta = resultado.Dietas.Single();
        Assert.Equal("terapeutica:Diabetes", dieta.ValorFinal);
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public void TransformadorDePaciente_KMinimoNoDefault_Funciona()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.No,
            dietas: new Dictionary<string, string> { { "Diabetes", "terapeutica" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 3, (_, _, _) => 2); // K=3, count=2 < 3

        var dieta = resultado.Dietas.Single();
        Assert.Equal("dato clínico no detallado por privacidad", dieta.ValorFinal);
    }

    [Fact]
    public void TransformadorDePaciente_FrecuenciaAusente_EquivaleACero()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.No,
            dietas: new Dictionary<string, string> { { "Rara", "terapeutica" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 0); // count=0

        var dieta = resultado.Dietas.Single();
        Assert.Equal("dato clínico no detallado por privacidad", dieta.ValorFinal);
    }

    [Fact]
    public void TransformadorDePaciente_ConteoNegativo_Lanza()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.No,
            dietas: new Dictionary<string, string> { { "Diabetes", "terapeutica" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        Assert.Throws<ArgumentException>(() => TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => -1));
    }

    [Fact]
    public void TransformadorDePaciente_KMinimoMenorQue2_Lanza()
    {
        var paciente = CrearPacienteBasico();
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        Assert.Throws<ArgumentException>(() => TransformadorDePaciente.Transformar(paciente, contexto, 1, (_, _, _) => 10));
    }

    [Fact]
    public void TransformadorDePaciente_AdvertenciaGenerica_NoIncluyeValorNiNombreNiIdNiTipo()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.No,
            dietas: new Dictionary<string, string> { { "DiabetesMellitus", "terapeutica" } },
            alergenos: new Dictionary<string, string> { { "Penicilina", "farmacologico" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 1);

        // Solo una advertencia única porque Distinct() elimina duplicados
        Assert.Single(resultado.Advertencias);
        var adv = resultado.Advertencias[0];
        Assert.Equal("Se generalizó información clínica poco frecuente por privacidad.", adv);
        Assert.DoesNotContain("Diabetes", adv);
        Assert.DoesNotContain("Penicilina", adv);
        Assert.DoesNotContain("terapeutica", adv);
        Assert.DoesNotContain("farmacologico", adv);
        Assert.DoesNotContain(paciente.Id.ToString(), adv);
        Assert.DoesNotContain(paciente.Nombre, adv);
    }

    [Fact]
    public void TransformadorDePaciente_MarcadorNuncaSignificaAusencia_ValorFinalSiemprePresente()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.No,
            dietas: new Dictionary<string, string> { { "Rara", "terapeutica" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 1);

        var dieta = resultado.Dietas.Single();
        Assert.Equal("dato clínico no detallado por privacidad", dieta.ValorFinal);
        Assert.NotEmpty(dieta.ValorFinal);
    }

    [Fact]
    public void TransformadorDePaciente_DietaYAlergenoRaros_AmbosGeneralizadosConAdvertenciasIdeticas()
    {
        var paciente = CrearPacienteBasico(
            consentimiento: EstadoConsentimientoIA.No,
            dietas: new Dictionary<string, string> { { "DietaRara", "terapeutica" } },
            alergenos: new Dictionary<string, string> { { "AlergenoRaro", "alimentario" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 1);

        Assert.Equal("dato clínico no detallado por privacidad", resultado.Dietas.Single().ValorFinal);
        Assert.Equal("dato clínico no detallado por privacidad", resultado.Alergenos.Single().ValorFinal);
        // Solo una advertencia única porque Distinct() elimina duplicados
        Assert.Single(resultado.Advertencias);
        Assert.Equal("Se generalizó información clínica poco frecuente por privacidad.", resultado.Advertencias[0]);
    }

    #endregion

    #region TransformadorDePaciente - Múltiples pacientes y estabilidad P1/P2

    [Fact]
    public void TransformadorDePaciente_DosPacientes_CodigosConsistentesEntreProveedores()
    {
        var id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var p1 = CrearPacienteBasico(id: id1, nombre: "Paciente1", edad: 30);
        var p2 = CrearPacienteBasico(id: id2, nombre: "Paciente2", edad: 40);

        var contexto = new ContextoDeCaso(new[] { p1, p2 }, DateOnly.FromDateTime(DateTime.Today));

        var r1 = TransformadorDePaciente.Transformar(p1, contexto, 5, (_, _, _) => 10);
        var r2 = TransformadorDePaciente.Transformar(p2, contexto, 5, (_, _, _) => 10);

        Assert.Equal("P1", r1.Referencia);
        Assert.Equal("P2", r2.Referencia);
    }

    [Fact]
    public void TransformadorDePaciente_UnPaciente_ReferenciaEsElPaciente()
    {
        var paciente = CrearPacienteBasico(nombre: "Juan");
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 10);

        Assert.Equal("el paciente", resultado.Referencia);
    }

    #endregion

    #region TransformadorDePaciente - Serialización y ausencia de datos sensibles

    [Fact]
    public void TransformadorDePaciente_ResultadoSerializado_NoContieneNombreNiGuidNiSalaNiTextoLibre()
    {
        var paciente = CrearPacienteBasico(
            nombre: "Juan Pérez",
            salaId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            dietas: new Dictionary<string, string> { { "Diabetes", "terapeutica" } },
            alergenos: new Dictionary<string, string> { { "Penicilina", "farmacologico" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));
        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 10);

        var json = JsonSerializer.Serialize(resultado, new JsonSerializerOptions { WriteIndented = true });
        var toString = resultado.ToString();

        // Verificar JSON
        Assert.DoesNotContain("Juan Pérez", json);
        Assert.DoesNotContain("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", json);
        Assert.DoesNotContain("sala", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("30", json); // edad exacta no debe aparecer (solo rango)
        Assert.DoesNotContain("texto libre", json, StringComparison.OrdinalIgnoreCase);

        // Verificar ToString
        Assert.DoesNotContain("Juan Pérez", toString);
        Assert.DoesNotContain("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", toString);
    }

    [Fact]
    public void BloqueDeContexto_Serializacion_CifrasYToString()
    {
        var cifras = new Dictionary<string, double> { { "calorias", 2000.5 }, { "proteinas", 80.0 } };
        var bloque = new BloqueDeContexto("valoracion", 1, "Resumen", cifras);

        var json = JsonSerializer.Serialize(bloque, new JsonSerializerOptions { WriteIndented = true });
        Assert.Contains("calorias", json);
        Assert.Contains("2000.5", json);
        Assert.Contains("proteinas", json);

        var toString = bloque.ToString();
        Assert.Contains("[redacted]", toString);
        // ToString contiene "Resumen = [redacted]" - el nombre de propiedad sí aparece, pero no el valor
        Assert.DoesNotContain("Resumen = Resumen", toString); // El valor "Resumen" no debe aparecer
        Assert.Contains("Resumen = [redacted]", toString); // El formato es "Resumen = [redacted]"
    }

    [Fact]
    public void ResultadoDeContexto_Serializacion_Y_ToString()
    {
        var bloque = new BloqueDeContexto("valoracion", 1, "Resumen", new Dictionary<string, double> { { "c", 1.0 } });
        var resultado = new ResultadoDeContexto(bloque, new[] { "Adv1" });

        var json = JsonSerializer.Serialize(resultado, new JsonSerializerOptions { WriteIndented = true });
        Assert.Contains("Bloque", json);
        Assert.Contains("Advertencias", json);

        var toString = resultado.ToString();
        Assert.Contains("Bloque = valoracion", toString);
        Assert.Contains("Advertencias = 1", toString);
    }

    #endregion

    #region AddIA - Registro por reflexión e idempotencia

    [Fact]
    public void AddIA_RegistraProveedoresPorReflexion()
    {
        var services = new ServiceCollection();
        services.AddIA(typeof(ContratosIA324Tests).Assembly);

        var proveedores = services.Where(d => d.ServiceType == typeof(IProveedorDeContexto)).ToArray();
        Assert.Contains(proveedores, d => d.ImplementationType == typeof(ProveedorFalso1));
        Assert.Contains(proveedores, d => d.ImplementationType == typeof(ProveedorFalso2));
    }

    [Fact]
    public void AddIA_Idempotente_DosLlamadasNoDuplican()
    {
        var services = new ServiceCollection();
        services.AddIA(typeof(ContratosIA324Tests).Assembly);
        services.AddIA(typeof(ContratosIA324Tests).Assembly);

        var proveedores = services.Where(d => d.ServiceType == typeof(IProveedorDeContexto)).ToArray();
        Assert.Equal(2, proveedores.Length); // Solo los dos proveedores falsos, sin duplicados
    }

    [Fact]
    public void AddApplication_DosLlamadas_NoDuplicaProveedoresDeContexto()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        var tras1 = services.Count(d => d.ServiceType == typeof(IProveedorDeContexto));
        services.AddApplication();
        var tras2 = services.Count(d => d.ServiceType == typeof(IProveedorDeContexto));

        Assert.Equal(tras1, tras2);
        // Los proveedores falsos están en el ensamblado de tests: AddApplication solo escanea Application.
        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(ProveedorFalso1));
    }

    [Fact]
    public void AddIA_RegistraPorTipo_SinInstanciarProveedores()
    {
        // Registrar no debe instanciar proveedores (ni validar unicidad de Fuente): solo tipos, con ciclo Scoped.
        var services = new ServiceCollection();
        services.AddIA(typeof(ContratosIA324Tests).Assembly);

        var proveedores = services.Where(d => d.ServiceType == typeof(IProveedorDeContexto)).ToArray();
        Assert.All(proveedores, d =>
        {
            Assert.NotNull(d.ImplementationType);
            Assert.Null(d.ImplementationInstance);
            Assert.Null(d.ImplementationFactory);
            Assert.Equal(ServiceLifetime.Scoped, d.Lifetime);
        });
    }

    #endregion

    #region Ramas de cobertura (#324)

    [Fact]
    public void BloqueDeContexto_ClaveDeCifraNula_Lanza()
    {
        Assert.Throws<ArgumentException>(() => new BloqueDeContexto("fuente", 1, "Resumen", new CifrasConClaveNula()));
    }

    [Fact]
    public void ContextoDeCaso_IdentificadoresOpcionales_SeConservanInternamente()
    {
        var paciente = CrearPacienteBasico();
        var (val, pac, menu, dieta) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today),
            valoracionId: val, pacienteId: pac, menuId: menu, dietaId: dieta);

        Assert.Equal(val, contexto.ValoracionId);
        Assert.Equal(pac, contexto.PacienteId);
        Assert.Equal(menu, contexto.MenuId);
        Assert.Equal(dieta, contexto.DietaId);
    }

    [Fact]
    public void ContextoDeCaso_PacientesNulos_Lanza()
    {
        Assert.Throws<ArgumentException>(() => new ContextoDeCaso(null!, DateOnly.FromDateTime(DateTime.Today)));
    }

    [Fact]
    public void TransformadorDePaciente_PacienteAjenoAlContexto_UsaCodigoDesconocido()
    {
        var enContexto = CrearPacienteBasico();
        var ajeno = CrearPacienteBasico();
        var contexto = new ContextoDeCaso(new[] { enContexto }, DateOnly.FromDateTime(DateTime.Today));

        var resultado = TransformadorDePaciente.Transformar(ajeno, contexto, 5, (_, _, _) => 10);

        Assert.Equal("desconocido", resultado.Referencia);
    }

    [Fact]
    public void TransformadorDePaciente_ConteoNegativoEnAlergeno_Lanza()
    {
        var paciente = CrearPacienteBasico(alergenos: new Dictionary<string, string> { { "Gluten", "alergia" } });
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));

        Assert.Throws<ArgumentException>(() =>
            TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, tipo, _) => tipo == "Alergeno" ? -1 : 10));
    }

    #endregion

    #region Privacidad - el valor generalizado no se puede recuperar (revisión de #324)

    private const string NombrePaciente = "CentinelaNombrePaciente";
    private const string DietaRara = "CentinelaDietaRarisima";
    private const string AlergenoRaro = "CentinelaAlergenoRaro";
    private const string TipoClinicoRaro = "centinela-tipo-clinico";

    private static PacienteSnapshot PacienteConDatosRaros(Guid id, Guid sala, EstadoConsentimientoIA consentimiento)
        => CrearPacienteBasico(
            id: id,
            nombre: NombrePaciente,
            edad: 47,
            salaId: sala,
            consentimiento: consentimiento,
            dietas: new Dictionary<string, string> { { DietaRara, TipoClinicoRaro } },
            alergenos: new Dictionary<string, string> { { AlergenoRaro, TipoClinicoRaro } });

    private static void AssertSinCentinelas(string texto, params string[] centinelas)
    {
        foreach (var centinela in centinelas)
        {
            Assert.DoesNotContain(centinela, texto, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TransformadorDePaciente_DatoGeneralizado_NoConservaValorOriginalEnObjetoJsonNiToString()
    {
        var id = Guid.NewGuid();
        var sala = Guid.NewGuid();
        var paciente = PacienteConDatosRaros(id, sala, EstadoConsentimientoIA.No);
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));

        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 1);

        // Ambos datos se generalizaron...
        Assert.Equal("dato clínico no detallado por privacidad", Assert.Single(resultado.Dietas).ValorFinal);
        Assert.Equal("dato clínico no detallado por privacidad", Assert.Single(resultado.Alergenos).ValorFinal);

        // ...y el valor real no sale por ninguna vía: JSON, ToString (del resultado y de cada dato) ni reflexión.
        var json = JsonSerializer.Serialize(resultado);
        var textos = new[] { json, resultado.ToString(), resultado.Dietas[0].ToString(), resultado.Alergenos[0].ToString() };
        foreach (var texto in textos)
        {
            AssertSinCentinelas(texto, DietaRara, AlergenoRaro, TipoClinicoRaro, NombrePaciente, id.ToString(), sala.ToString(), "47");
        }

        var propiedades = typeof(DatoClinicoTransformado).GetProperties().Select(p => p.Name).Order().ToArray();
        Assert.Equal(new[] { nameof(DatoClinicoTransformado.Tipo), nameof(DatoClinicoTransformado.ValorFinal) }, propiedades);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void TransformadorDePaciente_ConteoBajo_NoSeFiltraEnLaSalida(int conteo)
    {
        var paciente = PacienteConDatosRaros(Guid.NewGuid(), Guid.NewGuid(), EstadoConsentimientoIA.NoRegistrado);
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));

        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => conteo);

        var json = JsonSerializer.Serialize(resultado);
        Assert.DoesNotContain("Conteo", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"" + conteo + "\"", json);
        Assert.DoesNotContain(":" + conteo + ",", json);
        Assert.DoesNotContain(":" + conteo + "}", json);
    }

    [Fact]
    public void TransformadorDePaciente_DatoQuePasa_ConservaSoloTipoYValor()
    {
        var paciente = PacienteConDatosRaros(Guid.NewGuid(), Guid.NewGuid(), EstadoConsentimientoIA.Si);
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));

        var resultado = TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, _, _) => 1);

        var dieta = Assert.Single(resultado.Dietas);
        Assert.Equal("Dieta", dieta.Tipo);
        Assert.Equal($"{TipoClinicoRaro}:{DietaRara}", dieta.ValorFinal);
        var alergeno = Assert.Single(resultado.Alergenos);
        Assert.Equal("Alergeno", alergeno.Tipo);
        Assert.Equal($"{TipoClinicoRaro}:{AlergenoRaro}", alergeno.ValorFinal);
    }

    [Fact]
    public void ContextoDeCaso_Serializado_NoContienePacientesNiSusDatos()
    {
        var id = Guid.NewGuid();
        var sala = Guid.NewGuid();
        var paciente = PacienteConDatosRaros(id, sala, EstadoConsentimientoIA.No);
        var contexto = new ContextoDeCaso(
            new[] { paciente },
            DateOnly.FromDateTime(DateTime.Today),
            valoracionId: Guid.NewGuid(),
            pacienteId: id,
            menuId: Guid.NewGuid(),
            dietaId: Guid.NewGuid());

        var json = JsonSerializer.Serialize(contexto);

        AssertSinCentinelas(json, NombrePaciente, DietaRara, AlergenoRaro, TipoClinicoRaro, id.ToString(), sala.ToString(), "Pacientes", "\"Edad\"");
        AssertSinCentinelas(contexto.ToString(), NombrePaciente, DietaRara, id.ToString(), sala.ToString());
        Assert.Contains("FechaReferencia", json);
    }

    [Fact]
    public void ContextoDeCaso_Pacientes_NoEsPublico()
    {
        var propiedad = typeof(ContextoDeCaso).GetProperty(nameof(ContextoDeCaso.Pacientes), BindingFlags.Public | BindingFlags.Instance);

        Assert.Null(propiedad);
    }

    [Fact]
    public void PacienteSnapshot_ToString_NoExponeDatosDelPaciente()
    {
        var id = Guid.NewGuid();
        var sala = Guid.NewGuid();
        var paciente = PacienteConDatosRaros(id, sala, EstadoConsentimientoIA.Si) with
        {
            FechaIngreso = new DateOnly(2026, 3, 14),
        };

        var texto = paciente.ToString();

        AssertSinCentinelas(texto, NombrePaciente, DietaRara, AlergenoRaro, TipoClinicoRaro, id.ToString(), sala.ToString(), "47", "2026");
        Assert.Contains("Dietas = 1", texto);
        Assert.Contains("Alergenos = 1", texto);
    }

    [Theory]
    [InlineData("Dieta")]
    [InlineData("Alergeno")]
    public void TransformadorDePaciente_ConteoNegativo_MensajeDeErrorNoIncluyeElValorClinico(string tipo)
    {
        var paciente = PacienteConDatosRaros(Guid.NewGuid(), Guid.NewGuid(), EstadoConsentimientoIA.No);
        var contexto = new ContextoDeCaso(new[] { paciente }, DateOnly.FromDateTime(DateTime.Today));

        var ex = Assert.Throws<ArgumentException>(() =>
            TransformadorDePaciente.Transformar(paciente, contexto, 5, (_, t, _) => t == tipo ? -1 : 10));

        AssertSinCentinelas(ex.Message, DietaRara, AlergenoRaro, TipoClinicoRaro);
        Assert.Contains(tipo, ex.Message);
    }

    #endregion

    #region Helpers

    private static PacienteSnapshot CrearPacienteBasico(
        Guid? id = null,
        string nombre = "Paciente",
        int edad = 30,
        Guid? salaId = null,
        DateOnly? fechaIngreso = null,
        DateOnly? fechaAltaEstimada = null,
        EstadoConsentimientoIA consentimiento = EstadoConsentimientoIA.NoRegistrado,
        Dictionary<string, string>? dietas = null,
        Dictionary<string, string>? alergenos = null)
    {
        return new PacienteSnapshot
        {
            Id = id ?? Guid.NewGuid(),
            Nombre = nombre,
            Edad = edad,
            SalaId = salaId ?? Guid.NewGuid(),
            FechaIngreso = fechaIngreso,
            FechaAltaEstimada = fechaAltaEstimada,
            ConsentimientoIA = consentimiento,
            Dietas = dietas?.ToFrozenDictionary() ?? FrozenDictionary<string, string>.Empty,
            Alergenos = alergenos?.ToFrozenDictionary() ?? FrozenDictionary<string, string>.Empty
        };
    }

    // Diccionario de solo lectura que enumera una clave nula, imposible con un Dictionary real.
    private sealed class CifrasConClaveNula : IReadOnlyDictionary<string, double>
    {
        public double this[string key] => throw new KeyNotFoundException();
        public IEnumerable<string> Keys => new string[] { null! };
        public IEnumerable<double> Values => new[] { 1.0 };
        public int Count => 1;
        public bool ContainsKey(string key) => false;
        public bool TryGetValue(string key, out double value) { value = 0; return false; }
        public IEnumerator<KeyValuePair<string, double>> GetEnumerator()
        {
            yield return new KeyValuePair<string, double>(null!, 1.0);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // Proveedores falsos para pruebas de registro
    private sealed class ProveedorFalso1 : IProveedorDeContexto
    {
        public string Fuente => "falso1";
        public Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default)
            => Task.FromResult<ResultadoDeContexto?>(null);
    }

    private sealed class ProveedorFalso2 : IProveedorDeContexto
    {
        public string Fuente => "falso2";
        public Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default)
            => Task.FromResult<ResultadoDeContexto?>(null);
    }

    #endregion
}
