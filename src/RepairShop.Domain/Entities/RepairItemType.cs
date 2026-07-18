namespace RepairShop.Domain.Entities;

/// <summary>Kind of line item on a repair order.</summary>
public enum RepairItemType
{
    /// <summary>Arbeitszeit — labour, quantity is hours.</summary>
    Labour = 1,

    /// <summary>Ersatzteil — spare part, quantity is pieces.</summary>
    Part = 2
}
