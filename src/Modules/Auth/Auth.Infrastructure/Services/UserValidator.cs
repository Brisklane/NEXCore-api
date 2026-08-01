using System.Text.RegularExpressions;
using Auth.Application.DTOs;
using Auth.Application.Services.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Auth.Infrastructure.Services;

/// <summary>
/// Validates a registration request field by field, returning the first failure as an
/// <see cref="ApiResponse"/> (or a success once every check passes). Email and username are also
/// checked for uniqueness against the store; the profile fields are optional and only validated
/// when a value is supplied.
/// </summary>
public class UserValidator : IUserValidator
{
    private readonly AuthDbContext _context;

    public UserValidator(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse> ValidateAsync(ValidateUserDto request)
    {
        var emailValidation = await ValidateEmailAsync(request.Email);
        if (emailValidation != null) return emailValidation;

        var usernameValidation = ValidateUsername(request.UserName);
        if (usernameValidation != null) return usernameValidation;

        var usernameUniquenessValidation = await CheckUsernameUniquenessAsync(request.UserName);
        if (usernameUniquenessValidation != null) return usernameUniquenessValidation;

        var passwordValidation = ValidatePassword(request.Password);
        if (passwordValidation != null) return passwordValidation;

        var fullNameValidation = ValidateFullName(request.FullName);
        if (fullNameValidation != null) return fullNameValidation;

        var genderValidation = ValidateGender(request.Gender);
        if (genderValidation != null) return genderValidation;

        var dobValidation = ValidateDateOfBirth(request.DateOfBirth);
        if (dobValidation != null) return dobValidation;

        var phoneValidation = ValidatePhoneNumber(request.PhoneNumber);
        if (phoneValidation != null) return phoneValidation;

        var addressValidation = ValidateAddress(request.Address);
        if (addressValidation != null) return addressValidation;

        return new ApiResponse { Success = true, Message = "Validation passed." };
    }

    private async Task<ApiResponse?> ValidateEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Fail("Email is required.");

        if (!IsValidEmail(email))
            return Fail("Invalid email format.");

        // Compare on the normalized column so a differently-cased address is recognised as the
        // same registration, matching the unique index that backs it.
        var normalized = User.Normalize(email);
        var emailExists = await _context.Users.AnyAsync(u => u.EmailNormalized == normalized);
        if (emailExists)
            return Fail("Email is already registered.");

        return null;
    }

    private ApiResponse? ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Fail("Username is required.");

        if (username.Length < 3 || username.Length > 50)
            return Fail("Username must be between 3 and 50 characters.");

        // A username may be a plain handle or an email, so allow letters, digits and . _ - @
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9._@-]+$"))
            return Fail("Username may only contain letters, numbers, and the . _ - @ characters.");

        return null;
    }

    private async Task<ApiResponse?> CheckUsernameUniquenessAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        var normalized = User.Normalize(username);
        var usernameExists = await _context.Users.AnyAsync(u => u.UsernameNormalized == normalized);
        if (usernameExists)
            return Fail("Username is already taken.");

        return null;
    }

    private ApiResponse? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Fail("Password is required.");

        if (password.Length < 8)
            return Fail("Password must be at least 8 characters.");

        if (password.Length > 100)
            return Fail("Password cannot exceed 100 characters.");

        if (!HasUpperCase(password))
            return Fail("Password must contain at least one uppercase letter.");

        if (!HasLowerCase(password))
            return Fail("Password must contain at least one lowercase letter.");

        if (!HasDigit(password))
            return Fail("Password must contain at least one digit.");

        if (!HasSpecialChar(password))
            return Fail("Password must contain at least one special character.");

        return null;
    }

    private ApiResponse? ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Fail("Full name is required.");

        if (fullName.Length < 2)
            return Fail("Full name must be at least 2 characters.");

        if (fullName.Length > 100)
            return Fail("Full name cannot exceed 100 characters.");

        return null;
    }

    // The fields below are optional: a blank value passes, a supplied value must be well-formed.

    private ApiResponse? ValidateGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
            return null;

        var validGenders = new[] { "Male", "Female", "Other" };
        if (!validGenders.Contains(gender, StringComparer.OrdinalIgnoreCase))
            return Fail("Gender must be Male, Female, or Other.");

        return null;
    }

    private ApiResponse? ValidateDateOfBirth(string? dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(dateOfBirth))
            return null;

        if (!DateTime.TryParse(dateOfBirth, out DateTime dob))
            return Fail("Invalid date of birth format.");

        var age = DateTime.UtcNow.Year - dob.Year;
        if (dob > DateTime.UtcNow.AddYears(-age)) age--;

        if (age < 18)
            return Fail("Admin must be at least 18 years old.");

        if (age > 120)
            return Fail("Invalid date of birth.");

        return null;
    }

    private ApiResponse? ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var phoneDigits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (phoneDigits.Length < 10 || phoneDigits.Length > 15)
            return Fail("Phone number must be between 10 and 15 digits.");

        return null;
    }

    private ApiResponse? ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        if (address.Length < 5)
            return Fail("Address must be at least 5 characters.");

        if (address.Length > 500)
            return Fail("Address cannot exceed 500 characters.");

        return null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static ApiResponse Fail(string message) => new() { Success = false, Message = message };

    private static bool IsValidEmail(string email) =>
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

    private static bool HasUpperCase(string password) => password.Any(char.IsUpper);
    private static bool HasLowerCase(string password) => password.Any(char.IsLower);
    private static bool HasDigit(string password) => password.Any(char.IsDigit);
    private static bool HasSpecialChar(string password) => password.Any(ch => !char.IsLetterOrDigit(ch));
}
