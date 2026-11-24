// Fichas nutricionales de ejemplo (#284). GENERADO por scripts/datos-ejemplo/generar.py: no se edita a mano.
//
// Uso: mongosh "<cadena de conexión a la base de fichas>" fichas-nutricionales.js
// Es idempotente: reemplaza la ficha de cada ítem por su origen (tabla + id), así que se puede repetir.
//
// Si la colección no existe, la crea con el mismo validador $jsonSchema y el mismo índice único que
// MenusMongoContext.InicializarAsync (src/Menus.Infrastructure/Mongo/FichaNutricionalEsquema.cs). Si cambia el
// esquema allí, hay que cambiarlo aquí.

const COLECCION = "fichas_nutricionales";
const ID = "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";
const validador = {
  $jsonSchema: {
    bsonType: "object",
    required: ["origen", "tipo", "actualizadoEn"],
    properties: {
      origen: { bsonType: "object", required: ["tabla", "id"],
        properties: { tabla: { enum: ["Alimento", "Plato"] }, id: { bsonType: "string", pattern: ID } } },
      tipo: { enum: ["simple", "compuesto"] },
      porcionReferencia: { bsonType: "object", required: ["cantidad", "unidad"],
        properties: { cantidad: { bsonType: ["int", "double"], minimum: 0, exclusiveMinimum: true } } },
      macronutrientes: { bsonType: "object" },
      micronutrientes: { bsonType: "object" },
      alergenos: { bsonType: "array", items: { bsonType: "string" }, uniqueItems: true },
      modoPreparacion: { bsonType: "object" },
      ingredientes: { bsonType: "array", items: { bsonType: "object", required: ["alimentoId"],
        properties: { alimentoId: { bsonType: "string", pattern: ID } } } },
      actualizadoEn: { bsonType: "date" }
    },
    anyOf: [
      { properties: { origen: { properties: { tabla: { enum: ["Alimento"] } } }, tipo: { enum: ["simple"] } },
        not: { required: ["ingredientes"] } },
      { properties: { origen: { properties: { tabla: { enum: ["Plato"] } } }, tipo: { enum: ["compuesto"] } } }
    ]
  }
};

if (!db.getCollectionNames().includes(COLECCION)) {
  db.createCollection(COLECCION, { validator: validador, validationLevel: "strict", validationAction: "error" });
}
db.getCollection(COLECCION).createIndex({ "origen.tabla": 1, "origen.id": 1 }, { unique: true, name: "ux_fichas_origen" });

const fichas = EJSON.deserialize([
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000001"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 130,
      "proteinasG": 2.7,
      "carbohidratosG": 28.2,
      "grasasG": 0.3,
      "fibraG": 0.4
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T12:07:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000002"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 123,
      "proteinasG": 2.7,
      "carbohidratosG": 25.6,
      "grasasG": 1.0,
      "fibraG": 1.6
    },
    "micronutrientes": {
      "vitaminas": {
        "b1Mg": 0.2,
        "b3Mg": 2.1
      },
      "minerales": {
        "magnesioMg": 35,
        "fosforoMg": 110,
        "sodioMg": 4
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T12:14:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000003"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 265,
      "proteinasG": 9.0,
      "carbohidratosG": 49.0,
      "grasasG": 3.2,
      "fibraG": 2.7
    },
    "actualizadoEn": {
      "$date": "2026-10-03T12:21:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000004"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 247,
      "proteinasG": 13.0,
      "carbohidratosG": 41.0,
      "grasasG": 3.4,
      "fibraG": 7.0
    },
    "micronutrientes": {
      "vitaminas": {
        "b1Mg": 0.2,
        "b3Mg": 2.1
      },
      "minerales": {
        "magnesioMg": 35,
        "fosforoMg": 110,
        "sodioMg": 450
      }
    },
    "alergenos": [
      "gluten"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T12:28:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000005"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 421,
      "proteinasG": 9.5,
      "carbohidratosG": 71.0,
      "grasasG": 10.0,
      "fibraG": 2.5
    },
    "alergenos": [
      "gluten"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T12:35:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000006"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 158,
      "proteinasG": 5.8,
      "carbohidratosG": 31.0,
      "grasasG": 0.9,
      "fibraG": 1.8
    },
    "micronutrientes": {
      "vitaminas": {
        "b1Mg": 0.2,
        "b3Mg": 2.1
      },
      "minerales": {
        "magnesioMg": 35,
        "fosforoMg": 110,
        "sodioMg": 1
      }
    },
    "alergenos": [
      "gluten"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T12:42:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000007"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 389,
      "proteinasG": 16.9,
      "carbohidratosG": 66.3,
      "grasasG": 6.9,
      "fibraG": 10.6
    },
    "actualizadoEn": {
      "$date": "2026-10-03T12:49:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000008"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 361,
      "proteinasG": 6.9,
      "carbohidratosG": 76.8,
      "grasasG": 3.9,
      "fibraG": 7.3
    },
    "micronutrientes": {
      "vitaminas": {
        "b1Mg": 0.2,
        "b3Mg": 2.1
      },
      "minerales": {
        "magnesioMg": 35,
        "fosforoMg": 110,
        "sodioMg": 5
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T12:56:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000010"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 86,
      "proteinasG": 1.6,
      "carbohidratosG": 20.1,
      "grasasG": 0.1,
      "fibraG": 3.0
    },
    "micronutrientes": {
      "vitaminas": {
        "b1Mg": 0.2,
        "b3Mg": 2.1
      },
      "minerales": {
        "magnesioMg": 35,
        "fosforoMg": 110,
        "sodioMg": 55
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T13:10:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000011"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 160,
      "proteinasG": 1.4,
      "carbohidratosG": 38.1,
      "grasasG": 0.3,
      "fibraG": 1.8
    },
    "actualizadoEn": {
      "$date": "2026-10-03T13:17:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000012"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 122,
      "proteinasG": 1.3,
      "carbohidratosG": 31.9,
      "grasasG": 0.4,
      "fibraG": 2.3
    },
    "micronutrientes": {
      "vitaminas": {
        "b1Mg": 0.2,
        "b3Mg": 2.1
      },
      "minerales": {
        "magnesioMg": 35,
        "fosforoMg": 110,
        "sodioMg": 4
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T13:24:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000013"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 96,
      "proteinasG": 3.4,
      "carbohidratosG": 21.0,
      "grasasG": 1.5,
      "fibraG": 2.4
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T13:31:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000014"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 120,
      "proteinasG": 4.4,
      "carbohidratosG": 21.3,
      "grasasG": 1.9,
      "fibraG": 2.8
    },
    "micronutrientes": {
      "vitaminas": {
        "b1Mg": 0.2,
        "b3Mg": 2.1
      },
      "minerales": {
        "magnesioMg": 35,
        "fosforoMg": 110,
        "sodioMg": 7
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T13:38:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000015"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 165,
      "proteinasG": 31.0,
      "carbohidratosG": 0.0,
      "grasasG": 3.6
    },
    "actualizadoEn": {
      "$date": "2026-10-03T13:45:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000016"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 209,
      "proteinasG": 26.0,
      "carbohidratosG": 0.0,
      "grasasG": 10.9
    },
    "micronutrientes": {
      "vitaminas": {
        "b12Ug": 1.1,
        "b6Mg": 0.4
      },
      "minerales": {
        "hierroMg": 1.8,
        "zincMg": 2.4,
        "sodioMg": 95
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T13:52:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000017"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 96,
      "proteinasG": 20.0,
      "carbohidratosG": 0.0,
      "grasasG": 1.7
    },
    "alergenos": [
      "pescado"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T13:59:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000019"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 155,
      "proteinasG": 12.6,
      "carbohidratosG": 1.1,
      "grasasG": 10.6
    },
    "actualizadoEn": {
      "$date": "2026-10-03T14:13:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000020"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 52,
      "proteinasG": 10.9,
      "carbohidratosG": 0.7,
      "grasasG": 0.2
    },
    "micronutrientes": {
      "vitaminas": {
        "b12Ug": 1.1,
        "b6Mg": 0.4
      },
      "minerales": {
        "hierroMg": 1.8,
        "zincMg": 2.4,
        "sodioMg": 166
      }
    },
    "alergenos": [
      "huevo"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T14:20:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000021"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 187,
      "proteinasG": 27.0,
      "carbohidratosG": 0.0,
      "grasasG": 8.0
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T14:27:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000022"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 242,
      "proteinasG": 27.3,
      "carbohidratosG": 0.0,
      "grasasG": 13.9
    },
    "micronutrientes": {
      "vitaminas": {
        "b12Ug": 1.1,
        "b6Mg": 0.4
      },
      "minerales": {
        "hierroMg": 1.8,
        "zincMg": 2.4,
        "sodioMg": 62
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T14:34:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000023"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 104,
      "proteinasG": 17.0,
      "carbohidratosG": 3.8,
      "grasasG": 2.3
    },
    "actualizadoEn": {
      "$date": "2026-10-03T14:41:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000024"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 132,
      "proteinasG": 8.9,
      "carbohidratosG": 23.7,
      "grasasG": 0.5,
      "fibraG": 8.7
    },
    "micronutrientes": {
      "vitaminas": {
        "b12Ug": 1.1,
        "b6Mg": 0.4
      },
      "minerales": {
        "hierroMg": 1.8,
        "zincMg": 2.4,
        "sodioMg": 1
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T14:48:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000025"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 116,
      "proteinasG": 9.0,
      "carbohidratosG": 20.1,
      "grasasG": 0.4,
      "fibraG": 7.9
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T14:55:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000026"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 164,
      "proteinasG": 8.9,
      "carbohidratosG": 27.4,
      "grasasG": 2.6,
      "fibraG": 7.6
    },
    "micronutrientes": {
      "vitaminas": {
        "b12Ug": 1.1,
        "b6Mg": 0.4
      },
      "minerales": {
        "hierroMg": 1.8,
        "zincMg": 2.4,
        "sodioMg": 7
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T15:02:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000028"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 99,
      "proteinasG": 24.0,
      "carbohidratosG": 0.2,
      "grasasG": 0.3
    },
    "micronutrientes": {
      "vitaminas": {
        "b12Ug": 1.1,
        "b6Mg": 0.4
      },
      "minerales": {
        "hierroMg": 1.8,
        "zincMg": 2.4,
        "sodioMg": 111
      }
    },
    "alergenos": [
      "crustáceos"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T15:16:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000029"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 61,
      "proteinasG": 3.2,
      "carbohidratosG": 4.8,
      "grasasG": 3.3
    },
    "alergenos": [
      "leche"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T15:23:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000030"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 34,
      "proteinasG": 3.4,
      "carbohidratosG": 5.0,
      "grasasG": 0.1
    },
    "micronutrientes": {
      "vitaminas": {
        "dUg": 1.2,
        "b12Ug": 0.4
      },
      "minerales": {
        "calcioMg": 120,
        "sodioMg": 42
      }
    },
    "alergenos": [
      "leche"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T15:30:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000031"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 61,
      "proteinasG": 3.5,
      "carbohidratosG": 4.7,
      "grasasG": 3.3
    },
    "actualizadoEn": {
      "$date": "2026-10-03T15:37:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000032"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 99,
      "proteinasG": 3.0,
      "carbohidratosG": 17.0,
      "grasasG": 2.0
    },
    "micronutrientes": {
      "vitaminas": {
        "dUg": 1.2,
        "b12Ug": 0.4
      },
      "minerales": {
        "calcioMg": 120,
        "sodioMg": 50
      }
    },
    "alergenos": [
      "leche"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T15:44:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000033"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 264,
      "proteinasG": 18.0,
      "carbohidratosG": 4.1,
      "grasasG": 20.0
    },
    "alergenos": [
      "leche"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T15:51:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000034"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 356,
      "proteinasG": 25.0,
      "carbohidratosG": 2.2,
      "grasasG": 27.4
    },
    "micronutrientes": {
      "vitaminas": {
        "dUg": 1.2,
        "b12Ug": 0.4
      },
      "minerales": {
        "calcioMg": 120,
        "sodioMg": 819
      }
    },
    "alergenos": [
      "leche"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T15:58:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000035"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 98,
      "proteinasG": 11.1,
      "carbohidratosG": 3.4,
      "grasasG": 4.3
    },
    "actualizadoEn": {
      "$date": "2026-10-03T16:05:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000037"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 122,
      "proteinasG": 4.1,
      "carbohidratosG": 18.0,
      "grasasG": 3.7
    },
    "alergenos": [
      "leche",
      "huevo"
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T16:19:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000038"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 52,
      "proteinasG": 0.3,
      "carbohidratosG": 13.8,
      "grasasG": 0.2,
      "fibraG": 2.4
    },
    "micronutrientes": {
      "vitaminas": {
        "cMg": 30
      },
      "minerales": {
        "potasioMg": 180,
        "sodioMg": 1
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T16:26:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000039"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 89,
      "proteinasG": 1.1,
      "carbohidratosG": 22.8,
      "grasasG": 0.3,
      "fibraG": 2.6
    },
    "actualizadoEn": {
      "$date": "2026-10-03T16:33:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000040"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 68,
      "proteinasG": 2.6,
      "carbohidratosG": 14.3,
      "grasasG": 1.0,
      "fibraG": 5.4
    },
    "micronutrientes": {
      "vitaminas": {
        "cMg": 30
      },
      "minerales": {
        "potasioMg": 180,
        "sodioMg": 2
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T16:40:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000041"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 60,
      "proteinasG": 0.8,
      "carbohidratosG": 15.0,
      "grasasG": 0.4,
      "fibraG": 1.6
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T16:47:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000042"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 43,
      "proteinasG": 0.5,
      "carbohidratosG": 10.8,
      "grasasG": 0.3,
      "fibraG": 1.7
    },
    "micronutrientes": {
      "vitaminas": {
        "cMg": 30
      },
      "minerales": {
        "potasioMg": 180,
        "sodioMg": 8
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T16:54:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000043"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 50,
      "proteinasG": 0.5,
      "carbohidratosG": 13.1,
      "grasasG": 0.1,
      "fibraG": 1.4
    },
    "actualizadoEn": {
      "$date": "2026-10-03T17:01:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000044"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 47,
      "proteinasG": 0.9,
      "carbohidratosG": 11.8,
      "grasasG": 0.1,
      "fibraG": 2.4
    },
    "micronutrientes": {
      "vitaminas": {
        "cMg": 30
      },
      "minerales": {
        "potasioMg": 180,
        "sodioMg": 0
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T17:08:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000046"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 32,
      "proteinasG": 0.7,
      "carbohidratosG": 7.7,
      "grasasG": 0.3,
      "fibraG": 2.0
    },
    "micronutrientes": {
      "vitaminas": {
        "cMg": 30
      },
      "minerales": {
        "potasioMg": 180,
        "sodioMg": 1
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T17:22:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000047"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 45,
      "proteinasG": 0.7,
      "carbohidratosG": 10.4,
      "grasasG": 0.2,
      "fibraG": 0.2
    },
    "actualizadoEn": {
      "$date": "2026-10-03T17:29:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000048"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 54,
      "proteinasG": 0.1,
      "carbohidratosG": 13.1,
      "grasasG": 0.1,
      "fibraG": 0.3
    },
    "micronutrientes": {
      "vitaminas": {
        "cMg": 30
      },
      "minerales": {
        "potasioMg": 180,
        "sodioMg": 4
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T17:36:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000049"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 37,
      "proteinasG": 0.3,
      "carbohidratosG": 9.0,
      "grasasG": 0.1,
      "fibraG": 1.0
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T17:43:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000050"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 40,
      "proteinasG": 0.1,
      "carbohidratosG": 10.6,
      "grasasG": 0.0
    },
    "micronutrientes": {
      "vitaminas": {
        "cMg": 30
      },
      "minerales": {
        "potasioMg": 180,
        "sodioMg": 4
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T17:50:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000051"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 68,
      "proteinasG": 0.2,
      "carbohidratosG": 17.5,
      "grasasG": 0.1,
      "fibraG": 1.1
    },
    "actualizadoEn": {
      "$date": "2026-10-03T17:57:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000052"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 270,
      "proteinasG": 0.4,
      "carbohidratosG": 69.0,
      "grasasG": 0.1,
      "fibraG": 3.0
    },
    "micronutrientes": {
      "vitaminas": {
        "cMg": 30
      },
      "minerales": {
        "potasioMg": 180,
        "sodioMg": 10
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T18:04:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000053"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 299,
      "proteinasG": 3.1,
      "carbohidratosG": 79.2,
      "grasasG": 0.5,
      "fibraG": 3.7
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T18:11:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000055"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 18,
      "proteinasG": 0.9,
      "carbohidratosG": 3.9,
      "grasasG": 0.2,
      "fibraG": 1.2
    },
    "actualizadoEn": {
      "$date": "2026-10-03T18:25:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000056"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 15,
      "proteinasG": 0.7,
      "carbohidratosG": 3.6,
      "grasasG": 0.1,
      "fibraG": 0.5
    },
    "micronutrientes": {
      "vitaminas": {
        "aUg": 250,
        "cMg": 18
      },
      "minerales": {
        "potasioMg": 290,
        "hierroMg": 1.1,
        "sodioMg": 2
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T18:32:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000057"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 41,
      "proteinasG": 0.9,
      "carbohidratosG": 9.6,
      "grasasG": 0.2,
      "fibraG": 2.8
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T18:39:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000058"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 31,
      "proteinasG": 1.8,
      "carbohidratosG": 7.0,
      "grasasG": 0.2,
      "fibraG": 2.7
    },
    "micronutrientes": {
      "vitaminas": {
        "aUg": 250,
        "cMg": 18
      },
      "minerales": {
        "potasioMg": 290,
        "hierroMg": 1.1,
        "sodioMg": 6
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T18:46:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000059"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 26,
      "proteinasG": 1.0,
      "carbohidratosG": 6.5,
      "grasasG": 0.1,
      "fibraG": 0.5
    },
    "actualizadoEn": {
      "$date": "2026-10-03T18:53:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000060"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 34,
      "proteinasG": 2.8,
      "carbohidratosG": 6.6,
      "grasasG": 0.4,
      "fibraG": 2.6
    },
    "micronutrientes": {
      "vitaminas": {
        "aUg": 250,
        "cMg": 18
      },
      "minerales": {
        "potasioMg": 290,
        "hierroMg": 1.1,
        "sodioMg": 33
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T19:00:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000061"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 25,
      "proteinasG": 1.3,
      "carbohidratosG": 5.8,
      "grasasG": 0.1,
      "fibraG": 2.5
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T19:07:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000062"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 25,
      "proteinasG": 1.0,
      "carbohidratosG": 5.9,
      "grasasG": 0.2,
      "fibraG": 3.0
    },
    "micronutrientes": {
      "vitaminas": {
        "aUg": 250,
        "cMg": 18
      },
      "minerales": {
        "potasioMg": 290,
        "hierroMg": 1.1,
        "sodioMg": 2
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T19:14:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000064"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 43,
      "proteinasG": 1.6,
      "carbohidratosG": 9.6,
      "grasasG": 0.2,
      "fibraG": 2.8
    },
    "micronutrientes": {
      "vitaminas": {
        "aUg": 250,
        "cMg": 18
      },
      "minerales": {
        "potasioMg": 290,
        "hierroMg": 1.1,
        "sodioMg": 78
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T19:28:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000065"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 160,
      "proteinasG": 2.0,
      "carbohidratosG": 8.5,
      "grasasG": 14.7,
      "fibraG": 6.7
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T19:35:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Alimento",
      "id": "17e30000-0000-4000-8000-000000000066"
    },
    "tipo": "simple",
    "porcionReferencia": {
      "cantidad": 100,
      "unidad": "g"
    },
    "macronutrientes": {
      "energiaKcal": 33,
      "proteinasG": 1.9,
      "carbohidratosG": 7.5,
      "grasasG": 0.2,
      "fibraG": 3.2
    },
    "micronutrientes": {
      "vitaminas": {
        "aUg": 250,
        "cMg": 18
      },
      "minerales": {
        "potasioMg": 290,
        "hierroMg": 1.1,
        "sodioMg": 7
      }
    },
    "alergenos": [],
    "actualizadoEn": {
      "$date": "2026-10-03T19:42:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000067"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (300 g)"
    },
    "macronutrientes": {
      "energiaKcal": 458.1,
      "proteinasG": 35.5,
      "carbohidratosG": 45.2,
      "grasasG": 13.6,
      "fibraG": 1.4
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 316.2
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "guiso",
      "tiempoTotalMin": 45,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Dorar el pollo troceado.",
          "tiempoMin": 10
        },
        {
          "orden": 2,
          "descripcion": "Sofreír la zanahoria.",
          "tiempoMin": 5
        },
        {
          "orden": 3,
          "descripcion": "Añadir el arroz y el caldo; cocer a fuego lento.",
          "tiempoMin": 30
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000001",
        "nombre": "Arroz blanco",
        "aporte": {
          "energiaKcal": 195.0,
          "proteinasG": 4.0,
          "carbohidratosG": 42.3
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000016",
        "nombre": "Muslo de pollo",
        "aporte": {
          "energiaKcal": 250.8,
          "proteinasG": 31.2,
          "carbohidratosG": 0.0
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000057",
        "nombre": "Zanahoria",
        "aporte": {
          "energiaKcal": 12.3,
          "proteinasG": 0.3,
          "carbohidratosG": 2.9
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T19:49:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000068"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (300 g)"
    },
    "macronutrientes": {
      "energiaKcal": 378.0,
      "proteinasG": 49.4,
      "carbohidratosG": 30.1,
      "grasasG": 5.6,
      "fibraG": 2.7
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 118.5
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "horneado",
      "tiempoTotalMin": 55,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Adobar la pechuga.",
          "tiempoMin": 15
        },
        {
          "orden": 2,
          "descripcion": "Hornear con las papas en cuartos a 200 °C.",
          "tiempoMin": 40
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000015",
        "nombre": "Pechuga de pollo",
        "aporte": {
          "energiaKcal": 247.5,
          "proteinasG": 46.5,
          "carbohidratosG": 0.0
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000009",
        "nombre": "Papa",
        "aporte": {
          "energiaKcal": 130.5,
          "proteinasG": 2.9,
          "carbohidratosG": 30.1
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T19:56:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000069"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (280 g)"
    },
    "macronutrientes": {
      "energiaKcal": 191.7,
      "proteinasG": 32.7,
      "carbohidratosG": 10.1,
      "grasasG": 3.0,
      "fibraG": 3.5
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 177.9
      }
    },
    "alergenos": [
      "pescado"
    ],
    "modoPreparacion": {
      "tecnica": "plancha",
      "tiempoTotalMin": 18,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Cocer al vapor el brócoli y la zanahoria.",
          "tiempoMin": 10
        },
        {
          "orden": 2,
          "descripcion": "Asar el filete a la plancha.",
          "tiempoMin": 8
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000017",
        "nombre": "Filete de pescado blanco",
        "aporte": {
          "energiaKcal": 144.0,
          "proteinasG": 30.0,
          "carbohidratosG": 0.0
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000060",
        "nombre": "Brócoli",
        "aporte": {
          "energiaKcal": 27.2,
          "proteinasG": 2.2,
          "carbohidratosG": 5.3
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000057",
        "nombre": "Zanahoria",
        "aporte": {
          "energiaKcal": 20.5,
          "proteinasG": 0.5,
          "carbohidratosG": 4.8
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T20:03:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000070"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (160 g)"
    },
    "macronutrientes": {
      "energiaKcal": 168.8,
      "proteinasG": 11.1,
      "carbohidratosG": 31.0,
      "grasasG": 0.6,
      "fibraG": 10.6
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 181.6
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "guiso",
      "tiempoTotalMin": 55,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Remojar los frijoles la noche anterior.",
          "tiempoMin": 5
        },
        {
          "orden": 2,
          "descripcion": "Cocer en olla de presión.",
          "tiempoMin": 35
        },
        {
          "orden": 3,
          "descripcion": "Añadir la calabaza y espesar.",
          "tiempoMin": 15
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000024",
        "nombre": "Frijoles negros",
        "aporte": {
          "energiaKcal": 158.4,
          "proteinasG": 10.7,
          "carbohidratosG": 28.4
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000059",
        "nombre": "Calabaza",
        "aporte": {
          "energiaKcal": 10.4,
          "proteinasG": 0.4,
          "carbohidratosG": 2.6
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T20:10:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000071"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (200 g)"
    },
    "macronutrientes": {
      "energiaKcal": 166.0,
      "proteinasG": 11.6,
      "carbohidratosG": 30.6,
      "grasasG": 0.6,
      "fibraG": 10.8
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 210.4
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "guiso",
      "tiempoTotalMin": 40,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Cocer las lentejas.",
          "tiempoMin": 25
        },
        {
          "orden": 2,
          "descripcion": "Añadir las verduras en dados.",
          "tiempoMin": 15
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000025",
        "nombre": "Lentejas",
        "aporte": {
          "energiaKcal": 139.2,
          "proteinasG": 10.8,
          "carbohidratosG": 24.1
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000057",
        "nombre": "Zanahoria",
        "aporte": {
          "energiaKcal": 16.4,
          "proteinasG": 0.4,
          "carbohidratosG": 3.8
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000059",
        "nombre": "Calabaza",
        "aporte": {
          "energiaKcal": 10.4,
          "proteinasG": 0.4,
          "carbohidratosG": 2.6
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T20:17:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000072"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (250 g)"
    },
    "macronutrientes": {
      "energiaKcal": 204.5,
      "proteinasG": 5.4,
      "carbohidratosG": 42.6,
      "grasasG": 1.8,
      "fibraG": 3.6
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 31.5
      }
    },
    "alergenos": [
      "leche"
    ],
    "modoPreparacion": {
      "tecnica": "hervido",
      "tiempoTotalMin": 25,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Hervir las papas peladas.",
          "tiempoMin": 20
        },
        {
          "orden": 2,
          "descripcion": "Majar con la leche caliente.",
          "tiempoMin": 5
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000009",
        "nombre": "Papa",
        "aporte": {
          "energiaKcal": 174.0,
          "proteinasG": 3.8,
          "carbohidratosG": 40.2
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000029",
        "nombre": "Leche entera",
        "aporte": {
          "energiaKcal": 30.5,
          "proteinasG": 1.6,
          "carbohidratosG": 2.4
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T20:24:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000073"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (200 g)"
    },
    "macronutrientes": {
      "energiaKcal": 52.0,
      "proteinasG": 2.0,
      "carbohidratosG": 13.0,
      "grasasG": 0.2,
      "fibraG": 1.0
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 2.0
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "hervido",
      "tiempoTotalMin": 18,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Hervir la calabaza.",
          "tiempoMin": 15
        },
        {
          "orden": 2,
          "descripcion": "Triturar hasta textura fina.",
          "tiempoMin": 3
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000059",
        "nombre": "Calabaza",
        "aporte": {
          "energiaKcal": 52.0,
          "proteinasG": 2.0,
          "carbohidratosG": 13.0
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T20:31:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000074"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (160 g)"
    },
    "macronutrientes": {
      "energiaKcal": 25.8,
      "proteinasG": 1.6,
      "carbohidratosG": 5.6,
      "grasasG": 0.3,
      "fibraG": 1.6
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 18.0
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "crudo",
      "tiempoTotalMin": 10,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Lavar y desinfectar las verduras.",
          "tiempoMin": 5
        },
        {
          "orden": 2,
          "descripcion": "Cortar y aliñar al momento.",
          "tiempoMin": 5
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000054",
        "nombre": "Lechuga",
        "aporte": {
          "energiaKcal": 7.5,
          "proteinasG": 0.7,
          "carbohidratosG": 1.4
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000055",
        "nombre": "Tomate",
        "aporte": {
          "energiaKcal": 10.8,
          "proteinasG": 0.5,
          "carbohidratosG": 2.3
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000056",
        "nombre": "Pepino",
        "aporte": {
          "energiaKcal": 7.5,
          "proteinasG": 0.3,
          "carbohidratosG": 1.8
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T20:38:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000075"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (180 g)"
    },
    "macronutrientes": {
      "energiaKcal": 174.4,
      "proteinasG": 2.7,
      "carbohidratosG": 11.6,
      "grasasG": 14.9,
      "fibraG": 7.7
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 11.0
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "crudo",
      "tiempoTotalMin": 5,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Cortar en cubos y aliñar con limón.",
          "tiempoMin": 5
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000065",
        "nombre": "Aguacate",
        "aporte": {
          "energiaKcal": 160.0,
          "proteinasG": 2.0,
          "carbohidratosG": 8.5
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000055",
        "nombre": "Tomate",
        "aporte": {
          "energiaKcal": 14.4,
          "proteinasG": 0.7,
          "carbohidratosG": 3.1
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T20:45:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000076"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (130 g)"
    },
    "macronutrientes": {
      "energiaKcal": 200.9,
      "proteinasG": 18.2,
      "carbohidratosG": 15.3,
      "grasasG": 7.0,
      "fibraG": 1.6
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 78.1
      }
    },
    "alergenos": [
      "gluten"
    ],
    "modoPreparacion": {
      "tecnica": "hervido",
      "tiempoTotalMin": 40,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Preparar el caldo con el pollo.",
          "tiempoMin": 30
        },
        {
          "orden": 2,
          "descripcion": "Añadir los fideos y la zanahoria.",
          "tiempoMin": 10
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000016",
        "nombre": "Muslo de pollo",
        "aporte": {
          "energiaKcal": 125.4,
          "proteinasG": 15.6,
          "carbohidratosG": 0.0
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000006",
        "nombre": "Pasta corta",
        "aporte": {
          "energiaKcal": 63.2,
          "proteinasG": 2.3,
          "carbohidratosG": 12.4
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000057",
        "nombre": "Zanahoria",
        "aporte": {
          "energiaKcal": 12.3,
          "proteinasG": 0.3,
          "carbohidratosG": 2.9
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T20:52:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000078"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (120 g)"
    },
    "macronutrientes": {
      "energiaKcal": 186.0,
      "proteinasG": 15.1,
      "carbohidratosG": 1.3,
      "grasasG": 12.7
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 328.8
      }
    },
    "alergenos": [
      "huevo"
    ],
    "modoPreparacion": {
      "tecnica": "sartén",
      "tiempoTotalMin": 8,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Batir los huevos.",
          "tiempoMin": 2
        },
        {
          "orden": 2,
          "descripcion": "Cuajar a fuego medio.",
          "tiempoMin": 6
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000019",
        "nombre": "Huevo",
        "aporte": {
          "energiaKcal": 186.0,
          "proteinasG": 15.1,
          "carbohidratosG": 1.3
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T21:06:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000079"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (150 g)"
    },
    "macronutrientes": {
      "energiaKcal": 166.5,
      "proteinasG": 14.0,
      "carbohidratosG": 2.9,
      "grasasG": 10.8,
      "fibraG": 1.1
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 343.5
      }
    },
    "alergenos": [
      "huevo"
    ],
    "modoPreparacion": {
      "tecnica": "sartén",
      "tiempoTotalMin": 10,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Saltear la espinaca.",
          "tiempoMin": 4
        },
        {
          "orden": 2,
          "descripcion": "Añadir el huevo batido y cuajar.",
          "tiempoMin": 6
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000019",
        "nombre": "Huevo",
        "aporte": {
          "energiaKcal": 155.0,
          "proteinasG": 12.6,
          "carbohidratosG": 1.1
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000063",
        "nombre": "Espinaca",
        "aporte": {
          "energiaKcal": 11.5,
          "proteinasG": 1.4,
          "carbohidratosG": 1.8
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T21:13:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000080"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (190 g)"
    },
    "macronutrientes": {
      "energiaKcal": 287.7,
      "proteinasG": 40.9,
      "carbohidratosG": 1.6,
      "grasasG": 12.1,
      "fibraG": 0.5
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 281.0
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "guiso",
      "tiempoTotalMin": 80,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Cocer la carne y deshilacharla.",
          "tiempoMin": 60
        },
        {
          "orden": 2,
          "descripcion": "Guisar en salsa de tomate.",
          "tiempoMin": 20
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000021",
        "nombre": "Carne de res magra",
        "aporte": {
          "energiaKcal": 280.5,
          "proteinasG": 40.5,
          "carbohidratosG": 0.0
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000055",
        "nombre": "Tomate",
        "aporte": {
          "energiaKcal": 7.2,
          "proteinasG": 0.4,
          "carbohidratosG": 1.6
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T21:20:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000081"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (170 g)"
    },
    "macronutrientes": {
      "energiaKcal": 238.0,
      "proteinasG": 32.9,
      "carbohidratosG": 3.1,
      "grasasG": 9.7,
      "fibraG": 0.9
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 274.5
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "guiso",
      "tiempoTotalMin": 25,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Sofreír la carne molida.",
          "tiempoMin": 10
        },
        {
          "orden": 2,
          "descripcion": "Añadir el tomate y la zanahoria.",
          "tiempoMin": 15
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000021",
        "nombre": "Carne de res magra",
        "aporte": {
          "energiaKcal": 224.4,
          "proteinasG": 32.4,
          "carbohidratosG": 0.0
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000055",
        "nombre": "Tomate",
        "aporte": {
          "energiaKcal": 5.4,
          "proteinasG": 0.3,
          "carbohidratosG": 1.2
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000057",
        "nombre": "Zanahoria",
        "aporte": {
          "energiaKcal": 8.2,
          "proteinasG": 0.2,
          "carbohidratosG": 1.9
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T21:27:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000082"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (180 g)"
    },
    "macronutrientes": {
      "energiaKcal": 235.2,
      "proteinasG": 8.6,
      "carbohidratosG": 48.1,
      "grasasG": 0.7,
      "fibraG": 5.7
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 181.8
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "guiso",
      "tiempoTotalMin": 55,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Cocer los frijoles.",
          "tiempoMin": 30
        },
        {
          "orden": 2,
          "descripcion": "Añadir el arroz al caldo de frijoles.",
          "tiempoMin": 25
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000001",
        "nombre": "Arroz blanco",
        "aporte": {
          "energiaKcal": 156.0,
          "proteinasG": 3.2,
          "carbohidratosG": 33.8
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000024",
        "nombre": "Frijoles negros",
        "aporte": {
          "energiaKcal": 79.2,
          "proteinasG": 5.3,
          "carbohidratosG": 14.2
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T21:34:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000083"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (180 g)"
    },
    "macronutrientes": {
      "energiaKcal": 76.2,
      "proteinasG": 1.1,
      "carbohidratosG": 19.3,
      "grasasG": 0.4,
      "fibraG": 2.4
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 15.0
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "crudo",
      "tiempoTotalMin": 10,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Pelar y cortar en dados.",
          "tiempoMin": 10
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000042",
        "nombre": "Papaya",
        "aporte": {
          "energiaKcal": 25.8,
          "proteinasG": 0.3,
          "carbohidratosG": 6.5
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000043",
        "nombre": "Piña",
        "aporte": {
          "energiaKcal": 30.0,
          "proteinasG": 0.3,
          "carbohidratosG": 7.9
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000045",
        "nombre": "Melón",
        "aporte": {
          "energiaKcal": 20.4,
          "proteinasG": 0.5,
          "carbohidratosG": 4.9
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T21:41:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000084"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (200 g)"
    },
    "macronutrientes": {
      "energiaKcal": 156.5,
      "proteinasG": 6.2,
      "carbohidratosG": 21.3,
      "grasasG": 5.1,
      "fibraG": 0.2
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 65.0
      }
    },
    "alergenos": [
      "leche"
    ],
    "modoPreparacion": {
      "tecnica": "hervido",
      "tiempoTotalMin": 65,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Cocer el arroz en la leche a fuego lento.",
          "tiempoMin": 35
        },
        {
          "orden": 2,
          "descripcion": "Enfriar antes de servir.",
          "tiempoMin": 30
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000001",
        "nombre": "Arroz blanco",
        "aporte": {
          "energiaKcal": 65.0,
          "proteinasG": 1.4,
          "carbohidratosG": 14.1
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000029",
        "nombre": "Leche entera",
        "aporte": {
          "energiaKcal": 91.5,
          "proteinasG": 4.8,
          "carbohidratosG": 7.2
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T21:48:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000085"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (300 g)"
    },
    "macronutrientes": {
      "energiaKcal": 157.0,
      "proteinasG": 7.9,
      "carbohidratosG": 32.8,
      "grasasG": 0.5,
      "fibraG": 2.6
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 85.0
      }
    },
    "alergenos": [
      "leche"
    ],
    "modoPreparacion": {
      "tecnica": "licuado",
      "tiempoTotalMin": 3,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Licuar con hielo.",
          "tiempoMin": 3
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000030",
        "nombre": "Leche descremada",
        "aporte": {
          "energiaKcal": 68.0,
          "proteinasG": 6.8,
          "carbohidratosG": 10.0
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000039",
        "nombre": "Plátano fruta",
        "aporte": {
          "energiaKcal": 89.0,
          "proteinasG": 1.1,
          "carbohidratosG": 22.8
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T21:55:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000086"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (150 g)"
    },
    "macronutrientes": {
      "energiaKcal": 90.0,
      "proteinasG": 1.2,
      "carbohidratosG": 22.5,
      "grasasG": 0.6,
      "fibraG": 2.4
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 1.5
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "licuado",
      "tiempoTotalMin": 3,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Licuar la pulpa con agua y hielo.",
          "tiempoMin": 3
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000041",
        "nombre": "Mango",
        "aporte": {
          "energiaKcal": 90.0,
          "proteinasG": 1.2,
          "carbohidratosG": 22.5
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T22:02:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000087"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (200 g)"
    },
    "macronutrientes": {
      "energiaKcal": 320.0,
      "proteinasG": 2.8,
      "carbohidratosG": 76.2,
      "grasasG": 0.6,
      "fibraG": 3.6
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 28.0
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "hervido",
      "tiempoTotalMin": 35,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Hervir la yuca hasta que abra.",
          "tiempoMin": 30
        },
        {
          "orden": 2,
          "descripcion": "Bañar con mojo de ajo.",
          "tiempoMin": 5
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000011",
        "nombre": "Yuca",
        "aporte": {
          "energiaKcal": 320.0,
          "proteinasG": 2.8,
          "carbohidratosG": 76.2
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T22:09:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000089"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (240 g)"
    },
    "macronutrientes": {
      "energiaKcal": 289.6,
      "proteinasG": 27.7,
      "carbohidratosG": 38.8,
      "grasasG": 1.8,
      "fibraG": 2.6
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 273.6
      }
    },
    "alergenos": [
      "gluten",
      "pescado"
    ],
    "modoPreparacion": {
      "tecnica": "hervido",
      "tiempoTotalMin": 15,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Cocer la pasta al dente.",
          "tiempoMin": 10
        },
        {
          "orden": 2,
          "descripcion": "Mezclar con el atún y la salsa de tomate.",
          "tiempoMin": 5
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000006",
        "nombre": "Pasta corta",
        "aporte": {
          "energiaKcal": 189.6,
          "proteinasG": 7.0,
          "carbohidratosG": 37.2
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000018",
        "nombre": "Atún en agua",
        "aporte": {
          "energiaKcal": 92.8,
          "proteinasG": 20.4,
          "carbohidratosG": 0.0
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000055",
        "nombre": "Tomate",
        "aporte": {
          "energiaKcal": 7.2,
          "proteinasG": 0.4,
          "carbohidratosG": 1.6
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T22:23:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000090"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (160 g)"
    },
    "macronutrientes": {
      "energiaKcal": 192.0,
      "proteinasG": 31.6,
      "carbohidratosG": 0.9,
      "grasasG": 6.7
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 365.4
      }
    },
    "alergenos": [
      "crustáceos",
      "huevo"
    ],
    "modoPreparacion": {
      "tecnica": "sartén",
      "tiempoTotalMin": 9,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Saltear los camarones.",
          "tiempoMin": 5
        },
        {
          "orden": 2,
          "descripcion": "Añadir el huevo y revolver.",
          "tiempoMin": 4
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000028",
        "nombre": "Camarones",
        "aporte": {
          "energiaKcal": 99.0,
          "proteinasG": 24.0,
          "carbohidratosG": 0.2
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000019",
        "nombre": "Huevo",
        "aporte": {
          "energiaKcal": 93.0,
          "proteinasG": 7.6,
          "carbohidratosG": 0.7
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T22:30:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000091"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (220 g)"
    },
    "macronutrientes": {
      "energiaKcal": 209.6,
      "proteinasG": 21.0,
      "carbohidratosG": 11.2,
      "grasasG": 10.8,
      "fibraG": 5.4
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 64.2
      }
    },
    "alergenos": [
      "soja"
    ],
    "modoPreparacion": {
      "tecnica": "salteado",
      "tiempoTotalMin": 14,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Dorar el tofu en dados.",
          "tiempoMin": 8
        },
        {
          "orden": 2,
          "descripcion": "Saltear las verduras.",
          "tiempoMin": 6
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000027",
        "nombre": "Tofu",
        "aporte": {
          "energiaKcal": 172.8,
          "proteinasG": 19.0,
          "carbohidratosG": 3.4
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000060",
        "nombre": "Brócoli",
        "aporte": {
          "energiaKcal": 20.4,
          "proteinasG": 1.7,
          "carbohidratosG": 4.0
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000057",
        "nombre": "Zanahoria",
        "aporte": {
          "energiaKcal": 16.4,
          "proteinasG": 0.4,
          "carbohidratosG": 3.8
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T22:37:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000092"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (200 g)"
    },
    "macronutrientes": {
      "energiaKcal": 245.5,
      "proteinasG": 13.5,
      "carbohidratosG": 40.6,
      "grasasG": 3.6,
      "fibraG": 5.3
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 64.0
      }
    },
    "alergenos": [
      "gluten",
      "leche"
    ],
    "modoPreparacion": {
      "tecnica": "hervido",
      "tiempoTotalMin": 8,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Cocer la avena en la leche.",
          "tiempoMin": 8
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000007",
        "nombre": "Avena en hojuelas",
        "aporte": {
          "energiaKcal": 194.5,
          "proteinasG": 8.4,
          "carbohidratosG": 33.1
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000030",
        "nombre": "Leche descremada",
        "aporte": {
          "energiaKcal": 51.0,
          "proteinasG": 5.1,
          "carbohidratosG": 7.5
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T22:44:00Z"
    }
  },
  {
    "origen": {
      "tabla": "Plato",
      "id": "17e30000-0000-4000-8000-000000000093"
    },
    "tipo": "compuesto",
    "porcionReferencia": {
      "cantidad": 1,
      "unidad": "ración (200 g)"
    },
    "macronutrientes": {
      "energiaKcal": 155.0,
      "proteinasG": 5.8,
      "carbohidratosG": 29.3,
      "grasasG": 2.1,
      "fibraG": 5.5
    },
    "micronutrientes": {
      "minerales": {
        "sodioMg": 38.2
      }
    },
    "alergenos": [],
    "modoPreparacion": {
      "tecnica": "hervido",
      "tiempoTotalMin": 23,
      "pasos": [
        {
          "orden": 1,
          "descripcion": "Cocer la quinoa.",
          "tiempoMin": 15
        },
        {
          "orden": 2,
          "descripcion": "Saltear las verduras y mezclar.",
          "tiempoMin": 8
        }
      ]
    },
    "ingredientes": [
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000014",
        "nombre": "Quinoa",
        "aporte": {
          "energiaKcal": 120.0,
          "proteinasG": 4.4,
          "carbohidratosG": 21.3
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000058",
        "nombre": "Habichuelas",
        "aporte": {
          "energiaKcal": 18.6,
          "proteinasG": 1.1,
          "carbohidratosG": 4.2
        }
      },
      {
        "alimentoId": "17e30000-0000-4000-8000-000000000057",
        "nombre": "Zanahoria",
        "aporte": {
          "energiaKcal": 16.4,
          "proteinasG": 0.4,
          "carbohidratosG": 3.8
        }
      }
    ],
    "actualizadoEn": {
      "$date": "2026-10-03T22:51:00Z"
    }
  }
]);

let escritas = 0;
for (const f of fichas) {
  db.getCollection(COLECCION).replaceOne({ "origen.tabla": f.origen.tabla, "origen.id": f.origen.id }, f, { upsert: true });
  escritas++;
}
print(`Fichas nutricionales escritas: ${escritas}`);
