namespace JassTournamentManager.App.Features.Authentication
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(AuthenticationViewModel authenticationViewModel)
        {
            InitializeComponent();
            BindingContext = authenticationViewModel;
            authenticationViewModel.FormsReset += (_, _) => ResetFieldValidation(this);
        }

        public void ResetAfterAuthentication()
        {
            if (BindingContext is AuthenticationViewModel viewModel)
            {
                viewModel.ResetAfterAuthentication();
            }
        }

        private static void ResetFieldValidation(IVisualTreeElement element)
        {
            if (element is UraniumUI.Material.Controls.InputField field &&
                (field is Components.DeferredValidationTextField || field.Handler is not null))
            {
                if (field is not Components.DeferredValidationTextField)
                {
                    field.Content?.Unfocus();
                }
                field.ResetValidation();
            }

            foreach (IVisualTreeElement child in element.GetVisualChildren())
            {
                ResetFieldValidation(child);
            }
        }
    }
}
