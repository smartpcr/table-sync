namespace KustoTest2.Instrumentation
{
    public class AppInsightsSettings
    {
        public string InstrumentationKey { get; set; }
        public string Role { get; set; }
        public string Namespace { get; set; }
        public string Version { get; set; }
        public string[] Tags { get; set; }
    }
}
