using System.Collections.Specialized;
using System.Configuration;

// Retain the generated settings contract without modifying legacy IET data.
public class CryptoSettingsProvider : LocalFileSettingsProvider
{
    public override void Initialize(string name, NameValueCollection config)
    {
        base.Initialize(GetType().Name, config);
    }
}
