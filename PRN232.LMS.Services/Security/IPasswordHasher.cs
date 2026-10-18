namespace PRN232.LMS.Services.Security;

/// <summary>Hashes and verifies passwords (Lab 2 — passwords must never be stored as plain text).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
