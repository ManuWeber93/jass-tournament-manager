using UraniumUI.Material.Controls;

namespace JassTournamentManager.App.Features.Authentication.Components
{
    [ContentProperty(nameof(Validations))]
    public class DeferredValidationTextField : TextField
    {
        private bool hasBeenUnfocused;
        private bool isResetting;

        public event EventHandler? ValidationActivated;

        public DeferredValidationTextField()
        {
            Content.Unfocused += (_, _) =>
            {
                if (isResetting)
                {
                    return;
                }

                hasBeenUnfocused = true;
                CheckAndShowValidations();
                ValidationActivated?.Invoke(this, EventArgs.Empty);
            };
        }

        protected override void CheckAndShowValidations()
        {
            if (hasBeenUnfocused && Handler is not null)
            {
                base.CheckAndShowValidations();
            }
        }

        public override void DisplayValidation()
        {
            hasBeenUnfocused = true;
            base.DisplayValidation();
        }

        public override void ResetValidation()
        {
            try
            {
                isResetting = true;
                hasBeenUnfocused = false;
                Content.Unfocus();
                if (Handler is not null)
                {
                    base.ResetValidation();
                }
            }
            finally
            {
                isResetting = false;
            }
        }
    }
}