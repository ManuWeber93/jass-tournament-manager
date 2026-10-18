using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JassTournamentManager.App.Features.Authentication.Models;

namespace JassTournamentManager.App.Features.Authentication
{
    public partial class AuthenticationViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLoginMode))]
        public partial bool IsRegisterMode { get; set; }

        public AuthenticationViewModel(LoginFormViewModel loginForm, RegisterFlowViewModel registerFlow)
        {
            LoginForm = loginForm;
            RegisterFlow = registerFlow;
        }

        public LoginFormViewModel LoginForm { get; }

        public RegisterFlowViewModel RegisterFlow { get; }

        public bool IsLoginMode => !IsRegisterMode;

        [RelayCommand]
        private void ShowLogin()
        {
            if (LoginForm.IsBusy)
            {
                return;
            }
            IsRegisterMode = false;
        }

        [RelayCommand]
        private void ShowRegister()
        {
            if (LoginForm.IsBusy)
            {
                return;
            }
            IsRegisterMode = true;
            RegisterFlow.CurrentRegisterStep = RegisterStep.PreviousParticipationQuestion;
        }
    }
}
