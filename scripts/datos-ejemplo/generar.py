#!/usr/bin/env python3
"""Genera los datos de ejemplo del proyecto (#284).

Escribe, en esta misma carpeta:

- ``datos-ejemplo.sql``: el banco de datos de PostgreSQL, en una sola transacción.
- ``fichas-nutricionales.js``: las fichas nutricionales de MongoDB (mongosh).

Es determinista: con la misma semilla produce siempre los mismos datos, con identificadores fijos, así que los
ejemplos del README y de la documentación siguen valiendo. Solo usa la biblioteca estándar.

Las reglas que impone la base (CHECK, FK y triggers) se respetan al generar, y además se comprueban al final
(``verificar``) para que un cambio en este script no produzca un .sql que la base rechace a medias.

Uso:  python3 scripts/datos-ejemplo/generar.py
"""

from __future__ import annotations

import datetime as dt
import json
import random
import unicodedata
from dataclasses import dataclass, field
from decimal import ROUND_FLOOR, Decimal
from pathlib import Path

SEMILLA = 284
RNG = random.Random(SEMILLA)
CARPETA = Path(__file__).resolve().parent
UTC = dt.timezone.utc

# Fecha de corte de los datos: nada ocurre después (el dominio rechaza fechas futuras).
CORTE = dt.datetime(2026, 11, 23, 18, 0, tzinfo=UTC)

# Contraseña común de los usuarios de ejemplo: «Demo1234!». Hash BCrypt (factor 12) compatible con BCrypt.Net.
CONTRASENA_DEMO = "Demo1234!"
HASH_DEMO = "$2a$12$J9Dn9G5lLOg9spbqB3NRU.GCalfxZuCP96CYQnm6A3.H.Xde.PbcW"
DOMINIO_CORREO = "menus.demo"


def uid(prefijo: str, n: int) -> str:
    """Identificador fijo y legible: el prefijo dice la entidad y el final, el número."""
    return f"{prefijo}-0000-4000-8000-{n:012d}"


# ---------------------------------------------------------------------------------------------------------------
# Catálogos que siembra la migración InitialCreate (GUID fijos)
# ---------------------------------------------------------------------------------------------------------------

GRUPOS = {
    "Cereales": "11111111-0000-0000-0000-000000000001",
    "Proteínas": "11111111-0000-0000-0000-000000000002",
    "Lácteos": "11111111-0000-0000-0000-000000000003",
    "Frutas": "11111111-0000-0000-0000-000000000004",
    "Verduras": "11111111-0000-0000-0000-000000000005",
}
RESTRICCIONES = {
    "Sin gluten": "22222222-0000-0000-0000-000000000001",
    "Hiposódica": "22222222-0000-0000-0000-000000000002",
    "Sin lactosa": "22222222-0000-0000-0000-000000000003",
}
PROGRAMAS = {
    "Nutrición Pediátrica": "33333333-0000-0000-0000-000000000001",
    "Oncología": "33333333-0000-0000-0000-000000000002",
    "Geriatría": "33333333-0000-0000-0000-000000000003",
}
ESPECIALIDADES = {
    "Nutrición Clínica": "44444444-0000-0000-0000-000000000001",
    "Dietética Hospitalaria": "44444444-0000-0000-0000-000000000002",
    "Nutrición Deportiva": "44444444-0000-0000-0000-000000000003",
}
TIPOS = {
    "Entrante": "55555555-0000-0000-0000-000000000001",
    "Plato fuerte": "55555555-0000-0000-0000-000000000002",
    "Postre": "55555555-0000-0000-0000-000000000003",
    "Bebida": "55555555-0000-0000-0000-000000000004",
}
NIVELES = {
    "Bajo": "66666666-0000-0000-0000-000000000001",
    "Medio": "66666666-0000-0000-0000-000000000002",
    "Alto": "66666666-0000-0000-0000-000000000003",
}
RANGO_NIVEL = {"Bajo": 1, "Medio": 2, "Alto": 3}
RESULTADOS = {  # valor -> (nombre, id)
    1: ("Muy mala", "e89977e7-eeb6-4d32-96e2-096ace535bc8"),
    2: ("Mala", "4077627e-08b1-404a-aeb5-bb64a90538ba"),
    3: ("Normal", "023f26c4-50c4-4e20-8c24-7d27b341dfd6"),
    4: ("Buena", "c0cf2cb9-45bb-4a30-ab00-0c6a8bfc5ca1"),
    5: ("Excelente", "138dec6d-8202-4b54-831c-e6fd21508695"),
}

# Excluye (restricción → grupo que prohíbe). El catálogo de la migración no trae ninguna; esta es la única que el
# modelo por grupos expresa sin ambigüedad. Las demás restricciones se respetan al elegir los ítems (alérgenos y
# sodio), porque el grupo nutricional no basta para decidirlas.
EXCLUYE = [("Sin lactosa", "Lácteos")]

# ---------------------------------------------------------------------------------------------------------------
# Salas y nutricionistas
# ---------------------------------------------------------------------------------------------------------------

SALAS = ["Pediatría", "Oncología", "Geriatría", "Medicina Interna A", "Medicina Interna B", "Cirugía General"]

# (nombre, especialidad, es jefe)
NUTRICIONISTAS = [
    ("Laura Méndez Ortiz", "Nutrición Clínica", True),
    ("Ernesto Valdés Rivero", "Dietética Hospitalaria", True),
    ("Marisol Fuentes Cabrera", "Nutrición Clínica", True),
    ("Javier Hernández Soto", "Nutrición Clínica", False),
    ("Ana Beatriz Rojas Pérez", "Dietética Hospitalaria", False),
    ("Carlos Alberto Díaz Medina", "Nutrición Deportiva", False),
    ("Yanet Morales Castillo", "Nutrición Clínica", False),
    ("Raúl Fernández Gil", "Dietética Hospitalaria", False),
    ("Daniela Suárez León", "Nutrición Clínica", False),
    ("Pedro Luis Ramírez Vega", "Nutrición Deportiva", False),  # recién incorporado: sin dietas autorizadas
]

# ---------------------------------------------------------------------------------------------------------------
# Dietas: programa, grupos requeridos, grupos restringidos, restricciones y nutricionistas autorizados (índices)
# ---------------------------------------------------------------------------------------------------------------


@dataclass(eq=False)
class Dieta:
    n: int
    nombre: str
    programa: str
    requeridos: list[str]
    restringidos: list[str] = field(default_factory=list)
    restricciones: list[str] = field(default_factory=list)
    autorizados: list[int] = field(default_factory=list)
    sin_dulces: bool = False

    @property
    def id(self) -> str:
        return uid("d1e7a000", self.n)

    def prohibidos(self) -> set[str]:
        """Grupos prohibidos: los restringidos más los que excluyen sus restricciones (decisión D9-1)."""
        excluidos = {g for r, g in EXCLUYE if r in self.restricciones}
        return set(self.restringidos) | excluidos


PEDIATRIA = [0, 2, 3, 4]
ONCOLOGIA = [1, 2, 5, 6]
GERIATRIA = [0, 1, 7, 8]

DIETAS = [
    Dieta(1, "Normocalórica pediátrica", "Nutrición Pediátrica",
          ["Cereales", "Proteínas", "Lácteos", "Frutas", "Verduras"], autorizados=PEDIATRIA),
    Dieta(2, "Pediátrica sin gluten", "Nutrición Pediátrica",
          ["Proteínas", "Lácteos", "Frutas", "Verduras"], restricciones=["Sin gluten"], autorizados=PEDIATRIA),
    Dieta(3, "Hiperproteica oncológica", "Oncología",
          ["Proteínas", "Lácteos", "Cereales", "Verduras"], autorizados=ONCOLOGIA),
    Dieta(4, "Blanda oncológica sin lactosa", "Oncología",
          ["Cereales", "Proteínas", "Frutas"], restricciones=["Sin lactosa"], autorizados=ONCOLOGIA),
    Dieta(5, "Geriátrica hiposódica", "Geriatría",
          ["Cereales", "Proteínas", "Verduras", "Frutas"], restricciones=["Hiposódica"], autorizados=GERIATRIA),
    Dieta(6, "Hipoglucídica geriátrica", "Geriatría",
          ["Proteínas", "Verduras"], restringidos=["Cereales"], autorizados=GERIATRIA, sin_dulces=True),
    Dieta(7, "Geriátrica de fácil masticación", "Geriatría",
          ["Cereales", "Lácteos", "Frutas"], autorizados=GERIATRIA),
    # Recién creada: todavía sin menús ni pacientes, para ver una dieta vacía en la UI y en las consultas.
    Dieta(8, "Oncológica de transición", "Oncología", ["Proteínas", "Verduras"], autorizados=[1, 6]),
]

# ---------------------------------------------------------------------------------------------------------------
# Banco de alimentos y platos
# ---------------------------------------------------------------------------------------------------------------


@dataclass(eq=False)
class Item:
    n: int
    nombre: str
    clase: str  # Alimento | Plato
    grupo: str
    tipo: str
    nivel: str
    registra: int  # índice del nutricionista que lo registró
    kcal: float = 0
    proteinas: float = 0
    carbohidratos: float = 0
    grasas: float = 0
    fibra: float = 0
    sodio: float = 0
    alergenos: list[str] = field(default_factory=list)
    dulce: bool = False
    composicion: list[tuple["Item", int]] = field(default_factory=list)  # (alimento, gramos)
    tecnica: str = ""
    pasos: list[tuple[str, int]] = field(default_factory=list)

    @property
    def id(self) -> str:
        return uid("17e30000", self.n)


# (nombre, grupo, tipo, nivel, kcal, proteínas, carbohidratos, grasas, fibra, sodio mg, alérgenos, dulce)
# Valores por 100 g, aproximados de tablas de composición de alimentos.
ALIMENTOS = [
    ("Arroz blanco", "Cereales", "Plato fuerte", "Medio", 130, 2.7, 28.2, 0.3, 0.4, 1, [], False),
    ("Arroz integral", "Cereales", "Plato fuerte", "Medio", 123, 2.7, 25.6, 1.0, 1.6, 4, [], False),
    ("Pan de trigo", "Cereales", "Entrante", "Medio", 265, 9.0, 49.0, 3.2, 2.7, 491, ["gluten"], False),
    ("Pan integral", "Cereales", "Entrante", "Medio", 247, 13.0, 41.0, 3.4, 7.0, 450, ["gluten"], False),
    ("Galletas de soda", "Cereales", "Entrante", "Alto", 421, 9.5, 71.0, 10.0, 2.5, 950, ["gluten"], False),
    ("Pasta corta", "Cereales", "Plato fuerte", "Medio", 158, 5.8, 31.0, 0.9, 1.8, 1, ["gluten"], False),
    ("Avena en hojuelas", "Cereales", "Entrante", "Medio", 389, 16.9, 66.3, 6.9, 10.6, 2, ["gluten"], False),
    ("Harina de maíz", "Cereales", "Plato fuerte", "Medio", 361, 6.9, 76.8, 3.9, 7.3, 5, [], False),
    ("Papa", "Cereales", "Plato fuerte", "Medio", 87, 1.9, 20.1, 0.1, 1.8, 5, [], False),
    ("Boniato", "Cereales", "Plato fuerte", "Medio", 86, 1.6, 20.1, 0.1, 3.0, 55, [], False),
    ("Yuca", "Cereales", "Plato fuerte", "Alto", 160, 1.4, 38.1, 0.3, 1.8, 14, [], False),
    ("Plátano macho", "Cereales", "Plato fuerte", "Alto", 122, 1.3, 31.9, 0.4, 2.3, 4, [], False),
    ("Maíz tierno", "Cereales", "Entrante", "Medio", 96, 3.4, 21.0, 1.5, 2.4, 15, [], False),
    ("Quinoa", "Cereales", "Plato fuerte", "Medio", 120, 4.4, 21.3, 1.9, 2.8, 7, [], False),
    ("Pechuga de pollo", "Proteínas", "Plato fuerte", "Medio", 165, 31.0, 0.0, 3.6, 0.0, 74, [], False),
    ("Muslo de pollo", "Proteínas", "Plato fuerte", "Alto", 209, 26.0, 0.0, 10.9, 0.0, 95, [], False),
    ("Filete de pescado blanco", "Proteínas", "Plato fuerte", "Bajo", 96, 20.0, 0.0, 1.7, 0.0, 78,
     ["pescado"], False),
    ("Atún en agua", "Proteínas", "Plato fuerte", "Bajo", 116, 25.5, 0.0, 0.8, 0.0, 338, ["pescado"], False),
    ("Huevo", "Proteínas", "Entrante", "Medio", 155, 12.6, 1.1, 10.6, 0.0, 124, ["huevo"], False),
    ("Clara de huevo", "Proteínas", "Entrante", "Bajo", 52, 10.9, 0.7, 0.2, 0.0, 166, ["huevo"], False),
    ("Carne de res magra", "Proteínas", "Plato fuerte", "Medio", 187, 27.0, 0.0, 8.0, 0.0, 66, [], False),
    ("Cerdo magro", "Proteínas", "Plato fuerte", "Alto", 242, 27.3, 0.0, 13.9, 0.0, 62, [], False),
    ("Jamón de pavo", "Proteínas", "Entrante", "Bajo", 104, 17.0, 3.8, 2.3, 0.0, 1050, [], False),
    ("Frijoles negros", "Proteínas", "Plato fuerte", "Medio", 132, 8.9, 23.7, 0.5, 8.7, 1, [], False),
    ("Lentejas", "Proteínas", "Plato fuerte", "Medio", 116, 9.0, 20.1, 0.4, 7.9, 2, [], False),
    ("Garbanzos", "Proteínas", "Plato fuerte", "Alto", 164, 8.9, 27.4, 2.6, 7.6, 7, [], False),
    ("Tofu", "Proteínas", "Plato fuerte", "Medio", 144, 15.8, 2.8, 8.7, 2.3, 14, ["soja"], False),
    ("Camarones", "Proteínas", "Plato fuerte", "Bajo", 99, 24.0, 0.2, 0.3, 0.0, 111, ["crustáceos"], False),
    ("Leche entera", "Lácteos", "Bebida", "Medio", 61, 3.2, 4.8, 3.3, 0.0, 43, ["leche"], False),
    ("Leche descremada", "Lácteos", "Bebida", "Bajo", 34, 3.4, 5.0, 0.1, 0.0, 42, ["leche"], False),
    ("Yogur natural", "Lácteos", "Postre", "Bajo", 61, 3.5, 4.7, 3.3, 0.0, 46, ["leche"], False),
    ("Yogur de fresa", "Lácteos", "Postre", "Medio", 99, 3.0, 17.0, 2.0, 0.0, 50, ["leche"], True),
    ("Queso blanco", "Lácteos", "Entrante", "Medio", 264, 18.0, 4.1, 20.0, 0.0, 330, ["leche"], False),
    ("Queso gouda", "Lácteos", "Entrante", "Alto", 356, 25.0, 2.2, 27.4, 0.0, 819, ["leche"], False),
    ("Requesón", "Lácteos", "Entrante", "Bajo", 98, 11.1, 3.4, 4.3, 0.0, 364, ["leche"], False),
    ("Helado de vainilla", "Lácteos", "Postre", "Alto", 207, 3.5, 23.6, 11.0, 0.7, 80, ["leche", "huevo"],
     True),
    ("Natilla", "Lácteos", "Postre", "Alto", 122, 4.1, 18.0, 3.7, 0.0, 60, ["leche", "huevo"], True),
    ("Manzana", "Frutas", "Postre", "Bajo", 52, 0.3, 13.8, 0.2, 2.4, 1, [], False),
    ("Plátano fruta", "Frutas", "Postre", "Medio", 89, 1.1, 22.8, 0.3, 2.6, 1, [], False),
    ("Guayaba", "Frutas", "Postre", "Bajo", 68, 2.6, 14.3, 1.0, 5.4, 2, [], False),
    ("Mango", "Frutas", "Postre", "Medio", 60, 0.8, 15.0, 0.4, 1.6, 1, [], True),
    ("Papaya", "Frutas", "Postre", "Bajo", 43, 0.5, 10.8, 0.3, 1.7, 8, [], False),
    ("Piña", "Frutas", "Postre", "Bajo", 50, 0.5, 13.1, 0.1, 1.4, 1, [], False),
    ("Naranja", "Frutas", "Postre", "Bajo", 47, 0.9, 11.8, 0.1, 2.4, 0, [], False),
    ("Melón", "Frutas", "Postre", "Bajo", 34, 0.8, 8.2, 0.2, 0.9, 16, [], False),
    ("Fresas", "Frutas", "Postre", "Bajo", 32, 0.7, 7.7, 0.3, 2.0, 1, [], False),
    ("Jugo de naranja natural", "Frutas", "Bebida", "Bajo", 45, 0.7, 10.4, 0.2, 0.2, 1, [], False),
    ("Jugo de mango", "Frutas", "Bebida", "Medio", 54, 0.1, 13.1, 0.1, 0.3, 4, [], True),
    ("Jugo de guayaba", "Frutas", "Bebida", "Bajo", 37, 0.3, 9.0, 0.1, 1.0, 3, [], False),
    ("Limonada", "Frutas", "Bebida", "Bajo", 40, 0.1, 10.6, 0.0, 0.0, 4, [], True),
    ("Compota de manzana", "Frutas", "Postre", "Medio", 68, 0.2, 17.5, 0.1, 1.1, 2, [], True),
    ("Dulce de guayaba", "Frutas", "Postre", "Alto", 270, 0.4, 69.0, 0.1, 3.0, 10, [], True),
    ("Pasas", "Frutas", "Postre", "Alto", 299, 3.1, 79.2, 0.5, 3.7, 11, [], True),
    ("Lechuga", "Verduras", "Entrante", "Bajo", 15, 1.4, 2.9, 0.2, 1.3, 28, [], False),
    ("Tomate", "Verduras", "Entrante", "Bajo", 18, 0.9, 3.9, 0.2, 1.2, 5, [], False),
    ("Pepino", "Verduras", "Entrante", "Bajo", 15, 0.7, 3.6, 0.1, 0.5, 2, [], False),
    ("Zanahoria", "Verduras", "Entrante", "Bajo", 41, 0.9, 9.6, 0.2, 2.8, 69, [], False),
    ("Habichuelas", "Verduras", "Entrante", "Bajo", 31, 1.8, 7.0, 0.2, 2.7, 6, [], False),
    ("Calabaza", "Verduras", "Entrante", "Bajo", 26, 1.0, 6.5, 0.1, 0.5, 1, [], False),
    ("Brócoli", "Verduras", "Entrante", "Bajo", 34, 2.8, 6.6, 0.4, 2.6, 33, [], False),
    ("Col", "Verduras", "Entrante", "Bajo", 25, 1.3, 5.8, 0.1, 2.5, 18, [], False),
    ("Berenjena", "Verduras", "Plato fuerte", "Bajo", 25, 1.0, 5.9, 0.2, 3.0, 2, [], False),
    ("Espinaca", "Verduras", "Entrante", "Bajo", 23, 2.9, 3.6, 0.4, 2.2, 79, [], False),
    ("Remolacha", "Verduras", "Entrante", "Bajo", 43, 1.6, 9.6, 0.2, 2.8, 78, [], False),
    ("Aguacate", "Verduras", "Entrante", "Alto", 160, 2.0, 8.5, 14.7, 6.7, 7, [], False),
    ("Quimbombó", "Verduras", "Plato fuerte", "Bajo", 33, 1.9, 7.5, 0.2, 3.2, 7, [], False),
]

# (nombre, grupo, tipo, nivel, [(alimento, gramos)], técnica, [(paso, minutos)])
PLATOS = [
    ("Arroz con pollo", "Cereales", "Plato fuerte", "Alto",
     [("Arroz blanco", 150), ("Muslo de pollo", 120), ("Zanahoria", 30)], "guiso",
     [("Dorar el pollo troceado.", 10), ("Sofreír la zanahoria.", 5),
      ("Añadir el arroz y el caldo; cocer a fuego lento.", 30)]),
    ("Pollo asado con papas", "Proteínas", "Plato fuerte", "Medio",
     [("Pechuga de pollo", 150), ("Papa", 150)], "horneado",
     [("Adobar la pechuga.", 15), ("Hornear con las papas en cuartos a 200 °C.", 40)]),
    ("Pescado a la plancha con vegetales", "Proteínas", "Plato fuerte", "Bajo",
     [("Filete de pescado blanco", 150), ("Brócoli", 80), ("Zanahoria", 50)], "plancha",
     [("Cocer al vapor el brócoli y la zanahoria.", 10), ("Asar el filete a la plancha.", 8)]),
    ("Potaje de frijoles negros", "Proteínas", "Plato fuerte", "Medio",
     [("Frijoles negros", 120), ("Calabaza", 40)], "guiso",
     [("Remojar los frijoles la noche anterior.", 5), ("Cocer en olla de presión.", 35),
      ("Añadir la calabaza y espesar.", 15)]),
    ("Potaje de lentejas", "Proteínas", "Plato fuerte", "Medio",
     [("Lentejas", 120), ("Zanahoria", 40), ("Calabaza", 40)], "guiso",
     [("Cocer las lentejas.", 25), ("Añadir las verduras en dados.", 15)]),
    ("Puré de papa", "Cereales", "Plato fuerte", "Medio",
     [("Papa", 200), ("Leche entera", 50)], "hervido",
     [("Hervir las papas peladas.", 20), ("Majar con la leche caliente.", 5)]),
    ("Puré de calabaza", "Verduras", "Entrante", "Bajo",
     [("Calabaza", 200)], "hervido", [("Hervir la calabaza.", 15), ("Triturar hasta textura fina.", 3)]),
    ("Ensalada mixta", "Verduras", "Entrante", "Bajo",
     [("Lechuga", 50), ("Tomate", 60), ("Pepino", 50)], "crudo",
     [("Lavar y desinfectar las verduras.", 5), ("Cortar y aliñar al momento.", 5)]),
    ("Ensalada de aguacate y tomate", "Verduras", "Entrante", "Alto",
     [("Aguacate", 100), ("Tomate", 80)], "crudo", [("Cortar en cubos y aliñar con limón.", 5)]),
    ("Sopa de pollo con fideos", "Proteínas", "Entrante", "Medio",
     [("Muslo de pollo", 60), ("Pasta corta", 40), ("Zanahoria", 30)], "hervido",
     [("Preparar el caldo con el pollo.", 30), ("Añadir los fideos y la zanahoria.", 10)]),
    ("Crema de calabaza", "Verduras", "Entrante", "Medio",
     [("Calabaza", 150), ("Leche descremada", 100)], "hervido",
     [("Cocer la calabaza.", 15), ("Licuar con la leche.", 3)]),
    ("Tortilla de huevo", "Proteínas", "Plato fuerte", "Medio",
     [("Huevo", 120)], "sartén", [("Batir los huevos.", 2), ("Cuajar a fuego medio.", 6)]),
    ("Tortilla de espinaca", "Proteínas", "Plato fuerte", "Medio",
     [("Huevo", 100), ("Espinaca", 50)], "sartén",
     [("Saltear la espinaca.", 4), ("Añadir el huevo batido y cuajar.", 6)]),
    ("Ropa vieja", "Proteínas", "Plato fuerte", "Alto",
     [("Carne de res magra", 150), ("Tomate", 40)], "guiso",
     [("Cocer la carne y deshilacharla.", 60), ("Guisar en salsa de tomate.", 20)]),
    ("Picadillo de res", "Proteínas", "Plato fuerte", "Medio",
     [("Carne de res magra", 120), ("Tomate", 30), ("Zanahoria", 20)], "guiso",
     [("Sofreír la carne molida.", 10), ("Añadir el tomate y la zanahoria.", 15)]),
    ("Arroz congrí", "Cereales", "Plato fuerte", "Alto",
     [("Arroz blanco", 120), ("Frijoles negros", 60)], "guiso",
     [("Cocer los frijoles.", 30), ("Añadir el arroz al caldo de frijoles.", 25)]),
    ("Macedonia de frutas", "Frutas", "Postre", "Bajo",
     [("Papaya", 60), ("Piña", 60), ("Melón", 60)], "crudo", [("Pelar y cortar en dados.", 10)]),
    ("Arroz con leche", "Lácteos", "Postre", "Alto",
     [("Arroz blanco", 50), ("Leche entera", 150)], "hervido",
     [("Cocer el arroz en la leche a fuego lento.", 35), ("Enfriar antes de servir.", 30)]),
    ("Batido de plátano", "Lácteos", "Bebida", "Medio",
     [("Leche descremada", 200), ("Plátano fruta", 100)], "licuado", [("Licuar con hielo.", 3)]),
    ("Batido de mango", "Frutas", "Bebida", "Medio",
     [("Mango", 150)], "licuado", [("Licuar la pulpa con agua y hielo.", 3)]),
    ("Yuca con mojo", "Cereales", "Plato fuerte", "Alto",
     [("Yuca", 200)], "hervido", [("Hervir la yuca hasta que abra.", 30), ("Bañar con mojo de ajo.", 5)]),
    ("Vianda hervida", "Cereales", "Plato fuerte", "Medio",
     [("Boniato", 100), ("Papa", 100)], "hervido", [("Hervir las viandas en trozos.", 25)]),
    ("Pasta con atún", "Cereales", "Plato fuerte", "Medio",
     [("Pasta corta", 120), ("Atún en agua", 80), ("Tomate", 40)], "hervido",
     [("Cocer la pasta al dente.", 10), ("Mezclar con el atún y la salsa de tomate.", 5)]),
    ("Revoltillo de camarones", "Proteínas", "Plato fuerte", "Medio",
     [("Camarones", 100), ("Huevo", 60)], "sartén",
     [("Saltear los camarones.", 5), ("Añadir el huevo y revolver.", 4)]),
    ("Tofu salteado con vegetales", "Proteínas", "Plato fuerte", "Bajo",
     [("Tofu", 120), ("Brócoli", 60), ("Zanahoria", 40)], "salteado",
     [("Dorar el tofu en dados.", 8), ("Saltear las verduras.", 6)]),
    ("Avena con leche", "Cereales", "Entrante", "Medio",
     [("Avena en hojuelas", 50), ("Leche descremada", 150)], "hervido", [("Cocer la avena en la leche.", 8)]),
    ("Quinoa con vegetales", "Cereales", "Plato fuerte", "Medio",
     [("Quinoa", 100), ("Habichuelas", 60), ("Zanahoria", 40)], "hervido",
     [("Cocer la quinoa.", 15), ("Saltear las verduras y mezclar.", 8)]),
]

# Ítems que los pacientes rechazan más a menudo: alimenta la consulta 6b con un patrón visible.
POCO_ACEPTADOS = {"Brócoli", "Quimbombó", "Espinaca", "Berenjena", "Lentejas", "Pescado a la plancha con vegetales"}


def construir_banco() -> list[Item]:
    items: list[Item] = []
    por_nombre: dict[str, Item] = {}
    for i, (nombre, grupo, tipo, nivel, kcal, prot, carb, gras, fib, sod, alerg, dulce) in enumerate(ALIMENTOS):
        item = Item(i + 1, nombre, "Alimento", grupo, tipo, nivel, registra=[3, 4, 6, 7, 8][i % 5],
                    kcal=kcal, proteinas=prot, carbohidratos=carb, grasas=gras, fibra=fib, sodio=sod,
                    alergenos=list(alerg), dulce=dulce)
        items.append(item)
        por_nombre[nombre] = item
    base = len(items)
    for j, (nombre, grupo, tipo, nivel, comp, tecnica, pasos) in enumerate(PLATOS):
        composicion = [(por_nombre[a], g) for a, g in comp]
        plato = Item(base + j + 1, nombre, "Plato", grupo, tipo, nivel, registra=[3, 5, 6, 8, 4][j % 5],
                     composicion=composicion, tecnica=tecnica, pasos=pasos)
        # La ficha del plato agrega la de sus ingredientes; para elegir ítems por dieta se usa lo mismo.
        plato.kcal = sum(a.kcal * g / 100 for a, g in composicion)
        plato.proteinas = sum(a.proteinas * g / 100 for a, g in composicion)
        plato.carbohidratos = sum(a.carbohidratos * g / 100 for a, g in composicion)
        plato.grasas = sum(a.grasas * g / 100 for a, g in composicion)
        plato.fibra = sum(a.fibra * g / 100 for a, g in composicion)
        plato.sodio = sum(a.sodio * g / 100 for a, g in composicion) + (180 if tecnica in ("guiso", "sartén") else 0)
        plato.alergenos = sorted({x for a, _ in composicion for x in a.alergenos})
        plato.dulce = nombre in ("Arroz con leche", "Batido de mango", "Batido de plátano")
        items.append(plato)
        por_nombre[nombre] = plato
    return items


def compatible(item: Item, dieta: Dieta) -> bool:
    """Si una estrategia de generación elegiría el ítem para la dieta."""
    if item.grupo in dieta.prohibidos():
        return False
    if "Sin gluten" in dieta.restricciones and "gluten" in item.alergenos:
        return False
    if "Sin lactosa" in dieta.restricciones and "leche" in item.alergenos:
        return False
    if "Hiposódica" in dieta.restricciones and item.sodio > 300:
        return False
    if dieta.sin_dulces and item.dulce:
        return False
    return True


# ---------------------------------------------------------------------------------------------------------------
# Pacientes
# ---------------------------------------------------------------------------------------------------------------

NOMBRES = ["María", "José", "Ana", "Luis", "Carmen", "Jorge", "Rosa", "Miguel", "Elena", "Alejandro", "Lucía",
           "Roberto", "Isabel", "Fernando", "Teresa", "Andrés", "Patricia", "Manuel", "Sofía", "Ricardo",
           "Gabriela", "Alberto", "Mercedes", "Héctor", "Lourdes", "Ramón", "Silvia", "Orlando", "Beatriz",
           "Emilio", "Claudia", "Reinaldo", "Dayana", "Osvaldo", "Yamilé", "Ernesto", "Liset", "Armando"]
APELLIDOS = ["García", "Rodríguez", "González", "Fernández", "López", "Martínez", "Sánchez", "Pérez", "Gómez",
             "Martín", "Jiménez", "Ruiz", "Hernández", "Díaz", "Moreno", "Álvarez", "Romero", "Alonso",
             "Gutiérrez", "Navarro", "Torres", "Domínguez", "Vázquez", "Ramos", "Gil", "Ramírez", "Serrano",
             "Blanco", "Molina", "Morales", "Suárez", "Ortega", "Delgado", "Castro", "Ortiz", "Rubio", "Marín"]


@dataclass(eq=False)
class Paciente:
    n: int
    nombre: str
    edad: int
    sala: int
    dietas: list[int]
    apetito: float  # inclinación a aceptar lo que se le sirve, para dar variedad a la consulta 4

    @property
    def id(self) -> str:
        return uid("9ac1e000", self.n)


# (cantidad, rango de edad, sala, dietas posibles, probabilidad de inscribirse en una segunda dieta)
GRUPOS_PACIENTES = [
    (10, (2, 15), 0, [[1], [1], [2]], 0.0),
    (2, (6, 14), 5, [[1]], 0.0),
    (9, (24, 74), 1, [[3], [3], [4]], 0.15),
    (3, (35, 70), 3, [[3], [4]], 0.0),
    (2, (40, 65), 5, [[4]], 0.0),
    (10, (66, 93), 2, [[5], [6], [7]], 0.3),
    (4, (67, 88), 3, [[5], [7]], 0.25),
    (4, (65, 85), 4, [[6], [5]], 0.25),
    (4, (30, 80), 4, [[]], 0.0),  # recién ingresados: todavía sin dieta
]
SEGUNDA_DIETA = {1: [], 2: [], 3: [4], 4: [3], 5: [6], 6: [5], 7: [5]}


def construir_pacientes() -> list[Paciente]:
    pacientes: list[Paciente] = []
    usados: set[str] = set()
    n = 0
    for cantidad, (edad_min, edad_max), sala, opciones, prob_segunda in GRUPOS_PACIENTES:
        for _ in range(cantidad):
            n += 1
            while True:
                nombre = f"{RNG.choice(NOMBRES)} {RNG.choice(APELLIDOS)} {RNG.choice(APELLIDOS)}"
                if nombre not in usados:
                    usados.add(nombre)
                    break
            dietas = list(RNG.choice(opciones))
            if dietas and SEGUNDA_DIETA[dietas[0]] and RNG.random() < prob_segunda:
                dietas.append(SEGUNDA_DIETA[dietas[0]][0])
            pacientes.append(Paciente(n, nombre, RNG.randint(edad_min, edad_max), sala, dietas,
                                      apetito=round(RNG.uniform(-0.12, 0.12), 3)))
    return pacientes


# ---------------------------------------------------------------------------------------------------------------
# Menús
# ---------------------------------------------------------------------------------------------------------------

# Plantillas de parametrización: (cantidad total, {tipo: proporción}). Las proporciones suman 1 (RS-08).
PLANTILLAS = {
    "A": (4, {"Entrante": Decimal("0.25"), "Plato fuerte": Decimal("0.5"), "Postre": Decimal("0.25")}),
    "B": (5, {"Entrante": Decimal("0.2"), "Plato fuerte": Decimal("0.4"), "Postre": Decimal("0.2"),
              "Bebida": Decimal("0.2")}),
    "C": (6, {"Entrante": Decimal("0.333"), "Plato fuerte": Decimal("0.334"), "Postre": Decimal("0.333")}),
    "D": (4, {"Entrante": Decimal("0.25"), "Plato fuerte": Decimal("0.25"), "Postre": Decimal("0.25"),
              "Bebida": Decimal("0.25")}),
    "E": (3, {"Plato fuerte": Decimal("0.667"), "Postre": Decimal("0.333")}),
}


@dataclass(eq=False)
class Menu:
    n: int
    dieta: Dieta
    creador: int
    fecha: dt.datetime
    automatico: bool
    cantidad: int
    distribucion: dict[str, Decimal]
    grupos: list[str]
    items: list[Item]
    estado: str  # aprobado | rechazado | pendiente
    revisor: int | None = None
    fecha_validacion: dt.datetime | None = None
    observaciones: str | None = None
    origen: "Menu | None" = None  # menú rechazado al que reemplaza (Origina)

    @property
    def id(self) -> str:
        return uid("3e700000", self.n)


def repartir(cantidad: int, distribucion: dict[str, Decimal]) -> dict[str, int]:
    """Método del resto mayor, igual que EstrategiaGeneracionAleatoria."""
    cuotas = {t: p * cantidad for t, p in distribucion.items()}
    pisos = {t: int(c.to_integral_value(ROUND_FLOOR)) for t, c in cuotas.items()}
    faltan = cantidad - sum(pisos.values())
    for t in sorted(cuotas, key=lambda t: cuotas[t] - pisos[t], reverse=True)[:faltan]:
        pisos[t] += 1
    return pisos


def elegir_items(dieta: Dieta, cantidad: int, distribucion: dict[str, Decimal], banco: list[Item]) -> list[Item]:
    """Como la estrategia de la aplicación, pero buscando primero cubrir cada grupo requerido."""
    faltantes = set(dieta.requeridos)
    elegidos: list[Item] = []
    for tipo, k in repartir(cantidad, distribucion).items():
        candidatos = [i for i in banco if i.tipo == tipo and compatible(i, dieta)]
        RNG.shuffle(candidatos)
        if len(candidatos) < k:
            raise ValueError(f"No hay {k} ítems de tipo {tipo} para la dieta {dieta.nombre}")
        tomados: list[Item] = []
        for _ in range(k):
            resto = [c for c in candidatos if c not in tomados]
            mejor = next((c for c in resto if c.grupo in faltantes), None) \
                or next((c for c in resto if c.grupo in dieta.requeridos), resto[0])
            tomados.append(mejor)
            faltantes.discard(mejor.grupo)
        elegidos.extend(tomados)
    return elegidos


# Ciclo de vida de los menús de cada dieta: (automático, plantilla, estado). Un rechazo origina un reemplazo.
CICLO = [
    (True, "A", "aprobado"),
    (True, "B", "aprobado"),
    (False, "C", "aprobado"),
    (True, "D", "rechazado"),
    (True, "A", "aprobado"),
    (True, "B", "aprobado"),
    (False, "A", "pendiente"),
]
# Plantillas por dieta: una dieta sin bebidas compatibles suficientes o con pocos tipos usa otras.
PLANTILLAS_POR_DIETA = {6: ["A", "E", "A", "D", "E", "A", "A"], 4: ["A", "B", "C", "D", "B", "A", "C"]}

OBSERVACIONES_APROBADO = [None, "Aprobado. Buena variedad de grupos.", None,
                          "Aprobado. Revisar la porción del plato fuerte en la próxima rotación.", None,
                          "Aprobado sin observaciones."]
OBSERVACIONES_RECHAZO = [
    "Rechazado: el postre aporta demasiada azúcar para el perfil de la dieta.",
    "Rechazado: repite el mismo plato fuerte que el menú anterior; pedir más variedad.",
    "Rechazado: no cubre todos los grupos que exige la dieta.",
    "Rechazado: la textura de los ítems no es adecuada para estos pacientes.",
]


def jefes_de(dieta: Dieta) -> list[int]:
    return [i for i in dieta.autorizados if NUTRICIONISTAS[i][2]]


def construir_menus(banco: list[Item]) -> list[Menu]:
    menus: list[Menu] = []
    por_nombre = {i.nombre: i for i in banco}
    n = 0
    for dieta in DIETAS[:7]:
        creadores = [i for i in dieta.autorizados if not NUTRICIONISTAS[i][2]] + jefes_de(dieta)[:1]
        plantillas = PLANTILLAS_POR_DIETA.get(dieta.n, [p for _, p, _ in CICLO])
        inicio = dt.datetime(2026, 10, 5, 8, 30, tzinfo=UTC) + dt.timedelta(hours=dieta.n * 3, days=dieta.n % 3)
        for k, (automatico, _, estado) in enumerate(CICLO):
            n += 1
            fecha = inicio + dt.timedelta(days=7 * k, minutes=RNG.randint(0, 90))
            if estado == "pendiente":
                fecha = dt.datetime(2026, 11, 20, 9, 0, tzinfo=UTC) + dt.timedelta(hours=dieta.n * 5)
            cantidad, distribucion = PLANTILLAS[plantillas[k]]
            creador = creadores[k % len(creadores)]
            items = elegir_items(dieta, cantidad, distribucion, banco)
            observaciones = None
            if not automatico and k == 2:
                # Menús manuales: el nutricionista se aparta a veces de lo declarado; la consulta 5 lo detecta.
                if dieta.n == 1:
                    items = items[:-1]  # declara 6 y deja 5
                elif dieta.n == 3:
                    items = items + [por_nombre["Batido de plátano"]]  # una bebida que la distribución no prevé
                elif dieta.n == 6:
                    items = items[:-1] + [por_nombre["Arroz blanco"]]  # un cereal, grupo restringido
                    estado = "rechazado"
                    observaciones = "Rechazado: incluye arroz blanco y los cereales están restringidos en esta dieta."
            menu = Menu(n, dieta, creador, fecha, automatico, cantidad, dict(distribucion), list(dieta.requeridos),
                        items, estado, observaciones=observaciones)
            menus.append(menu)
            if estado in ("aprobado", "rechazado"):
                jefes = [j for j in jefes_de(dieta) if j != creador]
                menu.revisor = jefes[k % len(jefes)]
                menu.fecha_validacion = fecha + dt.timedelta(days=1, hours=RNG.randint(1, 6))
                if estado == "rechazado":
                    menu.observaciones = menu.observaciones or OBSERVACIONES_RECHAZO[(dieta.n + k) % 4]
                    # El rechazo origina un menú de reemplazo, creado al día siguiente y aprobado.
                    n += 1
                    f_reemplazo = menu.fecha_validacion + dt.timedelta(days=1, hours=2)
                    cant_r, dist_r = PLANTILLAS["A"]
                    reemplazo = Menu(n, dieta, creador, f_reemplazo, True, cant_r, dict(dist_r),
                                     list(dieta.requeridos), elegir_items(dieta, cant_r, dist_r, banco), "aprobado",
                                     origen=menu)
                    reemplazo.revisor = menu.revisor
                    reemplazo.fecha_validacion = f_reemplazo + dt.timedelta(hours=20)
                    reemplazo.observaciones = "Aprobado. Corrige lo señalado en el menú rechazado."
                    menus.append(reemplazo)
                else:
                    menu.observaciones = OBSERVACIONES_APROBADO[(dieta.n + k) % len(OBSERVACIONES_APROBADO)]
    return menus


# ---------------------------------------------------------------------------------------------------------------
# Asignaciones, servicios, consumo, valoraciones y revalorizaciones
# ---------------------------------------------------------------------------------------------------------------


@dataclass(eq=False)
class Valoracion:
    n: int
    menu: Menu
    paciente: Paciente
    nutricionista: int
    valor: int
    fecha: dt.datetime

    @property
    def id(self) -> str:
        return uid("7a100000", self.n)


@dataclass(eq=False)
class Revalorizacion:
    n: int
    fecha: dt.datetime
    nutricionista: int
    revisada: Valoracion
    generada: Valoracion | None

    @property
    def id(self) -> str:
        return uid("7e7a0000", self.n)


# Cómo responde cada dieta al nivel calórico de sus menús: (resultado base, pendiente por punto de nivel medio).
# Con esto la consulta 6a muestra correlaciones de signo distinto según la dieta.
RESPUESTA_DIETA = {1: (3.6, 0.7), 2: (3.4, 0.5), 3: (3.2, 1.2), 4: (3.5, 0.3), 5: (3.8, -0.9), 6: (3.9, -1.3),
                   7: (3.4, 0.4)}
ACEPTACION_NIVEL = {"Bajo": 0.86, "Medio": 0.78, "Alto": 0.66}


def construir_flujo(menus: list[Menu], pacientes: list[Paciente]):
    asignaciones: list[tuple[Menu, Paciente]] = []
    servicios: list[tuple[Menu, Paciente, dt.date]] = []
    consumos: list[tuple[Menu, Paciente, dt.date, Item, bool]] = []
    valoraciones: list[Valoracion] = []
    revalorizaciones: list[Revalorizacion] = []
    n_val = 0

    for menu in [m for m in menus if m.estado == "aprobado"]:
        inscritos = [p for p in pacientes if menu.dieta.n in p.dietas]
        if not inscritos:
            continue
        cuantos = min(len(inscritos), RNG.randint(3, 6))
        x = sum(RANGO_NIVEL[i.nivel] for i in menu.items) / len(menu.items)
        base, pendiente = RESPUESTA_DIETA[menu.dieta.n]
        assert menu.fecha_validacion is not None  # un menú aprobado siempre tiene validación
        for paciente in RNG.sample(inscritos, cuantos):
            asignaciones.append((menu, paciente))
            dia = menu.fecha_validacion.date() + dt.timedelta(days=RNG.randint(1, 3))
            fechas = []
            for _ in range(RNG.randint(1, 3)):
                if dia > CORTE.date():
                    break
                fechas.append(dia)
                dia += dt.timedelta(days=RNG.randint(2, 4))
            aceptados = total = 0
            for fecha in fechas:
                servicios.append((menu, paciente, fecha))
                for item in menu.items:
                    if RNG.random() < 0.08:
                        continue  # consumo no registrado de ese ítem en ese servicio
                    p = ACEPTACION_NIVEL[item.nivel] + paciente.apetito - (0.3 if item.nombre in POCO_ACEPTADOS else 0)
                    acepta = RNG.random() < p
                    consumos.append((menu, paciente, fecha, item, acepta))
                    aceptados += acepta
                    total += 1
            if not fechas or RNG.random() < 0.15:
                continue  # todavía sin valorar
            tasa = aceptados / total if total else 0.7
            valor = round(base + pendiente * (x - 2) + 2.2 * (tasa - 0.75) + RNG.gauss(0, 0.45))
            valor = max(1, min(5, valor))
            fecha_val = dt.datetime.combine(fechas[-1], dt.time(15, 30), tzinfo=UTC) + dt.timedelta(
                days=RNG.randint(1, 2), minutes=RNG.randint(0, 120))
            if fecha_val > CORTE:
                continue
            valorador = RNG.choice([i for i in menu.dieta.autorizados if not NUTRICIONISTAS[i][2]])
            n_val += 1
            valoracion = Valoracion(n_val, menu, paciente, valorador, valor, fecha_val)
            valoraciones.append(valoracion)
            # Seguimiento: a veces se vuelve a valorar la misma asignación (la consulta 6a usa la más reciente).
            seguimiento = fecha_val + dt.timedelta(days=RNG.randint(4, 8))
            if RNG.random() < 0.15 and seguimiento <= CORTE:
                n_val += 1
                valoraciones.append(Valoracion(n_val, menu, paciente, valorador,
                                               max(1, min(5, valor + RNG.choice([-1, 1, 1]))), seguimiento))

    # Revalorizaciones: los pacientes piden revisar las valoraciones malas. Unas ya están atendidas.
    malas = [v for v in valoraciones if v.valor <= 2]
    RNG.shuffle(malas)
    n_rev = 0
    for k, revisada in enumerate(malas[:10]):
        solicitud = revisada.fecha + dt.timedelta(days=1, hours=RNG.randint(1, 5))
        if solicitud > CORTE:
            continue
        otros = [i for i in revisada.menu.dieta.autorizados if i != revisada.nutricionista]
        ejecutor = otros[k % len(otros)]
        generada = None
        atencion = solicitud + dt.timedelta(days=1, hours=RNG.randint(1, 4))
        if k % 3 != 2 and atencion <= CORTE:
            n_val += 1
            generada = Valoracion(n_val, revisada.menu, revisada.paciente, ejecutor,
                                  min(5, revisada.valor + RNG.choice([1, 1, 2])), atencion)
            valoraciones.append(generada)
        n_rev += 1
        revalorizaciones.append(Revalorizacion(n_rev, solicitud, ejecutor, revisada, generada))

    return asignaciones, servicios, consumos, valoraciones, revalorizaciones


# ---------------------------------------------------------------------------------------------------------------
# Verificación: las reglas de la base, comprobadas antes de escribir
# ---------------------------------------------------------------------------------------------------------------


def verificar(banco, pacientes, menus, asignaciones, servicios, consumos, valoraciones, revalorizaciones):
    nombres = [i.nombre.strip().lower() for i in banco]
    assert len(nombres) == len(set(nombres)), "RC-01: nombres de ítems repetidos"
    assert all(p.edad >= 0 for p in pacientes), "RD-06: edad negativa"
    assert all(d in range(1, len(DIETAS) + 1) for p in pacientes for d in p.dietas)
    for plato in [i for i in banco if i.clase == "Plato"]:
        assert plato.composicion, f"RK-03: {plato.nombre} sin alimentos"
        assert all(a.clase == "Alimento" for a, _ in plato.composicion), "RS-12"
    for dieta in DIETAS:
        assert dieta.requeridos, "RK-07"
        assert not (set(dieta.requeridos) & dieta.prohibidos()), f"RS-11: {dieta.nombre}"
        assert len(jefes_de(dieta)) >= 1, f"{dieta.nombre} sin jefe autorizado"
    for m in menus:
        assert m.fecha <= CORTE
        assert m.creador in m.dieta.autorizados, "RS-03 creador"
        assert abs(sum(m.distribucion.values()) - 1) <= Decimal("0.001"), "RS-08"
        assert len({i.id for i in m.items}) == len(m.items), "ítem repetido en un menú"
        if m.revisor is not None:
            assert NUTRICIONISTAS[m.revisor][2] and m.revisor in m.dieta.autorizados, "RS-03 revisor"
            assert m.revisor != m.creador, "RS-05"
            assert m.fecha_validacion >= m.fecha and m.fecha_validacion <= CORTE, "RS-06"
        if m.origen is not None:
            assert m.origen.estado == "rechazado" and m.origen.dieta is m.dieta, "RS-17/RS-18"
    for menu, paciente in asignaciones:
        assert menu.dieta.n in paciente.dietas and menu.estado == "aprobado", "RS-04/RS-16"
    for menu, _, fecha in servicios:
        assert menu.fecha_validacion.date() <= fecha <= CORTE.date(), "RS-14"
    for menu, _, _, item, _ in consumos:
        assert item in menu.items
    for v in valoraciones:
        assert (v.menu, v.paciente) in set(asignaciones) and v.fecha <= CORTE
    for r in revalorizaciones:
        if r.generada:
            assert (r.generada.menu, r.generada.paciente) == (r.revisada.menu, r.revisada.paciente), "RS-10"
            assert r.generada.nutricionista == r.nutricionista, "RS-10 ejecutor"


# ---------------------------------------------------------------------------------------------------------------
# Escritura del SQL
# ---------------------------------------------------------------------------------------------------------------


def q(texto: str | None) -> str:
    return "NULL" if texto is None else "'" + texto.replace("'", "''") + "'"


def ts(momento: dt.datetime) -> str:
    return f"'{momento.astimezone(UTC):%Y-%m-%d %H:%M:%S}+00'"


def correo(nombre: str, sufijo: str = "") -> str:
    plano = unicodedata.normalize("NFKD", nombre).encode("ascii", "ignore").decode().lower()
    partes = plano.split()
    return f"{partes[0]}.{partes[-2] if len(partes) > 2 else partes[-1]}{sufijo}@{DOMINIO_CORREO}"


def nut_id(i: int) -> str:
    return uid("0a7e0000", i + 1)


def sala_id(i: int) -> str:
    return uid("5a1a0000", i + 1)


def insert(tabla: str, columnas: str, filas: list[str]) -> str:
    if not filas:
        return ""
    return f"INSERT INTO {tabla} ({columnas}) VALUES\n    " + ",\n    ".join(filas) + ";\n"


def escribir_sql(banco, pacientes, menus, asignaciones, servicios, consumos, valoraciones, revalorizaciones):
    s: list[str] = []
    s.append(f"""-- Datos de ejemplo de Menús Nutricionales (#284). GENERADO por scripts/datos-ejemplo/generar.py: no se
-- edita a mano; se cambia el generador y se vuelve a generar.
--
-- Requisito: la base con todas las migraciones aplicadas. Todo va en una sola transacción: si una regla de la base
-- rechaza algo, no queda nada a medias. Las reglas diferidas (RK-03, RK-07, RK-09, RS-14, RS-16) se comprueban en
-- el COMMIT.
--
-- Contraseña de todos los usuarios de ejemplo: {CONTRASENA_DEMO}  (solo para entornos de demostración)

\\set ON_ERROR_STOP on

BEGIN;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM salas WHERE id = '{sala_id(0)}') THEN
        RAISE EXCEPTION 'Los datos de ejemplo ya están cargados. Para recargarlos, recree la base (README).';
    END IF;
END $$;
""")
    s.append("\n-- Catálogo: qué grupos excluye cada restricción (Excluye)\n")
    s.append(insert("excluye", "restriccion_alimentaria_id, grupo_nutricional_id",
                    [f"('{RESTRICCIONES[r]}', '{GRUPOS[g]}')" for r, g in EXCLUYE]))

    s.append("\n-- Salas\n")
    s.append(insert("salas", "id, nombre", [f"('{sala_id(i)}', {q(n)})" for i, n in enumerate(SALAS)]))

    s.append("\n-- Nutricionistas (los tres primeros son jefes de nutrición)\n")
    s.append(insert("nutricionistas", "id, nombre, especialidad_id, es_jefe",
                    [f"('{nut_id(i)}', {q(n)}, '{ESPECIALIDADES[e]}', {str(j).lower()})"
                     for i, (n, e, j) in enumerate(NUTRICIONISTAS)]))

    s.append("\n-- Dietas, con los grupos que cubren (RK-07: al menos uno requerido), sus restricciones y quién puede\n"
             "-- crear y revisar sus menús (Autoriza)\n")
    s.append(insert("dietas", "id, nombre, programa_atencion_id",
                    [f"('{d.id}', {q(d.nombre)}, '{PROGRAMAS[d.programa]}')" for d in DIETAS]))
    s.append(insert("cubre_dieta", "dieta_id, grupo_nutricional_id, tipo",
                    [f"('{d.id}', '{GRUPOS[g]}', 'Requerido')" for d in DIETAS for g in d.requeridos]
                    + [f"('{d.id}', '{GRUPOS[g]}', 'Restringido')" for d in DIETAS for g in d.restringidos]))
    s.append(insert("restringe", "dieta_id, restriccion_alimentaria_id",
                    [f"('{d.id}', '{RESTRICCIONES[r]}')" for d in DIETAS for r in d.restricciones]))
    s.append(insert("autoriza", "nutricionista_id, dieta_id",
                    [f"('{nut_id(i)}', '{d.id}')" for d in DIETAS for i in d.autorizados]))

    s.append("\n-- Pacientes y su inscripción en dietas\n")
    s.append(insert("pacientes", "id, nombre, edad, sala_id",
                    [f"('{p.id}', {q(p.nombre)}, {p.edad}, '{sala_id(p.sala)}')" for p in pacientes]))
    s.append(insert("paciente_dieta", "paciente_id, dieta_id",
                    [f"('{p.id}', '{DIETAS[d - 1].id}')" for p in pacientes for d in p.dietas]))

    s.append("\n-- Banco de alimentos y platos, y la composición de cada plato (RK-03: al menos un alimento)\n")
    s.append(insert("items_alimentarios",
                    "id, nombre, grupo_nutricional_id, tipo_preparacion_id, nivel_calorico_id, nutricionista_id, clase",
                    [f"('{i.id}', {q(i.nombre)}, '{GRUPOS[i.grupo]}', '{TIPOS[i.tipo]}', '{NIVELES[i.nivel]}', "
                     f"'{nut_id(i.registra)}', '{i.clase}')" for i in banco]))
    s.append(insert("composicion", "plato_id, alimento_id, cantidad",
                    [f"('{p.id}', '{a.id}', {g})" for p in banco if p.clase == "Plato" for a, g in p.composicion]))

    s.append("\n-- Menús en orden cronológico: cada uno con sus parámetros (RS-07: en la misma transacción), sus ítems y,\n"
             "-- si ya tiene, su dictamen. Un reemplazo va después de la validación que lo origina.\n")
    for m in sorted(menus, key=lambda m: (m.fecha, m.n)):
        estado = {"aprobado": "aprobado", "rechazado": "rechazado", "pendiente": "pendiente de revisión"}[m.estado]
        s.append(f"\n-- Menú {m.n}: {m.dieta.nombre}, {'automático' if m.automatico else 'manual'}, {estado}"
                 + (f", reemplaza al menú {m.origen.n}" if m.origen else "") + "\n")
        s.append(insert("menus", "id, dieta_id, nutricionista_creador_id, fecha_creacion, cantidad_total_alimentos, "
                                 "es_automatico, validacion_origen_id",
                        [f"('{m.id}', '{m.dieta.id}', '{nut_id(m.creador)}', {ts(m.fecha)}, {m.cantidad}, "
                         f"{str(m.automatico).lower()}, {q(m.origen.id) if m.origen else 'NULL'})"]))
        s.append(insert("menu_distribucion", "menu_id, tipo_preparacion_id, proporcion",
                        [f"('{m.id}', '{TIPOS[t]}', {p})" for t, p in m.distribucion.items()]))
        s.append(insert("menu_grupo_nutricional", "menu_id, grupo_nutricional_id",
                        [f"('{m.id}', '{GRUPOS[g]}')" for g in m.grupos]))
        s.append(insert("menu_item", "menu_id, item_id", [f"('{m.id}', '{i.id}')" for i in m.items]))
        if m.revisor is not None:
            s.append(insert("validacion", "menu_id, nutricionista_id, fecha_validacion, aprobado, observaciones",
                            [f"('{m.id}', '{nut_id(m.revisor)}', {ts(m.fecha_validacion)}, "
                             f"{str(m.estado == 'aprobado').lower()}, {q(m.observaciones)})"]))

    s.append("\n-- Asignación de los menús aprobados a pacientes de su dieta (RS-04, RS-16)\n")
    s.append(insert("asignacion", "menu_id, paciente_id", [f"('{m.id}', '{p.id}')" for m, p in asignaciones]))
    s.append("\n-- Servicios, siempre después de la aprobación del menú (RS-14)\n")
    s.append(insert("servicios", "menu_id, paciente_id, fecha_servicio",
                    [f"('{m.id}', '{p.id}', '{f:%Y-%m-%d}')" for m, p, f in servicios]))
    s.append("\n-- Consumo de cada ítem servido, con su aceptación\n")
    s.append(insert("consumo", "menu_id, paciente_id, fecha_servicio, item_id, aceptacion",
                    [f"('{m.id}', '{p.id}', '{f:%Y-%m-%d}', '{i.id}', {str(a).lower()})" for m, p, f, i, a in consumos]))
    s.append("\n-- Valoraciones nutricionales de cada asignación\n")
    s.append(insert("valoraciones_nutricionales",
                    "id, menu_id, paciente_id, nutricionista_id, resultado_nutricional_id, fecha_valoracion",
                    [f"('{v.id}', '{v.menu.id}', '{v.paciente.id}', '{nut_id(v.nutricionista)}', "
                     f"'{RESULTADOS[v.valor][1]}', {ts(v.fecha)})" for v in valoraciones]))
    s.append("\n-- Revalorizaciones: las atendidas apuntan a la valoración que generaron (RS-10); las demás, pendientes\n")
    s.append(insert("revalorizaciones", "id, fecha_solicitud, nutricionista_id, valoracion_revisada_id, "
                                        "valoracion_generada_id",
                    [f"('{r.id}', {ts(r.fecha)}, '{nut_id(r.nutricionista)}', '{r.revisada.id}', "
                     f"{q(r.generada.id) if r.generada else 'NULL'})" for r in revalorizaciones]))

    s.append(f"\n-- Usuarios para entrar a la aplicación. Contraseña de todos: {CONTRASENA_DEMO}\n")
    usuarios = [f"('{uid('05e70000', 1)}', 'admin@{DOMINIO_CORREO}', '{HASH_DEMO}', 'Admin', NULL)"]
    correos = {f"admin@{DOMINIO_CORREO}"}
    k = 1
    for i, (nombre, _, _) in enumerate(NUTRICIONISTAS):
        k += 1
        c = correo(nombre)
        correos.add(c)
        usuarios.append(f"('{uid('05e70000', k)}', '{c}', '{HASH_DEMO}', 'Nutricionista', '{nut_id(i)}')")
    for p in pacientes:
        k += 1
        c = correo(p.nombre)
        sufijo = 2
        while c in correos:
            c = correo(p.nombre, str(sufijo))
            sufijo += 1
        correos.add(c)
        usuarios.append(f"('{uid('05e70000', k)}', '{c}', '{HASH_DEMO}', 'Paciente', '{p.id}')")
    s.append(insert("usuarios", "id, email, password_hash, rol, entidad_id", usuarios))

    s.append("\nCOMMIT;\n")
    (CARPETA / "datos-ejemplo.sql").write_text("".join(s), encoding="utf-8")
    return sorted(correos)


# ---------------------------------------------------------------------------------------------------------------
# Escritura de las fichas de MongoDB
# ---------------------------------------------------------------------------------------------------------------

VITAMINAS = {"Frutas": {"cMg": 30}, "Verduras": {"aUg": 250, "cMg": 18}, "Lácteos": {"dUg": 1.2, "b12Ug": 0.4},
             "Proteínas": {"b12Ug": 1.1, "b6Mg": 0.4}, "Cereales": {"b1Mg": 0.2, "b3Mg": 2.1}}
MINERALES = {"Frutas": {"potasioMg": 180}, "Verduras": {"potasioMg": 290, "hierroMg": 1.1},
             "Lácteos": {"calcioMg": 120}, "Proteínas": {"hierroMg": 1.8, "zincMg": 2.4},
             "Cereales": {"magnesioMg": 35, "fosforoMg": 110}}


def r1(x: float) -> float:
    return round(x, 1)


def ficha(item: Item, fecha: dt.datetime) -> dict | None:
    # La ficha es opcional: algunos ítems no la tienen, para que se vea en la UI y en la conciliación.
    if item.clase == "Alimento" and item.n % 9 == 0 or item.clase == "Plato" and item.n % 11 == 0:
        return None
    macros = {"energiaKcal": r1(item.kcal), "proteinasG": r1(item.proteinas),
              "carbohidratosG": r1(item.carbohidratos), "grasasG": r1(item.grasas)}
    if item.fibra:
        macros["fibraG"] = r1(item.fibra)
    doc: dict = {"origen": {"tabla": item.clase, "id": item.id},
                 "tipo": "simple" if item.clase == "Alimento" else "compuesto"}
    if item.clase == "Alimento":
        doc["porcionReferencia"] = {"cantidad": 100, "unidad": "g"}
        doc["macronutrientes"] = macros
        if item.n % 2 == 0:  # solo algunos alimentos tienen micronutrientes registrados
            doc["micronutrientes"] = {"vitaminas": VITAMINAS[item.grupo],
                                      "minerales": {**MINERALES[item.grupo], "sodioMg": item.sodio}}
        if item.n % 4 != 3:  # ausente = no registrado; [] = registrado sin alérgenos
            doc["alergenos"] = item.alergenos
    else:
        gramos = sum(g for _, g in item.composicion)
        doc["porcionReferencia"] = {"cantidad": 1, "unidad": f"ración ({gramos} g)"}
        doc["macronutrientes"] = macros
        doc["micronutrientes"] = {"minerales": {"sodioMg": r1(item.sodio)}}
        doc["alergenos"] = item.alergenos
        doc["modoPreparacion"] = {
            "tecnica": item.tecnica,
            "tiempoTotalMin": sum(m for _, m in item.pasos),
            "pasos": [{"orden": k + 1, "descripcion": p, "tiempoMin": m} for k, (p, m) in enumerate(item.pasos)],
        }
        doc["ingredientes"] = [
            {"alimentoId": a.id, "nombre": a.nombre,
             "aporte": {"energiaKcal": r1(a.kcal * g / 100), "proteinasG": r1(a.proteinas * g / 100),
                        "carbohidratosG": r1(a.carbohidratos * g / 100)}}
            for a, g in item.composicion]
    doc["actualizadoEn"] = fecha
    return doc


def escribir_mongo(banco: list[Item]) -> int:
    fecha = dt.datetime(2026, 10, 3, 12, 0, tzinfo=UTC)
    docs = []
    for item in banco:
        d = ficha(item, fecha + dt.timedelta(minutes=7 * item.n))
        if d:
            docs.append(d)
    cuerpo = json.dumps(docs, ensure_ascii=False, indent=2,
                        default=lambda o: {"$date": o.astimezone(UTC).strftime("%Y-%m-%dT%H:%M:%SZ")})
    js = f"""// Fichas nutricionales de ejemplo (#284). GENERADO por scripts/datos-ejemplo/generar.py: no se edita a mano.
//
// Uso: mongosh "<cadena de conexión a la base de fichas>" fichas-nutricionales.js
// Es idempotente: reemplaza la ficha de cada ítem por su origen (tabla + id), así que se puede repetir.
//
// Si la colección no existe, la crea con el mismo validador $jsonSchema y el mismo índice único que
// MenusMongoContext.InicializarAsync (src/Menus.Infrastructure/Mongo/FichaNutricionalEsquema.cs). Si cambia el
// esquema allí, hay que cambiarlo aquí.

const COLECCION = "fichas_nutricionales";
const ID = "^[0-9a-f]{{8}}-[0-9a-f]{{4}}-[0-9a-f]{{4}}-[0-9a-f]{{4}}-[0-9a-f]{{12}}$";
const validador = {{
  $jsonSchema: {{
    bsonType: "object",
    required: ["origen", "tipo", "actualizadoEn"],
    properties: {{
      origen: {{ bsonType: "object", required: ["tabla", "id"],
        properties: {{ tabla: {{ enum: ["Alimento", "Plato"] }}, id: {{ bsonType: "string", pattern: ID }} }} }},
      tipo: {{ enum: ["simple", "compuesto"] }},
      porcionReferencia: {{ bsonType: "object", required: ["cantidad", "unidad"],
        properties: {{ cantidad: {{ bsonType: ["int", "double"], minimum: 0, exclusiveMinimum: true }} }} }},
      macronutrientes: {{ bsonType: "object" }},
      micronutrientes: {{ bsonType: "object" }},
      alergenos: {{ bsonType: "array", items: {{ bsonType: "string" }}, uniqueItems: true }},
      modoPreparacion: {{ bsonType: "object" }},
      ingredientes: {{ bsonType: "array", items: {{ bsonType: "object", required: ["alimentoId"],
        properties: {{ alimentoId: {{ bsonType: "string", pattern: ID }} }} }} }},
      actualizadoEn: {{ bsonType: "date" }}
    }},
    anyOf: [
      {{ properties: {{ origen: {{ properties: {{ tabla: {{ enum: ["Alimento"] }} }} }}, tipo: {{ enum: ["simple"] }} }},
        not: {{ required: ["ingredientes"] }} }},
      {{ properties: {{ origen: {{ properties: {{ tabla: {{ enum: ["Plato"] }} }} }}, tipo: {{ enum: ["compuesto"] }} }} }}
    ]
  }}
}};

if (!db.getCollectionNames().includes(COLECCION)) {{
  db.createCollection(COLECCION, {{ validator: validador, validationLevel: "strict", validationAction: "error" }});
}}
db.getCollection(COLECCION).createIndex({{ "origen.tabla": 1, "origen.id": 1 }}, {{ unique: true, name: "ux_fichas_origen" }});

const fichas = EJSON.deserialize({cuerpo});

let escritas = 0;
for (const f of fichas) {{
  db.getCollection(COLECCION).replaceOne({{ "origen.tabla": f.origen.tabla, "origen.id": f.origen.id }}, f, {{ upsert: true }});
  escritas++;
}}
print(`Fichas nutricionales escritas: ${{escritas}}`);
"""
    (CARPETA / "fichas-nutricionales.js").write_text(js, encoding="utf-8")
    return len(docs)


def main() -> None:
    banco = construir_banco()
    pacientes = construir_pacientes()
    menus = construir_menus(banco)
    flujo = construir_flujo(menus, pacientes)
    verificar(banco, pacientes, menus, *flujo)
    asignaciones, servicios, consumos, valoraciones, revalorizaciones = flujo
    correos = escribir_sql(banco, pacientes, menus, *flujo)
    fichas = escribir_mongo(banco)
    estados = {e: sum(m.estado == e for m in menus) for e in ("aprobado", "rechazado", "pendiente")}
    print(f"Salas {len(SALAS)} · nutricionistas {len(NUTRICIONISTAS)} · dietas {len(DIETAS)} · "
          f"pacientes {len(pacientes)}")
    print(f"Alimentos {sum(i.clase == 'Alimento' for i in banco)} · platos {sum(i.clase == 'Plato' for i in banco)} · "
          f"fichas {fichas}")
    print(f"Menús {len(menus)} ({estados['aprobado']} aprobados, {estados['rechazado']} rechazados, "
          f"{estados['pendiente']} pendientes; {sum(m.origen is not None for m in menus)} reemplazos)")
    print(f"Asignaciones {len(asignaciones)} · servicios {len(servicios)} · consumos {len(consumos)} · "
          f"valoraciones {len(valoraciones)} · revalorizaciones {len(revalorizaciones)} "
          f"({sum(r.generada is not None for r in revalorizaciones)} atendidas)")
    print(f"Usuarios {len(correos)}")


if __name__ == "__main__":
    main()
