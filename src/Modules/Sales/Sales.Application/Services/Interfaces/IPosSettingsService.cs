using Sales.Application.DTOs;

namespace Sales.Application.Services.Interfaces;

public interface IPosSettingsService
{
    /// <summary>Returns the branch POS settings, creating a default row if none exists yet.</summary>
    Task<PosSettingsDto> GetAsync();

    /// <summary>Applies only the non-null fields in <paramref name="dto"/> (PATCH semantics).</summary>
    Task<PosSettingsDto> UpdateAsync(UpdatePosSettingsDto dto);

    /// <summary>Resets all settings to their factory defaults.</summary>
    Task<PosSettingsDto> ResetAsync();
}
