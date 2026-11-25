using System;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace MagicKeyboardSuite.WinUI3
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                var mainInstance = AppInstance.FindOrRegisterForKey("MagicKeyboardSuite_MainInstance_v1");
                if (!mainInstance.IsCurrent)
                {
                    var actArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                    await mainInstance.RedirectActivationToAsync(actArgs);
                    Environment.Exit(0);
                    return;
                }

                mainInstance.Activated += OnMainInstanceActivated;
            }
            catch { }

            _window = new MainWindow();
            _window.Activate();
        }

        private void OnMainInstanceActivated(object? sender, AppActivationArguments e)
        {
            if (_window is MainWindow mainWin)
            {
                mainWin.DispatcherQueue.TryEnqueue(() => mainWin.BringToFront());
            }
        }
    }
}
