using System.Diagnostics.CodeAnalysis;
using RepairShop.Domain.Common;

namespace RepairShop.Domain.Entities;

/// <summary>A person or company that owns one or more vehicles.</summary>
public class Customer
{
    private readonly List<Vehicle> _vehicles = [];

    private Customer()
    {
        // EF Core materialisation.
        Name = null!;
    }

    public Customer(string name, string? email, string? phone)
    {
        Rename(name);
        UpdateContact(email, phone);
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public IReadOnlyCollection<Vehicle> Vehicles => _vehicles.AsReadOnly();

    [MemberNotNull(nameof(Name))]
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name must not be empty.");
        }

        Name = name.Trim();
    }

    /// <summary>Sets the contact details. Blank values are stored as "unknown" (null), not as "".</summary>
    public void UpdateContact(string? email, string? phone)
    {
        Email = NullIfBlank(email);
        Phone = NullIfBlank(phone);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
