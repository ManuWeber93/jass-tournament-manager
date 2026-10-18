namespace JassTournamentManager.App.Features.Home
{
    public partial class HomePage : ContentPage
    {
        public HomePage(HomeViewModel homeViewModel)
        {
            InitializeComponent();
            BindingContext = homeViewModel;
        }
    }
}