using System;
using System.Collections.Generic;
//using System.Linq;
using System.Diagnostics;

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Logging.Serilog;
using Avalonia.Styling;

namespace Rationals.Explorer
{
    public class App : Application {
        public override void Initialize() {
            // Look up csproj's AvaloniaResource-s,
            // find an Application config for "Rationals.Explorer.App"
            // and load it
            AvaloniaXamlLoader.Load(this);
        }
    }

    class Program
    {
        // Your application's entry point. 
        // Here you can initialize your MVVM framework, DI container, etc.
        private static void AppMain(Application app, string[] args) {
            Debug.WriteLine("AppMain reached");
            //
            var window = new MainWindow();
            app.Run(window);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp() {
            var appBuilder = AppBuilder.Configure<App>();
            appBuilder.UsePlatformDetect();
            //appBuilder.UseDataGrid();
            appBuilder.LogToDebug(Avalonia.Logging.LogEventLevel.Information);
            return appBuilder;
        }

        public static void Main(string[] args) {
            var appBuilder = BuildAvaloniaApp();
            appBuilder.Start(AppMain, args);
        }
    }
}
