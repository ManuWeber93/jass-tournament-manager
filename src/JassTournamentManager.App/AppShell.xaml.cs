using JassTournamentManager.App.Features.Authentication;
using JassTournamentManager.App.Features.Home;
using JassTournamentManager.App.Infrastructure.Auth;
using JassTournamentManager.App.Resources.Localization;

namespace JassTournamentManager.App;

public partial class AppShell : Shell
{
    private readonly LoginPage loginPage;
    private readonly HomePage homePage;
    private readonly IAuthSession session;

	public AppShell(LoginPage loginPage, HomePage homePage, IAuthSession session)
	{
		InitializeComponent();
        this.loginPage = loginPage;
        this.homePage = homePage;
        this.session = session;
        FlyoutBehavior = FlyoutBehavior.Disabled;
        session.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IAuthSession.CurrentUser))
            {
                MainThread.BeginInvokeOnMainThread(ShowCurrentPage);
            }
        };
        ShowCurrentPage();
    }

    private void ShowCurrentPage()
    {
        bool isAuthenticated = session.CurrentUser is not null;
        // Replace the root so back navigation cannot return to the previous authentication state.
        foreach (ShellContent content in Items.SelectMany(item => item.Items).SelectMany(section => section.Items))
        {
            content.Content = null;
        }
        Items.Clear();
        Items.Add(new ShellContent
        {
            Title = isAuthenticated ? AppResources.Home_Title : AppResources.Login_Title,
            Route = isAuthenticated ? "Home" : "Login",
            Content = isAuthenticated ? homePage : loginPage
        });
    }
}
