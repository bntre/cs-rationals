using System;
using System.Collections.Generic;
//using System.Linq;
using System.Diagnostics;
using System.Threading;

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Controls.ApplicationLifetimes;

namespace Rationals.Explorer
{
    public class App : Application {
        public override void Initialize() {
            // Look up csproj's AvaloniaResource-s,
            // find an Application config for "Rationals.Explorer.App"
            // and load it
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted() {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new MainWindow();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }

    class Program
    {
        // Your application's entry point. 
        // Here you can initialize your MVVM framework, DI container, etc.

        private static void AppMain(Application app, string[] args) {
            Debug.WriteLine("AppMain begin");
            /*
            var window = new MainWindow();
            app.Run(window);
            */

            /*
            // https://github.com/AvaloniaUI/Avalonia/wiki/Application-lifetimes
            // A cancellation token source that will be used to stop the main loop
            var cts = new CancellationTokenSource();
            // Do you startup code here
            new MainWindow().Show();
            // Start the main loop
            app.Run(cts.Token);
            */

            Debug.WriteLine("AppMain end");
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp() {
            var appBuilder = AppBuilder.Configure<App>();
            appBuilder.UsePlatformDetect();
            appBuilder.LogToTrace(Avalonia.Logging.LogEventLevel.Information);
            return appBuilder;
        }

        public static void Main(string[] args) {
            var appBuilder = BuildAvaloniaApp();
            //appBuilder.Start<MainWindow>();
            appBuilder.StartWithClassicDesktopLifetime(args);
        }
    }
}
