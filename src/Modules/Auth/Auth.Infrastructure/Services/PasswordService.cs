using Auth.Domain.Interfaces;

namespace Auth.Infrastructure.Services;

/// <summary>
/// BCrypt-backed password hashing. The work factor (12) sets the cost, and verification swallows
/// malformed-hash exceptions so a corrupt stored value reads as "no match" rather than throwing.
/// </summary>
public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
