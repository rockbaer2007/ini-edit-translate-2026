using System.Collections.Specialized;
using System.Configuration;

// Compatibility name. OpenAI credentials use separate Windows DPAPI storage.
public class CryptoKeyProvider : LocalFileSettingsProvider
{
    public override void Initialize(string name, NameValueCollection config)
    {
        base.Initialize(GetType().Name, config);
    }
}
