namespace Menus.Infrastructure.Mongo;

/// <summary>Resultado de <see cref="BajaCoordinadaDeItems.EliminarAsync"/> (RM-09).</summary>
public enum ResultadoBaja
{
    /// <summary>El ítem se borró de PostgreSQL y, si tenía ficha, también se borró de MongoDB.</summary>
    Eliminado,

    /// <summary>
    /// El ítem se borró de PostgreSQL, pero el borrado de su ficha en MongoDB falló. Queda una ficha huérfana
    /// que la conciliación (RM-10) elimina más adelante.
    /// </summary>
    FichaPendiente,

    /// <summary>No existía un ítem con ese identificador en PostgreSQL. No se tocó MongoDB.</summary>
    NoExiste,
}
