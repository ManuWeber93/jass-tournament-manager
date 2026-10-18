using JassTournamentManager.App.Features.Authentication;
using JassTournamentManager.App.Features.Home;
using JassTournamentManager.App.Infrastructure.Api;
using JassTournamentManager.App.Infrastructure.Auth;
using Microsoft.Extensions.Logging;
using UraniumUI;

namespace JassTournamentManager.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseUraniumUI()
			.UseUraniumUIMaterial()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<HomeViewModel>();

        builder.Services.AddSingleton<AuthenticationViewModel>();
        builder.Services.AddSingleton<LoginFormViewModel>();
        builder.Services.AddSingleton(_ => new RegisterFlowViewModel());

        builder.Services.AddSingleton(new ApiOptions());
        builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        builder.Services.AddSingleton<IAuthSession, AuthSession>();
        builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>((serviceProvider, httpClient) =>
        {
            httpClient.BaseAddress = serviceProvider.GetRequiredService<ApiOptions>().BaseAddress;
            httpClient.Timeout = TimeSpan.FromSeconds(15);
        });
        builder.Services.AddTransient<AuthenticatingHttpHandler>();

        builder.Services
            .AddHttpClient(ApiOptions.HttpClientName, (serviceProvider, httpClient) =>
            {
                ApiOptions apiOptions = serviceProvider.GetRequiredService<ApiOptions>();
                httpClient.BaseAddress = apiOptions.BaseAddress;
            })
            .AddHttpMessageHandler<AuthenticatingHttpHandler>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
