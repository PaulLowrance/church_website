using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Admin;

public class DashboardResponse
{
    public string Message { get; set; } = string.Empty;
}

public class AdminDashboardEndpoint : EndpointWithoutRequest<DashboardResponse>
{
    public override void Configure()
    {
        Get("/api/admin/dashboard");
        Roles("Admin");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(new DashboardResponse
        {
            Message = "Welcome to the admin dashboard!"
        }, cancellation: ct);
    }
}
