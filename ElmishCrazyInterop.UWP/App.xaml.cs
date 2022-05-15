namespace ElmishCrazyInterop;

using ElmishCrazyInterop.Elmish;

using global::Windows.ApplicationModel;
using global::Windows.ApplicationModel.Activation;
using global::Windows.Networking.Connectivity;
using global::Windows.UI.Xaml;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

using AppProgram = ElmishCrazyInterop.Programs.App.Program;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public sealed partial class App : Application
{
    private readonly Lazy<Shell> shell = new Lazy<Shell>();
    private readonly Lazy<AppProgram> appProgram;

    private global::Windows.UI.Xaml.Window window;

    internal IServiceProvider ServiceProvider => appProgram.Value.ServiceProvider;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        appProgram = new Lazy<AppProgram>(new Func<AppProgram>(CreateAppProgram));
        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="e">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        Contract.Assume(e != null, nameof(e) + " is null.");
#if DEBUG
        if (System.Diagnostics.Debugger.IsAttached)
        {
            // this.DebugSettings.EnableFrameRateCounter = true;
        }
#endif

        window = global::Windows.UI.Xaml.Window.Current;
        var shell = this.shell.Value;
        // Get a Frame to act as the navigation context and navigate to the first page
        var rootFrame = shell.RootFrame;

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (rootFrame.Content == null)
        {
            var program =
                appProgram.Value.Program
                .WithSubscription(AppProgram.GetLifecycleEventsSubscription(SubscribeToLifecycleEvents))
                .WithSubscription(AppProgram.GetNetworkStatusSubscription(SubscribeToNetworkStatus));

            global::Elmish.Uno.ViewModel.StartLoop(
                UnoHost.ElmConfig, shell, global::Elmish.ProgramModule.runWith, program,
                (ElmishCrazyInterop.WinRT.ApplicationExecutionState)e.PreviousExecutionState);

            if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                //TODO: Load state from previously suspended application
            }

            // Place the frame in the current Window
            window.Content = shell;
        }

        if (!e.PrelaunchActivated)
        {
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            window.Activate();
        }
    }

    private AppProgram CreateAppProgram()
    {
        var hostBuilder =
            UnoHost.CreateDefaultBuilder()
                   .ConfigureServices(ConfigureServices);

        var serviceProvider = hostBuilder.Build().Services;
        return new AppProgram(serviceProvider);
    }

    private void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
    {
        services.AddSingleton<global::Elmish.Uno.Navigation.INavigationService>(_ =>
            new global::Elmish.Uno.Navigation.NavigationService(
                shell.Value.RootFrame,
                new Dictionary<string, Type>()
                {
                    [nameof(Pages.Main)] = typeof(MainPage),
                }));
    }

    private void SubscribeToLifecycleEvents(
        Action<Exception, string, bool, Action<bool>> onUnhandledException,
        Action<DateTimeOffset, Action> onSuspending,
        Action<object> onResuming,
        Action<Action> onEnteredBackground,
        Action<Action> onLeavingBackground)
    {
        this.UnhandledException +=
            (_, e) => onUnhandledException(e.Exception, e.Message, e.Handled, isHandled => e.Handled = isHandled);

        // <summary>
        // Invoked when application execution is being suspended.  Application state is saved
        // without knowing whether the application will be terminated or resumed with the contents
        // of memory still intact.
        // </summary>
        // <param name="sender">The source of the suspend request.</param>
        // <param name="e">Details about the suspend request.</param>
        void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var op = e.SuspendingOperation;
            var deferral = op.GetDeferral();
            onSuspending(op.Deadline, () => deferral.Complete());
        }
        this.Suspending += OnSuspending;
        this.Resuming += (_, o) => onResuming(o);

        this.EnteredBackground += (_, e) => onEnteredBackground(
            e.GetDeferral().Complete
        );
        this.LeavingBackground += (_, e) => onLeavingBackground(
            e.GetDeferral().Complete
        );
    }

    private static void SubscribeToNetworkStatus(Action<ElmishCrazyInterop.WinRT.NetworkConnectivityLevel> onNetworkChanged)
     => NetworkInformation.NetworkStatusChanged += (sender) =>
     {
         var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
         if (connectionProfile != null)
         {
             var connectivityLevel = connectionProfile.GetNetworkConnectivityLevel();
             onNetworkChanged((ElmishCrazyInterop.WinRT.NetworkConnectivityLevel)connectivityLevel);
         }
         else
         {
             onNetworkChanged(ElmishCrazyInterop.WinRT.NetworkConnectivityLevel.None);
         }
     };
}
