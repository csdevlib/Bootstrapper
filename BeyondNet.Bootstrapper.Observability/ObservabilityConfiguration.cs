using System.Collections.Generic;

namespace BeyondNet.Bootstrapper.Observability
{
    public class ObservabilityConfiguration
    {
        public string? OTLPEndpoint { get; set; } = "http://localhost:4317";

        public string? ServiceName { get; set; } = "UnknownService";

        public string? ServiceVersion { get; set; } = "1.0.0";

        public Dictionary<string, object>? ResourceAttributes { get; set; }
    }
}
