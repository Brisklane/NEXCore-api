namespace Nexcore.SharedKernel.ValueObjects;

/// <summary>
/// A postal address held as a value object: two instances with the same components are equal,
/// and <see cref="ToString"/> renders the non-empty parts as a single comma-separated line.
/// </summary>
public class Address
{
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public Address() { }

    public Address(string? streetAddress, string? city, string? state, string? postalCode, string? country = null)
    {
        StreetAddress = streetAddress;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(StreetAddress)) parts.Add(StreetAddress);
        if (!string.IsNullOrEmpty(City)) parts.Add(City);
        if (!string.IsNullOrEmpty(State)) parts.Add(State);
        if (!string.IsNullOrEmpty(PostalCode)) parts.Add(PostalCode);
        if (!string.IsNullOrEmpty(Country)) parts.Add(Country);

        return string.Join(", ", parts);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Address other) return false;

        return StreetAddress == other.StreetAddress &&
               City == other.City &&
               State == other.State &&
               PostalCode == other.PostalCode &&
               Country == other.Country;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StreetAddress, City, State, PostalCode, Country);
    }
}
