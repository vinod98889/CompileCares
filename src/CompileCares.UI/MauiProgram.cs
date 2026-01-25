using CompileCares.UI.Services.AuthService;
using CompileCares.UI.Services.BaseAPIService;
using CompileCares.UI.Services.ConsultationService;
using CompileCares.UI.Services.ServerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CompileCares.UI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add Configuration from appsettings.json
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // ✅ Register API Server Service
            builder.Services.AddSingleton<IApiServerService, ApiServerService>();
            builder.Services.AddScoped<IBaseApiService, BaseApiService>();
            builder.Services.AddScoped<IAuthService, AuthService>();            
            builder.Services.AddScoped<IHttpClientService, HttpClientService>();
            builder.Services.AddScoped<IAuthStateService, AuthStateService>();
            builder.Services.AddScoped<IConsultationService, ConsultationService>();

            // ✅ PROPERLY REGISTER HttpClient WITH BaseAddress
            builder.Services.AddScoped(sp =>
            {
                var apiServerService = sp.GetRequiredService<IApiServerService>();

                // Start server if not running
                if (!apiServerService.IsRunning)
                {
                    apiServerService.StartServer("http://localhost:7194");
                    // Small delay for server to initialize
                    Task.Delay(1000).Wait();
                }

                // Create HttpClient with BaseAddress
                var client = new HttpClient
                {
                    BaseAddress = new Uri(apiServerService.ServerUrl)
                };

                // Set default timeout
                client.Timeout = TimeSpan.FromSeconds(30);

                // Add default headers
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                return client;
            });

            return builder.Build();
        }
    }
}