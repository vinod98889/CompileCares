using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.UI.Services.AuthService
{
    public interface IAuthStateService : INotifyPropertyChanged
    {
        bool IsAuthenticated { get; }
        string UserName { get; }
        string UserRole { get; }
        bool IsCheckingAuth { get; }

        Task InitializeAsync();
        Task LogoutAsync();
        Task RefreshAuthStateAsync();
    }
}
