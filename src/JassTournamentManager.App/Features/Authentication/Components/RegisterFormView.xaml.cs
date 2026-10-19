namespace JassTournamentManager.App.Features.Authentication.Components
{
    public partial class RegisterFormView : ContentView
    {
        public RegisterFormView()
        {
            InitializeComponent();
        }

        private void OnPasswordConfirmationValidationActivated(object? sender, EventArgs e)
        {
            if (BindingContext is RegisterFlowViewModel viewModel)
            {
                viewModel.ValidatePasswordConfirmationCommand.Execute(null);
            }
        }
    }
}
