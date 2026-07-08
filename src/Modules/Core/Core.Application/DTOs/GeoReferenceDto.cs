namespace Core.Application.DTOs;

// ?? Country ???????????????????????????????????????????????????????????????????

public class CountryDto
{
    public string Code { get; set; } = string.Empty;
    public string Code3 { get; set; } = string.Empty;
    public int NumericCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OfficialName { get; set; }
    public string? Region { get; set; }
    public string? SubRegion { get; set; }
    public string? CurrencyCode { get; set; }
    public string? PhoneCode { get; set; }
    public string? TimeZone { get; set; }
    public string? FlagEmoji { get; set; }
    public string? PostalCodePattern { get; set; }
    public string AddressFormat { get; set; } = "DEFAULT";
    public string StateLabel { get; set; } = "State / Province";
    public string? PostalCodeLabel { get; set; }
    public bool IsActive { get; set; }
}

// ?? Subdivision ???????????????????????????????????????????????????????????????

public class SubdivisionDto
{
    public string Code { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SubdivisionType { get; set; } = "State";
    public bool IsActive { get; set; }
}

// ?? City ??????????????????????????????????????????????????????????????????????

public class CityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? SubdivisionCode { get; set; }
    public string? PostalCode { get; set; }
    public string? CityCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int Population { get; set; }
}

// ?? Address Format Template ???????????????????????????????????????????????????

/// <summary>
/// Tells a client UI which address fields to render and in what order for a given country.
/// </summary>
public class AddressFormatDto
{
    public string CountryCode { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = "DEFAULT";

    /// <summary>Ordered list of field descriptors the UI should render.</summary>
    public List<AddressFieldDto> Fields { get; set; } = [];
}

public class AddressFieldDto
{
    /// <summary>Machine key: Street1, Street2, City, State, PostalCode, Country.</summary>
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>Localised label shown to the user.</summary>
    public string Label { get; set; } = string.Empty;

    public bool IsRequired { get; set; }
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; }
}

// ?? Currency ??????????????????????????????????????????????????????????????????????

public class CurrencyDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public int DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
}

// ?? Phone Code (country dial-code picker) ????????????????????????????????????

/// <summary>
/// Lightweight projection for a phone number country-code picker.
///
/// Flag sources (pick one per your frontend):
///   • FlagEmoji  — Unicode emoji (🇵🇰). Works everywhere, no request needed.
///   • FlagPng20  — https://flagcdn.com/w20/{code}.png  (20 px, ~1 KB)
///   • FlagPng40  — https://flagcdn.com/w40/{code}.png  (40 px, ~2 KB)
///   • FlagSvgUrl — https://flagcdn.com/{code}.svg      (scalable, ~3 KB)
///
/// flagcdn.com is a free, open CDN — no API key required.
/// The {code} segment is always the ISO 3166-1 alpha-2 code in lowercase.
/// </summary>
public class PhoneCodeDto
{
    /// <summary>ISO 3166-1 alpha-2, e.g. "PK".</summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>English country name.</summary>
    public string CountryName { get; set; } = string.Empty;

    /// <summary>Dial prefix including +, e.g. "+92".</summary>
    public string PhoneCode { get; set; } = string.Empty;

    /// <summary>Unicode flag emoji, e.g. "🇵🇰". Works in all modern browsers/apps.</summary>
    public string FlagEmoji { get; set; } = string.Empty;

    /// <summary>20 px PNG flag from flagcdn.com — best for small inline icons.</summary>
    public string FlagPng20 { get; set; } = string.Empty;

    /// <summary>40 px PNG flag from flagcdn.com — best for retina / larger pickers.</summary>
    public string FlagPng40 { get; set; } = string.Empty;

    /// <summary>SVG flag from flagcdn.com — scales to any size without quality loss.</summary>
    public string FlagSvgUrl { get; set; } = string.Empty;
}

// ?? Language ??????????????????????????????????????????????????????????????????

public class LanguageDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public bool IsRtl { get; set; }
    public bool IsActive { get; set; }
}
