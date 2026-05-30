namespace JustGo;

internal static class GradingResource
{
    /// <summary>
    /// Extension methods that configure the ITKD Grading Blazor project resource.
    /// </summary>
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ProjectResource> AddGradingApp(IResourceBuilder<ProjectResource> api)
        {
            return builder.AddProject<Projects.JustGo_Grading>("grading")
                .WithExternalHttpEndpoints()
                .WithReference(api)
                .WaitFor(api)
                .WithBrowserLogs(browser: "chrome", userDataMode: BrowserUserDataMode.Isolated);
        }
    }
}
