using System.Security.Cryptography;
using System.Text;

namespace Modules.AI.Infrastructure.Caching;

public static class AiCacheKeys
{
    public static string NormalizeCondition(string text)
        => "ficticia:ai:normalize-condition:" + Sha256(text.Trim().ToLowerInvariant());

    private static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
