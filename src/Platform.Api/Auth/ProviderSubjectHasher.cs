using System.Security.Cryptography;
using System.Text;

namespace Platform.Api.Auth;

public interface IProviderSubjectHasher
{
    string Hash(string provider, string subject);
}

public sealed class Sha256ProviderSubjectHasher : IProviderSubjectHasher
{
    public string Hash(string provider, string subject)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Provider is required.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Provider subject is required.", nameof(subject));
        }

        var material = Encoding.UTF8.GetBytes($"{provider.Trim()}\n{subject.Trim()}");
        var hash = SHA256.HashData(material);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
