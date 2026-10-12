using Menus.Domain.Entities;
using Xunit;

namespace Menus.Domain.Tests.Nomencladores;

public class NomencladoresEdgeCasesTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void GrupoNutricional_NombreSoloEspacios_LanzaExcepcion(string nombreInvalido)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GrupoNutricional(Guid.NewGuid(), nombreInvalido));
    }

    [Fact]
    public void Especialidad_NombreConEspaciosAlRededor_SeGuardaTrimmed()
    {
        // Arrange
        const string nombreConEspacios = "   Nutrición Clínica   ";

        // Act
        var especialidad = new Especialidad(Guid.NewGuid(), nombreConEspacios);

        // Assert
        Assert.Equal("Nutrición Clínica", especialidad.Nombre);
    }

    [Fact]
    public void Sala_NombreConEspaciosAlRededor_SeGuardaTrimmed()
    {
        // Arrange
        const string nombreConEspacios = "   Sala A - Pediatría   ";

        // Act
        var sala = new Sala(Guid.NewGuid(), nombreConEspacios);

        // Assert
        Assert.Equal("Sala A - Pediatría", sala.Nombre);
    }
}dotnet test --collect:"XPlat Code Coverage"
 reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
 Restore complete (2.2s)
   Menus.Domain net10.0 succeeded (4.6s) → src/Menus.Domain/bin/Debug/net10.0/Menus.Domain.dll
   Menus.Domain.Tests net10.0 failed with 8 error(s) (0.5s)
     /home/fontes49/Projects/UH/Menus-Nutricionales/tests/Menus.Domain.Tests/Abstractions/EntityTests.cs(2,20): error CS0234: The type or namespace name 'Entities' does not exist in the namespace 'Menus.Domain' (are you missing an assembly reference?)
     /home/fontes49/Projects/UH/Menus-Nutricionales/tests/Menus.Domain.Tests/Enums/DomainEnumsTests.cs(1,20): error CS0234: The type or namespace name 'Enums' does not exist in the namespace 'Menus.Domain' (are you missing an assembly reference?)
     /home/fontes49/Projects/UH/Menus-Nutricionales/tests/Menus.Domain.Tests/Invariants/DietaInvariantsTests.cs(1,20): error CS0234: The type or namespace name 'Entities' does not exist in the namespace 'Menus.Domain' (are you missing an assembly reference?)
     /home/fontes49/Projects/UH/Menus-Nutricionales/tests/Menus.Domain.Tests/Invariants/MenuInvariantsTests.cs(1,20): error CS0234: The type or namespace name 'Entities' does not exist in the namespace 'Menus.Domain' (are you missing an assembly reference?)
     /home/fontes49/Projects/UH/Menus-Nutricionales/tests/Menus.Domain.Tests/Invariants/PacienteInvariantsTests.cs(1,20): error CS0234: The type or namespace name 'Entities' does not exist in the namespace 'Menus.Domain' (are you missing an assembly reference?)
     /home/fontes49/Projects/UH/Menus-Nutricionales/tests/Menus.Domain.Tests/Nomencladores/NomencladoresEdgeCasesTests.cs(1,20): error CS0234: The type or namespace name 'Entities' does not exist in the namespace 'Menus.Domain' (are you missing an assembly reference?)
     /home/fontes49/Projects/UH/Menus-Nutricionales/tests/Menus.Domain.Tests/Abstractions/EntityTests.cs(9,34): error CS0246: The type or namespace name 'Entity<>' could not be found (are you missing a using directive or an assembly reference?)
     /home/fontes49/Projects/UH/Menus-Nutricionales/tests/Menus.Domain.Tests/Abstractions/EntityTests.cs(14,34): error CS0246: The type or namespace name 'Entity<>' could not be found (are you missing a using directive or an assembly reference?)
   Menus.Application net10.0 succeeded (0.5s) → src/Menus.Application/bin/Debug/net10.0/Menus.Application.dll
   Menus.Application.Tests net10.0 succeeded (0.8s) → tests/Menus.Application.Tests/bin/Debug/net10.0/Menus.Application.Tests.dll
 [xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.11)
 [xUnit.net 00:00:00.13]   Discovering: Menus.Application.Tests
 [xUnit.net 00:00:00.19]   Discovered:  Menus.Application.Tests
 [xUnit.net 00:00:00.23]   Starting:    Menus.Application.Tests
 [xUnit.net 00:00:00.33]   Finished:    Menus.Application.Tests
   Menus.Application.Tests test net10.0 succeeded (2.2s)
   Menus.Client net10.0 browser-wasm succeeded (8.4s) → src/Menus.Client/bin/Debug/net10.0/wwwroot
   Menus.Client.Tests net10.0 succeeded (0.7s) → tests/Menus.Client.Tests/bin/Debug/net10.0/Menus.Client.Tests.dll
 [xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.11)
 [xUnit.net 00:00:00.11]   Discovering: Menus.Client.Tests
 [xUnit.net 00:00:00.17]   Discovered:  Menus.Client.Tests
 [xUnit.net 00:00:00.20]   Starting:    Menus.Client.Tests
 [xUnit.net 00:00:00.30]   Finished:    Menus.Client.Tests
   Menus.Client.Tests test net10.0 succeeded (2.1s)
 
 Test summary: total: 2, failed: 0, succeeded: 2, skipped: 0, duration: 5.3s
 Build failed with 8 error(s) in 14.0s
 
 Attac grep -rn "namespace " src/Menus.Domain/
      src/Menus.Domain/Abstractions/IRepository.cs:1:namespace Menus.Domain.Abstractions;
      src/Menus.Domain/Abstractions/IUnitOfWork.cs:1:namespace Menus.Domain.Abstractions;
      ➜ Menus-Nutricionales ⚡( tests/entidades)
      ▶ grep -rn "class Entity" src/Menus.Domain/
      ➜ Menus-Nutricionales ⚡( tests/entidades) 
      ▶ grep -rn "class Entity" src/Menus.Domain/
      ➜ Menus-Nutricionales ⚡( tests/entidades) 
      ▶ find src/Menus.Domain -type f
      git status
      git fetch origin
      git log --oneline -5 origin/main
      src/Menus.Domain/Abstractions/IRepository.cs
      src/Menus.Domain/Abstractions/IUnitOfWork.cs
      src/Menus.Domain/Menus.Domain.csproj
      src/Menus.Domain/obj/Menus.Domain.csproj.nuget.g.props
      src/Menus.Domain/obj/Menus.Domain.csproj.nuget.g.targets
      src/Menus.Domain/obj/Debug/net10.0/refint/Menus.Domain.dll
      src/Menus.Domain/obj/Debug/net10.0/ref/Menus.Domain.dll
      src/Menus.Domain/obj/Debug/net10.0/Menus.Domain.GlobalUsings.g.cs
      src/Menus.Domain/obj/Debug/net10.0/Menus.Domain.assets.cache
      src/Menus.Domain/obj/Debug/net10.0/Menus.Domain.GeneratedMSBuildEditorConfig.editorconfig
      src/Menus.Domain/obj/Debug/net10.0/.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
      src/Menus.Domain/obj/Debug/net10.0/Menus.Domain.AssemblyInfoInputs.cache
      src/Menus.Domain/obj/Debug/net10.0/Menus.Domain.AssemblyInfo.cs
      src/Menus.Domain/obj/Debug/net10.0/Menus.Domain.csproj.CoreCompileInputs.cache
      src/Menus.Domain/obj/Debug/net10.0/Menus.Domain.sourcelink.json
      src/Menus.Domain/obj/Debug/net10.0/Menus.Domain.dll
      src/Menus.Domain/obj/Debug/net10.0/Menus.Domain.pdb
      src/Menus.Domain/obj/Debug/net10.0/Menus.Domain.csproj.FileListAbsolute.txt
      src/Menus.Domain/obj/project.packagespec.json
      src/Menus.Domain/obj/rider.project.model.nuget.info
      src/Menus.Domain/obj/project.assets.json
      src/Menus.Domain/obj/rider.project.restore.info
      src/Menus.Domain/obj/project.nuget.cache
      src/Menus.Domain/obj/Menus.Domain.csproj.nuget.dgspec.json
      src/Menus.Domain/bin/Debug/net10.0/Menus.Domain.deps.json
      src/Menus.Domain/bin/Debug/net10.0/Menus.Domain.dll
      src/Menus.Domain/bin/Debug/net10.0/Menus.Domain.pdb
      On branch tests/entidades
      Changes to be committed:
        (use "git restore --staged <file>..." to unstage)
              new file:   tests/Menus.Domain.Tests/Abstractions/EntityTests.cs
              new file:   tests/Menus.Domain.Tests/Enums/DomainEnumsTests.cs
              new file:   tests/Menus.Domain.Tests/Invariants/DietaInvariantsTests.cs
              new file:   tests/Menus.Domain.Tests/Invariants/MenuInvariantsTests.cs
              new file:   tests/Menus.Domain.Tests/Invariants/PacienteInvariantsTests.cs
              new file:   tests/Menus.Domain.Tests/Nomencladores/NomencladoresEdgeCasesTests.cs
      
      Changes not staged for commit:
        (use "git add <file>..." to update what will be committed)
        (use "git restore <file>..." to discard changes in working directory)
              modified:   tests/Menus.Domain.Tests/Abstractions/EntityTests.cs
              modified:   tests/Menus.Domain.Tests/Enums/DomainEnumsTests.cs
              modified:   tests/Menus.Domain.Tests/Invariants/DietaInvariantsTests.cs
              modified:   tests/Menus.Domain.Tests/Invariants/MenuInvariantsTests.cs
              modified:   tests/Menus.Domain.Tests/Invariants/PacienteInvariantsTests.cs
              modified:   tests/Menus.Domain.Tests/Nomencladores/NomencladoresEdgeCasesTests.cs
      
      remote: Enumerating objects: 32, done.
      remote: Counting objects: 100% (32/32), done.
      remote: Compressing objects: 100% (12/12), done.
      remote: Total 32 (delta 20), reused 32 (delta 20), pack-reused 0 (from 0)
      Unpacking objects: 100% (32/32), 6.88 KiB | 352.00 KiB/s, done.
      From https://github.com/Team-Bales/Menus-Nutricionales
       * [new branch]      feat/52-entidades-nutricionista-sala -> origin/feat/52-entidades-nutricionista-sala
         24e923b..5f48ea6  feat/issue-54-entidad-menu           -> origin/feat/issue-54-entidad-menu
         8033d3a..06aff17  feat/issue_51_entidad_dieta          -> origin/feat/issue_51_entidad_dieta
      fdd33f9 (origin/main, origin/HEAD) Merge pull request #125 from Team-Bales/feat/50-entidades-alimento-plato
      d6592d6 Merge pull request #124 from Team-Bales/docs/glosario-sync-informe
      a4cd7e3 docs: sincronizar glosario con el informe
      56e5bc7 Merge pull request #116 from Team-Bales/dependabot/nuget/tests/Menus.Application.Tests/multi-2a09a887a9
      8f36ff3 docs(githuhments:
   /home/fontes49/Projects/UH/Menus-Nutricionales/tests/Menus.Application.Tests/TestResults/70a2c0ba-d1b3-4386-ab7e-b1416307da40/coverage.cobertura.xml                                                            
   /home/fontes49/Projects/UH/Menus-Nutricionales/tests/Menus.Client.Tests/TestResults/f6dc40d4-97f6-4372-9081-28a24257b3c8/coverage.cobertura.xml                                                                 
 bash: command not found: reportgenerator                                                                 
 ➜ Menus-Nutricionales ⚡( tests/entidades) 
 ▶ 