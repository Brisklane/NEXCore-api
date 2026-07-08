using Auth.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Auth.Application.Services.Interfaces;

/// <summary>Pre-persistence checks for a user request (uniqueness, format, policy), returning the first failure.</summary>
public interface IUserValidator
{
    Task<ApiResponse> ValidateAsync(ValidateUserDto request);
}
