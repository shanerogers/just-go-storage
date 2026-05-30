namespace JustGo;

/// <summary>
/// Extension methods that configure the ITKD Grading Blazor project resource.
/// </summary>
internal static class GradingResource
{
    public static IResourceBuilder<ProjectResource> AddGradingApp(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api)
    {
        return builder.AddProject<Projects.JustGo_Grading>("grading")
            .WithExternalHttpEndpoints()
            .WithReference(api)
            .WaitFor(api);
    }
}
