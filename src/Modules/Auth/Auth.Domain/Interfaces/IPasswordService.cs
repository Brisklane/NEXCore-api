namespace Auth.Domain.Interfaces;

/// <summary>Hashes new passwords and checks a candidate against a stored hash. The algorithm is an implementation detail.</summary>
public interface IPasswordService
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string hash);
}
