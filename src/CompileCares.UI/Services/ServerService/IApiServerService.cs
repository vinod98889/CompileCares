using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.ServerService
{
    public interface IApiServerService
    {
        void StartServer(string url = "http://localhost:7194");
        void StopServer();
        Task<bool> CheckHealthAsync();
        bool IsRunning { get; }
        string ServerUrl { get; }
        string GetServerInfo();
    }
}
