namespace Web.Features.Diagnostics;

public static class DiagnosticsFeatureExtensions
{
    public static WebApplicationBuilder AddDiagnosticsFeature(this WebApplicationBuilder builder)
    {
        var errorLog = new DiagnosticErrorLog();
        builder.Services.AddSingleton(errorLog);
        builder.Logging.AddProvider(new DiagnosticErrorLoggerProvider(errorLog));
        return builder;
    }
}
