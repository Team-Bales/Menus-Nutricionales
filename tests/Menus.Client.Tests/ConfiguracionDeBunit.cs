using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Bunit;

namespace Menus.Client.Tests;

/// <summary>Configuración de bUnit para todas las pruebas del cliente (#366).</summary>
internal static class ConfiguracionDeBunit
{
    /// <summary>
    /// Sube a 5 segundos el límite de <c>WaitForAssertion</c> y <c>WaitForElement</c>; el de bUnit es 1 segundo.
    /// </summary>
    /// <remarks>
    /// Con el runner del CI ocupado, o con varios procesos de pruebas a la vez, 1 segundo no alcanza y la espera
    /// falla con <c>WaitForFailedException</c> aunque la página esté bien (pasó con <c>SalasTests</c> y
    /// <c>CatalogoDietasTests</c>). Las esperas terminan apenas se cumple la condición, así que las pruebas que pasan
    /// no se hacen más lentas: solo tarda más en informar una que de verdad falla.
    /// </remarks>
    [ModuleInitializer]
    [SuppressMessage(
        "Usage",
        "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Es un proyecto de pruebas: el inicializador configura bUnit para todas sus clases.")]
    internal static void Inicializar() => BunitContext.DefaultWaitTimeout = TimeSpan.FromSeconds(5);
}
