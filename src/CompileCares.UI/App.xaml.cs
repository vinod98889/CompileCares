using CompileCares.UI.Services.ServerService;

namespace CompileCares.UI
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly IApiServerService _apiServerService;
        public App(IApiServerService apiServerService)
        {
            InitializeComponent();
            _apiServerService = apiServerService;

            MainPage = new MainPage();

            // Start the API server when app starts
            StartApiServer();
        }
        private void StartApiServer()
        {
            try
            {
                // Try multiple ports
                var ports = new[] { 8080, 8081, 7194, 7195, 5000 };

                foreach (var port in ports)
                {
                    try
                    {
                        _apiServerService.StartServer($"http://localhost:{port}");
                        System.Diagnostics.Debug.WriteLine($"✅ CompileCares API Server started on: http://localhost:{port}");
                        System.Diagnostics.Debug.WriteLine($"🔗 Database: AppCareDB on UMESH\\SQLEXPRESS");
                        return; // Success, exit
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Port {port} failed: {ex.Message}");
                        // Try next port
                    }
                }

                System.Diagnostics.Debug.WriteLine($"❌ All ports are busy. Could not start API server.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to start API server: {ex.Message}");
            }
        }
        protected override void OnSleep()
        {
            // Optional: Stop server when app goes to background
            // _apiServerService.StopServer();
            base.OnSleep();
        }

        protected override void OnResume()
        {
            // Optional: Restart server when app resumes
            // StartApiServer();
            base.OnResume();
        }

        // Replace CleanUp() with this:
        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            window.Destroying += (sender, args) =>
            {
                _apiServerService.StopServer();
            };

            return window;
        }
    }
}
