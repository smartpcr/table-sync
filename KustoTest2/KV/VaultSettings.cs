namespace KustoTest2.KV
{
    public class VaultSettings
    {
        public string VaultName { get; set; }
        public string VaultUrl => $"https://{VaultName}.vault.azure.net";
    }
}
