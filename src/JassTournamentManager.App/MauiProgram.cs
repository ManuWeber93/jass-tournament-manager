using JassTournamentManager.App.Features.Authentication;
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

        builder.Services.AddTransient<LoginPage>();

        builder.Services.AddTransient<AuthenticationViewModel>();
        builder.Services.AddTransient<LoginFormViewModel>();
        builder.Services.AddTransient<RegisterFlowViewModel>();

        builder.Services.AddSingleton(new ApiOptions());
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
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
