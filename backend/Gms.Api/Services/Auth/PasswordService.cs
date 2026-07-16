using Gms.Api.Domain;
using Microsoft.AspNetCore.Identity;

namespace Gms.Api.Services.Auth;

/// <summary>Secure password hashing/verification. Wraps the built-in PasswordHasher —
/// no custom cryptography.</summary>
public interface IPasswordService
{
    string Hash(AppUser user, string password);
    bool Verify(AppUser user, string password);
    void ValidatePolicy(string password);
}

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<AppUser> _hasher = new();

    public string Hash(AppUser user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(AppUser user, string password)
    {
        if (string.IsNullOrEmpty(user.PasswordHash)) return false;
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    /// <summary>Minimal development password policy: ≥10 chars, upper, lower, digit, special.</summary>
    public void ValidatePolicy(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 10)
            throw new Common.AuthValidationException("Parola en az 10 karakter olmalıdır.");
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower))
            throw new Common.AuthValidationException("Parola hem büyük hem küçük harf içermelidir.");
        if (!password.Any(char.IsDigit))
            throw new Common.AuthValidationException("Parola en az bir rakam içermelidir.");
        if (password.All(char.IsLetterOrDigit))
            throw new Common.AuthValidationException("Parola en az bir özel karakter içermelidir.");
    }
}
