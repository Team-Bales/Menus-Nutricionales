using System;
using Menus.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Menus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "especialidades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_especialidades", x => x.id);
                    table.CheckConstraint("ck_especialidades_r6_15_nombre", "btrim(nombre) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "grupos_nutricionales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupos_nutricionales", x => x.id);
                    table.CheckConstraint("ck_grupos_nutricionales_r5_11_nombre", "btrim(nombre) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "niveles_caloricos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niveles_caloricos", x => x.id);
                    table.CheckConstraint("ck_niveles_caloricos_r5_09_nombre", "nombre IN ('Bajo', 'Medio', 'Alto')");
                });

            migrationBuilder.CreateTable(
                name: "programas_atencion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_programas_atencion", x => x.id);
                    table.CheckConstraint("ck_programas_atencion_r6_15_nombre", "btrim(nombre) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "restricciones_alimentarias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restricciones_alimentarias", x => x.id);
                    table.CheckConstraint("ck_restricciones_alimentarias_r6_15_nombre", "btrim(nombre) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "resultados_nutricionales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    valor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resultados_nutricionales", x => x.id);
                    table.CheckConstraint("ck_resultados_nutricionales_r7_19_nombre", "nombre IN ('Muy mala', 'Mala', 'Normal', 'Buena', 'Excelente')");
                    table.CheckConstraint("ck_resultados_nutricionales_r7_19_valor", "valor BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "salas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salas", x => x.id);
                    table.CheckConstraint("ck_salas_r6_15_nombre", "btrim(nombre) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "tipos_preparacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_preparacion", x => x.id);
                    table.CheckConstraint("ck_tipos_preparacion_r5_08_nombre", "nombre IN ('Entrante', 'Plato fuerte', 'Postre', 'Bebida')");
                });

            migrationBuilder.CreateTable(
                name: "nutricionistas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    especialidad_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutricionistas", x => x.id);
                    table.CheckConstraint("ck_nutricionistas_r6_15_nombre", "btrim(nombre) <> ''");
                    table.ForeignKey(
                        name: "FK_nutricionistas_especialidades_especialidad_id",
                        column: x => x.especialidad_id,
                        principalTable: "especialidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dietas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    programa_atencion_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dietas", x => x.id);
                    table.CheckConstraint("ck_dietas_r6_15_nombre", "btrim(nombre) <> ''");
                    table.ForeignKey(
                        name: "FK_dietas_programas_atencion_programa_atencion_id",
                        column: x => x.programa_atencion_id,
                        principalTable: "programas_atencion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "excluye",
                columns: table => new
                {
                    restriccion_alimentaria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_nutricional_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_excluye", x => new { x.restriccion_alimentaria_id, x.grupo_nutricional_id });
                    table.ForeignKey(
                        name: "FK_excluye_grupos_nutricionales_grupo_nutricional_id",
                        column: x => x.grupo_nutricional_id,
                        principalTable: "grupos_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_excluye_restricciones_alimentarias_restriccion_alimentaria_~",
                        column: x => x.restriccion_alimentaria_id,
                        principalTable: "restricciones_alimentarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pacientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    edad = table.Column<int>(type: "integer", nullable: false),
                    sala_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pacientes", x => x.id);
                    table.CheckConstraint("ck_pacientes_r6_14_edad", "edad >= 0");
                    table.CheckConstraint("ck_pacientes_r6_15_nombre", "btrim(nombre) <> ''");
                    table.ForeignKey(
                        name: "FK_pacientes_salas_sala_id",
                        column: x => x.sala_id,
                        principalTable: "salas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "items_alimentarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    grupo_nutricional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_preparacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nivel_calorico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clase = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items_alimentarios", x => x.id);
                    table.CheckConstraint("ck_items_alimentarios_r5_11_nombre", "btrim(nombre) <> ''");
                    table.CheckConstraint("ck_items_alimentarios_r5_19_clase", "clase IN ('Alimento', 'Plato')");
                    table.ForeignKey(
                        name: "FK_items_alimentarios_grupos_nutricionales_grupo_nutricional_id",
                        column: x => x.grupo_nutricional_id,
                        principalTable: "grupos_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_alimentarios_niveles_caloricos_nivel_calorico_id",
                        column: x => x.nivel_calorico_id,
                        principalTable: "niveles_caloricos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_alimentarios_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_alimentarios_tipos_preparacion_tipo_preparacion_id",
                        column: x => x.tipo_preparacion_id,
                        principalTable: "tipos_preparacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "autoriza",
                columns: table => new
                {
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dieta_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autoriza", x => new { x.nutricionista_id, x.dieta_id });
                    table.ForeignKey(
                        name: "FK_autoriza_dietas_dieta_id",
                        column: x => x.dieta_id,
                        principalTable: "dietas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autoriza_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cubre_dieta",
                columns: table => new
                {
                    dieta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_nutricional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cubre_dieta", x => new { x.dieta_id, x.grupo_nutricional_id });
                    table.CheckConstraint("ck_cubre_dieta_r6_17b_tipo", "tipo IN ('Requerido', 'Restringido')");
                    table.ForeignKey(
                        name: "FK_cubre_dieta_dietas_dieta_id",
                        column: x => x.dieta_id,
                        principalTable: "dietas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cubre_dieta_grupos_nutricionales_grupo_nutricional_id",
                        column: x => x.grupo_nutricional_id,
                        principalTable: "grupos_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "restringe",
                columns: table => new
                {
                    dieta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    restriccion_alimentaria_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restringe", x => new { x.dieta_id, x.restriccion_alimentaria_id });
                    table.ForeignKey(
                        name: "FK_restringe_dietas_dieta_id",
                        column: x => x.dieta_id,
                        principalTable: "dietas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_restringe_restricciones_alimentarias_restriccion_alimentari~",
                        column: x => x.restriccion_alimentaria_id,
                        principalTable: "restricciones_alimentarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "paciente_dieta",
                columns: table => new
                {
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dieta_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paciente_dieta", x => new { x.paciente_id, x.dieta_id });
                    table.ForeignKey(
                        name: "FK_paciente_dieta_dietas_dieta_id",
                        column: x => x.dieta_id,
                        principalTable: "dietas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_paciente_dieta_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "composicion",
                columns: table => new
                {
                    plato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alimento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_composicion", x => new { x.plato_id, x.alimento_id });
                    table.CheckConstraint("ck_composicion_r5_12_cantidad", "cantidad > 0");
                    table.ForeignKey(
                        name: "FK_composicion_items_alimentarios_alimento_id",
                        column: x => x.alimento_id,
                        principalTable: "items_alimentarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_composicion_items_alimentarios_plato_id",
                        column: x => x.plato_id,
                        principalTable: "items_alimentarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asignacion",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asignacion", x => new { x.menu_id, x.paciente_id });
                    table.ForeignKey(
                        name: "FK_asignacion_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "servicios",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_servicio = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicios", x => new { x.menu_id, x.paciente_id, x.fecha_servicio });
                    table.ForeignKey(
                        name: "FK_servicios_asignacion_menu_id_paciente_id",
                        columns: x => new { x.menu_id, x.paciente_id },
                        principalTable: "asignacion",
                        principalColumns: new[] { "menu_id", "paciente_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "valoraciones_nutricionales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resultado_nutricional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_valoracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_valoraciones_nutricionales", x => x.id);
                    table.ForeignKey(
                        name: "FK_valoraciones_nutricionales_asignacion_menu_id_paciente_id",
                        columns: x => new { x.menu_id, x.paciente_id },
                        principalTable: "asignacion",
                        principalColumns: new[] { "menu_id", "paciente_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_valoraciones_nutricionales_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_valoraciones_nutricionales_resultados_nutricionales_resulta~",
                        column: x => x.resultado_nutricional_id,
                        principalTable: "resultados_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "revalorizaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_solicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valoracion_revisada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valoracion_generada_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revalorizaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_revalorizaciones_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_revalorizaciones_valoraciones_nutricionales_valoracion_gene~",
                        column: x => x.valoracion_generada_id,
                        principalTable: "valoraciones_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_revalorizaciones_valoraciones_nutricionales_valoracion_revi~",
                        column: x => x.valoracion_revisada_id,
                        principalTable: "valoraciones_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consumo",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_servicio = table.Column<DateOnly>(type: "date", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aceptacion = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumo", x => new { x.menu_id, x.paciente_id, x.fecha_servicio, x.item_id });
                    table.ForeignKey(
                        name: "FK_consumo_servicios_menu_id_paciente_id_fecha_servicio",
                        columns: x => new { x.menu_id, x.paciente_id, x.fecha_servicio },
                        principalTable: "servicios",
                        principalColumns: new[] { "menu_id", "paciente_id", "fecha_servicio" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_distribucion",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_preparacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proporcion = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_distribucion", x => new { x.menu_id, x.tipo_preparacion_id });
                    table.CheckConstraint("ck_menu_distribucion_r7_16_proporcion", "proporcion > 0 AND proporcion <= 1");
                    table.ForeignKey(
                        name: "FK_menu_distribucion_tipos_preparacion_tipo_preparacion_id",
                        column: x => x.tipo_preparacion_id,
                        principalTable: "tipos_preparacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_grupo_nutricional",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_nutricional_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_grupo_nutricional", x => new { x.menu_id, x.grupo_nutricional_id });
                    table.ForeignKey(
                        name: "FK_menu_grupo_nutricional_grupos_nutricionales_grupo_nutricion~",
                        column: x => x.grupo_nutricional_id,
                        principalTable: "grupos_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_item",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_item", x => new { x.menu_id, x.item_id });
                    table.ForeignKey(
                        name: "FK_menu_item_items_alimentarios_item_id",
                        column: x => x.item_id,
                        principalTable: "items_alimentarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dieta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutricionista_creador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cantidad_total_alimentos = table.Column<int>(type: "integer", nullable: false),
                    es_automatico = table.Column<bool>(type: "boolean", nullable: false),
                    validacion_origen_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menus", x => x.id);
                    table.CheckConstraint("ck_menus_r7_15_cantidad_total_alimentos", "cantidad_total_alimentos > 0");
                    table.CheckConstraint("ck_menus_r7_37_validacion_origen", "validacion_origen_id <> id");
                    table.ForeignKey(
                        name: "FK_menus_dietas_dieta_id",
                        column: x => x.dieta_id,
                        principalTable: "dietas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_menus_nutricionistas_nutricionista_creador_id",
                        column: x => x.nutricionista_creador_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "validacion",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_validacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    aprobado = table.Column<bool>(type: "boolean", nullable: false),
                    observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_validacion", x => x.menu_id);
                    table.ForeignKey(
                        name: "FK_validacion_menus_menu_id",
                        column: x => x.menu_id,
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_validacion_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "especialidades",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { new Guid("44444444-0000-0000-0000-000000000001"), "Nutrición Clínica" },
                    { new Guid("44444444-0000-0000-0000-000000000002"), "Dietética Hospitalaria" },
                    { new Guid("44444444-0000-0000-0000-000000000003"), "Nutrición Deportiva" }
                });

            migrationBuilder.InsertData(
                table: "grupos_nutricionales",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), "Cereales" },
                    { new Guid("11111111-0000-0000-0000-000000000002"), "Proteínas" },
                    { new Guid("11111111-0000-0000-0000-000000000003"), "Lácteos" },
                    { new Guid("11111111-0000-0000-0000-000000000004"), "Frutas" },
                    { new Guid("11111111-0000-0000-0000-000000000005"), "Verduras" }
                });

            migrationBuilder.InsertData(
                table: "niveles_caloricos",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { new Guid("66666666-0000-0000-0000-000000000001"), "Bajo" },
                    { new Guid("66666666-0000-0000-0000-000000000002"), "Medio" },
                    { new Guid("66666666-0000-0000-0000-000000000003"), "Alto" }
                });

            migrationBuilder.InsertData(
                table: "programas_atencion",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { new Guid("33333333-0000-0000-0000-000000000001"), "Nutrición Pediátrica" },
                    { new Guid("33333333-0000-0000-0000-000000000002"), "Oncología" },
                    { new Guid("33333333-0000-0000-0000-000000000003"), "Geriatría" }
                });

            migrationBuilder.InsertData(
                table: "restricciones_alimentarias",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000001"), "Sin gluten" },
                    { new Guid("22222222-0000-0000-0000-000000000002"), "Hiposódica" },
                    { new Guid("22222222-0000-0000-0000-000000000003"), "Sin lactosa" },
                    { new Guid("22222222-0000-0000-0000-000000000004"), "Vegetariana" },
                    { new Guid("22222222-0000-0000-0000-000000000005"), "Vegana" }
                });

            migrationBuilder.InsertData(
                table: "resultados_nutricionales",
                columns: new[] { "id", "nombre", "valor" },
                values: new object[,]
                {
                    { new Guid("023f26c4-50c4-4e20-8c24-7d27b341dfd6"), "Normal", 3 },
                    { new Guid("138dec6d-8202-4b54-831c-e6fd21508695"), "Excelente", 5 },
                    { new Guid("4077627e-08b1-404a-aeb5-bb64a90538ba"), "Mala", 2 },
                    { new Guid("c0cf2cb9-45bb-4a30-ab00-0c6a8bfc5ca1"), "Buena", 4 },
                    { new Guid("e89977e7-eeb6-4d32-96e2-096ace535bc8"), "Muy mala", 1 }
                });

            migrationBuilder.InsertData(
                table: "tipos_preparacion",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { new Guid("55555555-0000-0000-0000-000000000001"), "Entrante" },
                    { new Guid("55555555-0000-0000-0000-000000000002"), "Plato fuerte" },
                    { new Guid("55555555-0000-0000-0000-000000000003"), "Postre" },
                    { new Guid("55555555-0000-0000-0000-000000000004"), "Bebida" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_asignacion_paciente_id",
                table: "asignacion",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_autoriza_dieta_id",
                table: "autoriza",
                column: "dieta_id");

            migrationBuilder.CreateIndex(
                name: "IX_composicion_alimento_id",
                table: "composicion",
                column: "alimento_id");

            migrationBuilder.CreateIndex(
                name: "IX_consumo_menu_id_item_id",
                table: "consumo",
                columns: new[] { "menu_id", "item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cubre_dieta_grupo_nutricional_id",
                table: "cubre_dieta",
                column: "grupo_nutricional_id");

            migrationBuilder.CreateIndex(
                name: "IX_dietas_programa_atencion_id",
                table: "dietas",
                column: "programa_atencion_id");

            migrationBuilder.CreateIndex(
                name: "IX_especialidades_nombre",
                table: "especialidades",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_excluye_grupo_nutricional_id",
                table: "excluye",
                column: "grupo_nutricional_id");

            migrationBuilder.CreateIndex(
                name: "IX_grupos_nutricionales_nombre",
                table: "grupos_nutricionales",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_alimentarios_grupo_nutricional_id",
                table: "items_alimentarios",
                column: "grupo_nutricional_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_alimentarios_nivel_calorico_id",
                table: "items_alimentarios",
                column: "nivel_calorico_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_alimentarios_nutricionista_id",
                table: "items_alimentarios",
                column: "nutricionista_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_alimentarios_tipo_preparacion_id",
                table: "items_alimentarios",
                column: "tipo_preparacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_menu_distribucion_tipo_preparacion_id",
                table: "menu_distribucion",
                column: "tipo_preparacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_menu_grupo_nutricional_grupo_nutricional_id",
                table: "menu_grupo_nutricional",
                column: "grupo_nutricional_id");

            migrationBuilder.CreateIndex(
                name: "IX_menu_item_item_id",
                table: "menu_item",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_menus_dieta_id",
                table: "menus",
                column: "dieta_id");

            migrationBuilder.CreateIndex(
                name: "IX_menus_nutricionista_creador_id",
                table: "menus",
                column: "nutricionista_creador_id");

            migrationBuilder.CreateIndex(
                name: "ux_menus_r7_38_validacion_origen",
                table: "menus",
                column: "validacion_origen_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_niveles_caloricos_nombre",
                table: "niveles_caloricos",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nutricionistas_especialidad_id",
                table: "nutricionistas",
                column: "especialidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_paciente_dieta_dieta_id",
                table: "paciente_dieta",
                column: "dieta_id");

            migrationBuilder.CreateIndex(
                name: "IX_pacientes_sala_id",
                table: "pacientes",
                column: "sala_id");

            migrationBuilder.CreateIndex(
                name: "IX_programas_atencion_nombre",
                table: "programas_atencion",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_restricciones_alimentarias_nombre",
                table: "restricciones_alimentarias",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_restringe_restriccion_alimentaria_id",
                table: "restringe",
                column: "restriccion_alimentaria_id");

            migrationBuilder.CreateIndex(
                name: "IX_resultados_nutricionales_nombre",
                table: "resultados_nutricionales",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resultados_nutricionales_valor",
                table: "resultados_nutricionales",
                column: "valor",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revalorizaciones_nutricionista_id",
                table: "revalorizaciones",
                column: "nutricionista_id");

            migrationBuilder.CreateIndex(
                name: "IX_revalorizaciones_valoracion_generada_id",
                table: "revalorizaciones",
                column: "valoracion_generada_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revalorizaciones_valoracion_revisada_id",
                table: "revalorizaciones",
                column: "valoracion_revisada_id");

            migrationBuilder.CreateIndex(
                name: "IX_tipos_preparacion_nombre",
                table: "tipos_preparacion",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_validacion_nutricionista_id",
                table: "validacion",
                column: "nutricionista_id");

            migrationBuilder.CreateIndex(
                name: "IX_valoraciones_nutricionales_menu_id_paciente_id",
                table: "valoraciones_nutricionales",
                columns: new[] { "menu_id", "paciente_id" });

            migrationBuilder.CreateIndex(
                name: "IX_valoraciones_nutricionales_nutricionista_id",
                table: "valoraciones_nutricionales",
                column: "nutricionista_id");

            migrationBuilder.CreateIndex(
                name: "IX_valoraciones_nutricionales_resultado_nutricional_id",
                table: "valoraciones_nutricionales",
                column: "resultado_nutricional_id");

            migrationBuilder.AddForeignKey(
                name: "FK_asignacion_menus_menu_id",
                table: "asignacion",
                column: "menu_id",
                principalTable: "menus",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_consumo_menu_item_menu_id_item_id",
                table: "consumo",
                columns: new[] { "menu_id", "item_id" },
                principalTable: "menu_item",
                principalColumns: new[] { "menu_id", "item_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_menu_distribucion_menus_menu_id",
                table: "menu_distribucion",
                column: "menu_id",
                principalTable: "menus",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_menu_grupo_nutricional_menus_menu_id",
                table: "menu_grupo_nutricional",
                column: "menu_id",
                principalTable: "menus",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_menu_item_menus_menu_id",
                table: "menu_item",
                column: "menu_id",
                principalTable: "menus",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_menus_validacion_validacion_origen_id",
                table: "menus",
                column: "validacion_origen_id",
                principalTable: "validacion",
                principalColumn: "menu_id",
                onDelete: ReferentialAction.Restrict);

            // Triggers e índice de las restricciones que EF no declara (Persistence/Sql). Van al final, cuando
            // ya existen todas las tablas que referencian.
            migrationBuilder.AplicarRestriccionesNoDeclarativas();
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Primero se quitan los triggers y el índice: dependen de las tablas que se borran a continuación.
            migrationBuilder.QuitarRestriccionesNoDeclarativas();

            migrationBuilder.DropForeignKey(
                name: "FK_validacion_menus_menu_id",
                table: "validacion");

            migrationBuilder.DropTable(
                name: "autoriza");

            migrationBuilder.DropTable(
                name: "composicion");

            migrationBuilder.DropTable(
                name: "consumo");

            migrationBuilder.DropTable(
                name: "cubre_dieta");

            migrationBuilder.DropTable(
                name: "excluye");

            migrationBuilder.DropTable(
                name: "menu_distribucion");

            migrationBuilder.DropTable(
                name: "menu_grupo_nutricional");

            migrationBuilder.DropTable(
                name: "paciente_dieta");

            migrationBuilder.DropTable(
                name: "restringe");

            migrationBuilder.DropTable(
                name: "revalorizaciones");

            migrationBuilder.DropTable(
                name: "menu_item");

            migrationBuilder.DropTable(
                name: "servicios");

            migrationBuilder.DropTable(
                name: "restricciones_alimentarias");

            migrationBuilder.DropTable(
                name: "valoraciones_nutricionales");

            migrationBuilder.DropTable(
                name: "items_alimentarios");

            migrationBuilder.DropTable(
                name: "asignacion");

            migrationBuilder.DropTable(
                name: "resultados_nutricionales");

            migrationBuilder.DropTable(
                name: "grupos_nutricionales");

            migrationBuilder.DropTable(
                name: "niveles_caloricos");

            migrationBuilder.DropTable(
                name: "tipos_preparacion");

            migrationBuilder.DropTable(
                name: "pacientes");

            migrationBuilder.DropTable(
                name: "salas");

            migrationBuilder.DropTable(
                name: "menus");

            migrationBuilder.DropTable(
                name: "dietas");

            migrationBuilder.DropTable(
                name: "validacion");

            migrationBuilder.DropTable(
                name: "programas_atencion");

            migrationBuilder.DropTable(
                name: "nutricionistas");

            migrationBuilder.DropTable(
                name: "especialidades");
        }
    }
}
