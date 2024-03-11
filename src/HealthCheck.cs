using Grpc.Core;
using Grpc.Health.V1;
using static Grpc.Health.V1.HealthCheckResponse.Types;

namespace LegalEntities;

public class HealthCheck : Health.HealthBase
{
    public override Task<HealthCheckResponse> Check(HealthCheckRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HealthCheckResponse()
        {
            Status = ServingStatus.Serving
        });
    }
}
