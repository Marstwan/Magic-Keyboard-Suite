using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MagicKeyboardSuite.Shared;
using MagicKeyboardSuite.WinUI3.Models;
using MagicKeyboardSuite.WinUI3.Services;
using MagicKeyboardSuite.WinUI3.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace MagicKeyboardSuite.WinUI3
{
    public sealed partial class MainWindow : Window
    {
        private readonly SettingsService _settings = new();
        private readonly BootCampDriverService _bootCampDrivers = new();
        private readonly BluetoothService _bluetooth;
        private bool _isLoading = true;
        private bool _includeNearbyScan;
        private string? _busyMac;
        private int _refreshVersion;

        private static readonly string[] AppLanguageCodes = { "auto", "ca", "es", "en" };

        // UpgradeCode del instalador MSI (Installer/Package.wxs). No cambia entre versiones.
        private const string MsiUpgradeCode = "{7C91F4A0-2D18-4E9B-B102-9A84C6E10F33}";

        // Tamaño mínimo en píxeles lógicos (DIPs), idéntico a macOS (Theme.Size.windowMinWidth / windowMinHeight)
        private const int MinWindowWidthDips = 980;
        private const int MinWindowHeightDips = 660;
        private const int GWLP_WNDPROC = -4;
        private const uint WM_GETMINMAXINFO = 0x0024;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate? _wndProcDelegate;
        private IntPtr _prevWndProc = IntPtr.Zero;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        private static extern uint MsiEnumRelatedProducts(string lpUpgradeCode, uint dwReserved, uint iProductIndex, System.Text.StringBuilder lpProductBuf);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate dwNewLong);

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        public void BringToFront()
        {
            try
            {
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                ShowWindow(hWnd, 9); // SW_RESTORE
                SetForegroundWindow(hWnd);
                this.Activate();
            }
            catch
            {
                this.Activate();
            }
        }

        public MainWindow()
        {
            this.InitializeComponent();
            _bluetooth = new BluetoothService(_settings);

            try
            {
                this.SystemBackdrop = new MicaBackdrop();
                this.ExtendsContentIntoTitleBar = true;
                this.SetTitleBar(AppTitleBar);
                this.AppWindow.Resize(new SizeInt32(1080, 740));

                InstallMinWindowSizeHook();

                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AppIcon.ico");
                if (File.Exists(iconPath))
                {
                    this.AppWindow.SetIcon(iconPath);
                }
            }
            catch { }

            ApplyLocalizedStrings();
            PopulateControls();
            LoadCurrentSettingsIntoUI();

            _bluetooth.DevicesChanged += () =>
            {
                DispatcherQueue.TryEnqueue(async () => await RefreshBluetoothKeyboardsAsync());
            };
            _bluetooth.StatusUpdated += (title, body, success) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    StatusInfoBar.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Informational;
                    StatusInfoBar.Title = title;
                    StatusInfoBar.Message = body;
                });
            };

            this.Closed += (_, _) => _bluetooth.Dispose();
            this.Activated += async (_, args) =>
            {
                if (!_isLoading && args.WindowActivationState != WindowActivationState.Deactivated)
                {
                    await RefreshBluetoothKeyboardsAsync();
                }
            };

            _isLoading = false;
            _ = RefreshBluetoothKeyboardsAsync();
        }

        private void InstallMinWindowSizeHook()
        {
            try
            {
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (hWnd == IntPtr.Zero) return;

                _wndProcDelegate = CustomWndProc;
                _prevWndProc = SetWindowLongPtr(hWnd, GWLP_WNDPROC, _wndProcDelegate);
            }
            catch { }
        }

        private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_GETMINMAXINFO && lParam != IntPtr.Zero)
            {
                try
                {
                    uint dpi = GetDpiForWindow(hWnd);
                    double scale = dpi > 0 ? dpi / 96.0 : 1.0;

                    MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    mmi.ptMinTrackSize.x = (int)Math.Round(MinWindowWidthDips * scale);
                    mmi.ptMinTrackSize.y = (int)Math.Round(MinWindowHeightDips * scale);
                    Marshal.StructureToPtr(mmi, lParam, false);
                    return IntPtr.Zero;
                }
                catch { }
            }

            return CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);
        }

        private void ApplyLocalizedStrings()
        {
            this.Title = L10n.AppName;
            AppTitleText.Text = L10n.AppName;
            AppVersionBadgeText.Text = L10n.AppVersion(AppInfo.Version);

            NavBluetoothItem.Content = L10n.NavBluetooth;
            NavLayoutItem.Content = L10n.NavLayout;
            NavShortcutsItem.Content = L10n.NavShortcuts;
            NavGuideItem.Content = L10n.NavGuide;

            FooterEngineText.Text = L10n.FooterEngineActive;

            BtTitleText.Text = L10n.BtTitle;
            BtSubtitleText.Text = L10n.BtSubtitle;
            BtRefreshButtonText.Text = L10n.BtRefresh;
            BtScanButtonText.Text = L10n.BtScan;

            StatusInfoBar.Title = L10n.StatusIdleTitle;
            StatusInfoBar.Message = L10n.StatusIdleBody;

            BtFilterTitleText.Text = L10n.BtFilterTitle;
            BtFilterSubtitleText.Text = L10n.BtFilterSubtitle;

            LayoutTitleText.Text = L10n.LayoutTitle;
            LayoutSubtitleText.Text = L10n.LayoutSubtitle;
            DriversTitleText.Text = L10n.LayoutDriversTitle;
            SelectDriversFolderButtonText.Text = L10n.LayoutDriversSelectFolder;
            SelectDriversZipButtonText.Text = L10n.LayoutDriversSelectZip;
            LayoutActiveTitleText.Text = L10n.LayoutActiveTitle;
            LayoutActiveSubtitleText.Text = L10n.LayoutActiveSubtitle;
            LayoutAppLanguageTitleText.Text = L10n.LayoutAppLanguageTitle;
            LayoutAppLanguageSubtitleText.Text = L10n.LayoutAppLanguageSubtitle;
            LayoutIsoTitleText.Text = L10n.LayoutIsoTitle;
            LayoutIsoSubtitleText.Text = L10n.LayoutIsoSubtitle;
            LayoutOptionTitleText.Text = L10n.LayoutOptionTitle;
            LayoutOptionSubtitleText.Text = L10n.LayoutOptionSubtitle;
            LayoutAppleOnlyTitleText.Text = L10n.LayoutAppleOnlyTitle;
            LayoutAppleOnlySubtitleText.Text = L10n.LayoutAppleOnlySubtitle;
            LayoutRememberTitleText.Text = L10n.LayoutRememberTitle;
            LayoutRememberSubtitleText.Text = L10n.LayoutRememberSubtitle;

            ShortcutsTitleText.Text = L10n.ShortcutsTitle;
            ShortcutsSubtitleText.Text = L10n.ShortcutsSubtitle;
            ShortcutsModifiersTitleText.Text = L10n.ShortcutsModifiersTitle;
            ShortcutsModifiersSubtitleText.Text = L10n.ShortcutsModifiersSubtitle;
            ShortcutsNavTitleText.Text = L10n.ShortcutsNavTitle;
            ShortcutsNavSubtitleText.Text = L10n.ShortcutsNavSubtitle;
            ShortcutsMediaTitleText.Text = L10n.ShortcutsMediaTitle;
            ShortcutsMediaSubtitleText.Text = L10n.ShortcutsMediaSubtitle;
            ShortcutsRightOptionFnTitleText.Text = L10n.ShortcutsRightOptionFnTitle;
            ShortcutsRightOptionFnSubtitleText.Text = L10n.ShortcutsRightOptionFnSubtitle;

            GuideTitleText.Text = L10n.GuideTitle;
            GuideSubtitleText.Text = L10n.GuideSubtitle;
            GuideToWindowsTitleText.Text = L10n.GuideToWindowsTitle;
            GuideToWindows1Text.Text = "1. " + L10n.GuideToWindows1;
            GuideToWindows2Text.Text = "2. " + L10n.GuideToWindows2;
            GuideToWindows3Text.Text = "3. " + L10n.GuideToWindows3;
            GuideToMacTitleText.Text = L10n.GuideToMacTitle;
            GuideToMac1Text.Text = "1. " + L10n.GuideToMac1;
            GuideToMac2Text.Text = "2. " + L10n.GuideToMac2;
            GuideToMac3Text.Text = "3. " + L10n.GuideToMac3;
            GuideSymbolsTitleText.Text = L10n.GuideSymbolsTitle;
            GuideSymbols1Text.Text = "• " + L10n.GuideSymbols1;
            GuideSymbols2Text.Text = "• " + L10n.GuideSymbols2;
            GuideSymbols3Text.Text = "• " + L10n.GuideSymbols3;

            ReopenOnboardingTitleText.Text = L10n.OnboardingTitle;
            ReopenOnboardingSubtitleText.Text = L10n.OnboardingSubtitle;
            ReopenOnboardingButtonText.Text = L10n.OnboardingReopenButton;

            OnboardingSetupBadgeText.Text = L10n.OnboardingSetupBadge;
            OnboardingSetupTitleText.Text = L10n.OnboardingSetupTitle;
            OnboardingSetupSubtitleText.Text = L10n.OnboardingSetupSubtitle;
            OnboardingLangTitleText.Text = L10n.LayoutAppLanguageTitle;
            OnboardingLangSubtitleText.Text = L10n.LayoutAppLanguageSubtitle;
            OnboardingDriversTitleText.Text = L10n.LayoutDriversTitle;
            OnboardingSelectDriversFolderButtonText.Text = L10n.LayoutDriversSelectFolder;
            OnboardingSelectDriversZipButtonText.Text = L10n.LayoutDriversSelectZip;
            OnboardingNextButtonText.Text = L10n.OnboardingNextButton;
            OnboardingBackButtonText.Text = L10n.OnboardingBackButton;

            OnboardingBadgeText.Text = L10n.OnboardingBadge;
            OnboardingTitleText.Text = L10n.OnboardingTitle;
            OnboardingSubtitleText.Text = L10n.OnboardingSubtitle;
            OnboardingStep1TitleText.Text = L10n.OnboardingStep1Title;
            OnboardingStep1BodyText.Text = L10n.OnboardingStep1Body;
            OnboardingStep2TitleText.Text = L10n.OnboardingStep2Title;
            OnboardingStep2BodyText.Text = L10n.OnboardingStep2Body;
            OnboardingStep3TitleText.Text = L10n.OnboardingStep3Title;
            OnboardingStep3BodyText.Text = L10n.OnboardingStep3Body;
            OnboardingReopenHintText.Text = L10n.OnboardingReopenHint;
            OnboardingStartButtonText.Text = L10n.OnboardingStartButton;
            RefreshBootCampDriversStatus();

            UninstallTitleText.Text = L10n.UninstallTitle;
            UninstallSubtitleText.Text = L10n.UninstallSubtitle;
            UninstallButtonText.Text = L10n.UninstallButton;

            foreach (var toggle in new[] { IsoSwapToggle, LeftOptionToggle, OnlyAppleToggle, RememberReleasedToggle, MacNavToggle, MediaKeysToggle, RightOptionFnToggle })
            {
                toggle.OnContent = L10n.ToggleOn;
                toggle.OffContent = L10n.ToggleOff;
            }
        }

        private void PopulateControls()
        {
            DeviceFilterComboBox.Items.Clear();
            DeviceFilterComboBox.Items.Add(L10n.BtFilterApple);
            DeviceFilterComboBox.Items.Add(L10n.BtFilterKeyboards);
            DeviceFilterComboBox.Items.Add(L10n.BtFilterAll);

            LayoutComboBox.Items.Clear();
            foreach (var lay in AppleLayoutDefinition.All)
            {
                LayoutComboBox.Items.Add(lay.Name);
            }

            AppLanguageComboBox.Items.Clear();
            AppLanguageComboBox.Items.Add(L10n.LayoutAppLanguageAuto);
            AppLanguageComboBox.Items.Add(L10n.LayoutAppLanguageCa);
            AppLanguageComboBox.Items.Add(L10n.LayoutAppLanguageEs);
            AppLanguageComboBox.Items.Add(L10n.LayoutAppLanguageEn);

            OnboardingLanguageComboBox.Items.Clear();
            OnboardingLanguageComboBox.Items.Add(L10n.LayoutAppLanguageAuto);
            OnboardingLanguageComboBox.Items.Add(L10n.LayoutAppLanguageCa);
            OnboardingLanguageComboBox.Items.Add(L10n.LayoutAppLanguageEs);
            OnboardingLanguageComboBox.Items.Add(L10n.LayoutAppLanguageEn);

            ModifierModeComboBox.Items.Clear();
            ModifierModeComboBox.Items.Add(L10n.ShortcutsModifiersMac);
            ModifierModeComboBox.Items.Add(L10n.ShortcutsModifiersSwap);
            ModifierModeComboBox.Items.Add(L10n.ShortcutsModifiersWindows);
        }

        private void LoadCurrentSettingsIntoUI()
        {
            _settings.Load();

            DeviceFilterComboBox.SelectedIndex = (int)_settings.DeviceFilter;
            ModifierModeComboBox.SelectedIndex = _settings.ModifierMode;
            IsoSwapToggle.IsOn = _settings.SwapIsoKeys;
            LeftOptionToggle.IsOn = _settings.LeftOptionAsAltGr;
            OnlyAppleToggle.IsOn = _settings.OnlyAppleHardware;
            RememberReleasedToggle.IsOn = _settings.RememberReleased;
            MacNavToggle.IsOn = _settings.MacNavigationShortcuts;
            MediaKeysToggle.IsOn = _settings.MediaKeysDefault;
            RightOptionFnToggle.IsOn = _settings.RightOptionAsFn;

            int langIdx = Array.IndexOf(AppLanguageCodes, _settings.AppLanguage);
            int resolvedLangIdx = langIdx >= 0 ? langIdx : 0;
            AppLanguageComboBox.SelectedIndex = resolvedLangIdx;
            OnboardingLanguageComboBox.SelectedIndex = resolvedLangIdx;

            int idx = AppleLayoutDefinition.All
                .Select((item, index) => (item, index))
                .FirstOrDefault(x => string.Equals(x.item.Klid, _settings.PreferredKlid, StringComparison.OrdinalIgnoreCase)).index;
            LayoutComboBox.SelectedIndex = idx;
            UpdateFooterLayoutText();

            OnboardingOverlay.Visibility = _settings.HasSeenOnboarding ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateFooterLayoutText()
        {
            int idx = Math.Clamp(LayoutComboBox.SelectedIndex, 0, AppleLayoutDefinition.All.Count - 1);
            ActiveLayoutFooterText.Text = L10n.FooterCurrentLayout(AppleLayoutDefinition.All[idx].Name);
        }

        private void SaveUISettings()
        {
            if (_isLoading) return;

            _settings.DeviceFilter = (DeviceFilterMode)Math.Clamp(DeviceFilterComboBox.SelectedIndex, 0, 2);
            _settings.ModifierMode = Math.Clamp(ModifierModeComboBox.SelectedIndex, 0, 2);
            _settings.SwapIsoKeys = IsoSwapToggle.IsOn;
            _settings.LeftOptionAsAltGr = LeftOptionToggle.IsOn;
            _settings.OnlyAppleHardware = OnlyAppleToggle.IsOn;
            _settings.RememberReleased = RememberReleasedToggle.IsOn;
            _settings.MacNavigationShortcuts = MacNavToggle.IsOn;
            _settings.MediaKeysDefault = MediaKeysToggle.IsOn;
            _settings.RightOptionAsFn = RightOptionFnToggle.IsOn;

            int langIdx = Math.Clamp(AppLanguageComboBox.SelectedIndex, 0, AppLanguageCodes.Length - 1);
            _settings.AppLanguage = AppLanguageCodes[langIdx];

            if (LayoutComboBox.SelectedIndex >= 0 && LayoutComboBox.SelectedIndex < AppleLayoutDefinition.All.Count)
            {
                _settings.PreferredKlid = AppleLayoutDefinition.All[LayoutComboBox.SelectedIndex].Klid;
                UpdateFooterLayoutText();
            }

            _settings.Save();
        }

        private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                BluetoothPage.Visibility = tag == "bluetooth" ? Visibility.Visible : Visibility.Collapsed;
                LayoutPage.Visibility = tag == "layout" ? Visibility.Visible : Visibility.Collapsed;
                ShortcutsPage.Visibility = tag == "shortcuts" ? Visibility.Visible : Visibility.Collapsed;
                GuidePage.Visibility = tag == "guide" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public async Task RefreshBluetoothKeyboardsAsync(bool forceRefresh = false)
        {
            if (KeyboardsCardsContainer == null) return;

            int myVersion = ++_refreshVersion;
            var keyboards = await _bluetooth.GetKeyboardsAsync(forceRefresh, _includeNearbyScan);
            if (myVersion != _refreshVersion) return;

            KeyboardsCardsContainer.Children.Clear();

            if (keyboards.Count == 0)
            {
                KeyboardsCardsContainer.Children.Add(Components.CreateEmptyStateCard());
                return;
            }

            foreach (var kbd in keyboards)
            {
                bool isBusy = string.Equals(_busyMac, kbd.MacAddress, StringComparison.OrdinalIgnoreCase);
                var card = Components.CreateKeyboardCard(
                    kbd,
                    isBusy,
                    onRelease: async () =>
                    {
                        _busyMac = kbd.MacAddress;
                        await RefreshBluetoothKeyboardsAsync();
                        await _bluetooth.ReleaseForPeerAsync(kbd);
                        _busyMac = null;
                        await RefreshBluetoothKeyboardsAsync();
                    },
                    onConnect: async () =>
                    {
                        _busyMac = kbd.MacAddress;
                        await RefreshBluetoothKeyboardsAsync();
                        await _bluetooth.ConnectOrPairAsync(kbd);
                        _busyMac = null;
                        await RefreshBluetoothKeyboardsAsync();
                    });
                KeyboardsCardsContainer.Children.Add(card);
            }
        }

        private async void OnRefreshBluetoothClicked(object sender, RoutedEventArgs e)
        {
            await RefreshBluetoothKeyboardsAsync(forceRefresh: true);
        }

        private async void OnScanNearbyClicked(object sender, RoutedEventArgs e)
        {
            ScanNearbyButton.IsEnabled = false;
            BtScanButtonText.Text = L10n.BtScanning;
            StatusInfoBar.Severity = InfoBarSeverity.Informational;
            StatusInfoBar.Title = L10n.StatusScanningTitle;
            StatusInfoBar.Message = L10n.StatusScanningBody;

            _includeNearbyScan = true;
            await RefreshBluetoothKeyboardsAsync(forceRefresh: true);
            await Task.Delay(2500);
            await RefreshBluetoothKeyboardsAsync(forceRefresh: true);

            StatusInfoBar.Severity = InfoBarSeverity.Success;
            StatusInfoBar.Title = L10n.StatusScanDoneTitle;
            StatusInfoBar.Message = L10n.StatusScanDoneBody;
            BtScanButtonText.Text = L10n.BtScan;
            ScanNearbyButton.IsEnabled = true;
        }

        private async void OnDeviceFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            SaveUISettings();
            await RefreshBluetoothKeyboardsAsync(forceRefresh: true);
        }

        private async void OnAppLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            SaveUISettings();

            _isLoading = true;
            ApplyLocalizedStrings();
            PopulateControls();
            LoadCurrentSettingsIntoUI();
            _isLoading = false;

            await RefreshBluetoothKeyboardsAsync();
        }

        private async void OnOnboardingLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            AppLanguageComboBox.SelectedIndex = OnboardingLanguageComboBox.SelectedIndex;
            SaveUISettings();

            _isLoading = true;
            ApplyLocalizedStrings();
            PopulateControls();
            LoadCurrentSettingsIntoUI();
            _isLoading = false;

            await RefreshBluetoothKeyboardsAsync();
        }

        private void OnLayoutSelectionChanged(object sender, SelectionChangedEventArgs e) => SaveUISettings();
        private void OnModifierModeChanged(object sender, SelectionChangedEventArgs e) => SaveUISettings();

        private void RefreshBootCampDriversStatus()
        {
            var res = Application.Current.Resources;
            bool installed = _bootCampDrivers.AreDriversInstalled();

            string subtitle = installed ? L10n.LayoutDriversInstalled : L10n.LayoutDriversMissing;
            string badge = installed ? L10n.LayoutDriversBadgeInstalled : L10n.LayoutDriversBadgeMissing;
            var fillBrush = (Brush)res[installed ? "MKSStateConnectedFillBrush" : "MKSStatePairedFillBrush"];
            var fgBrush = (Brush)res[installed ? "MKSStateConnectedBrush" : "MKSStatePairedBrush"];

            DriversSubtitleText.Text = subtitle;
            DriversStatusBadgeText.Text = badge;
            DriversStatusBadge.Background = fillBrush;
            DriversStatusBadgeText.Foreground = fgBrush;

            OnboardingDriversSubtitleText.Text = subtitle;
            OnboardingDriversStatusBadgeText.Text = badge;
            OnboardingDriversStatusBadge.Background = fillBrush;
            OnboardingDriversStatusBadgeText.Foreground = fgBrush;

            if (!installed && StatusInfoBar.Title == L10n.StatusIdleTitle)
            {
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.Title = L10n.LayoutDriversTitle;
                StatusInfoBar.Message = L10n.LayoutDriversMissing;
            }
        }

        private async void OnSelectDriversFolderClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FolderPicker();
                picker.FileTypeFilter.Add("*");
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder == null) return;

                await RunDriversImportAsync(() => _bootCampDrivers.ImportFromFolderAsync(folder.Path));
            }
            catch { }
        }

        private async void OnSelectDriversZipClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.FileTypeFilter.Add(".zip");
                picker.FileTypeFilter.Add(".msi");
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                if (file.Path.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    string parentDir = Path.GetDirectoryName(file.Path) ?? file.Path;
                    await RunDriversImportAsync(() => _bootCampDrivers.ImportFromFolderAsync(parentDir));
                }
                else
                {
                    await RunDriversImportAsync(() => _bootCampDrivers.ImportFromZipAsync(file.Path));
                }
            }
            catch { }
        }

        private async Task RunDriversImportAsync(Func<Task<int>> importFunc)
        {
            SelectDriversFolderButton.IsEnabled = false;
            SelectDriversZipButton.IsEnabled = false;
            OnboardingSelectDriversFolderButton.IsEnabled = false;
            OnboardingSelectDriversZipButton.IsEnabled = false;

            DriversInstallProgressRing.Visibility = Visibility.Visible;
            DriversInstallProgressRing.IsActive = true;
            OnboardingDriversProgressRing.Visibility = Visibility.Visible;
            OnboardingDriversProgressRing.IsActive = true;

            DriversSubtitleText.Text = L10n.LayoutDriversInstalling;
            OnboardingDriversSubtitleText.Text = L10n.LayoutDriversInstalling;

            int installedCount = await importFunc();

            DriversInstallProgressRing.IsActive = false;
            DriversInstallProgressRing.Visibility = Visibility.Collapsed;
            OnboardingDriversProgressRing.IsActive = false;
            OnboardingDriversProgressRing.Visibility = Visibility.Collapsed;

            SelectDriversFolderButton.IsEnabled = true;
            SelectDriversZipButton.IsEnabled = true;
            OnboardingSelectDriversFolderButton.IsEnabled = true;
            OnboardingSelectDriversZipButton.IsEnabled = true;

            RefreshBootCampDriversStatus();

            if (installedCount > 0)
            {
                _settings.Save();
                string msg = L10n.LayoutDriversSuccess(installedCount);
                StatusInfoBar.Severity = InfoBarSeverity.Success;
                StatusInfoBar.Title = L10n.LayoutDriversTitle;
                StatusInfoBar.Message = msg;
                DriversSubtitleText.Text = msg;
                OnboardingDriversSubtitleText.Text = msg;
            }
            else
            {
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.Title = L10n.LayoutDriversTitle;
                StatusInfoBar.Message = L10n.LayoutDriversNotFound;
                DriversSubtitleText.Text = L10n.LayoutDriversNotFound;
                OnboardingDriversSubtitleText.Text = L10n.LayoutDriversNotFound;
            }
        }

        private async void OnSettingsToggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            SaveUISettings();
            if (ReferenceEquals(sender, RememberReleasedToggle))
            {
                await RefreshBluetoothKeyboardsAsync();
            }
        }

        private void OnOnboardingNextClicked(object sender, RoutedEventArgs e)
        {
            OnboardingSetupPanel.Visibility = Visibility.Collapsed;
            OnboardingGuidePanel.Visibility = Visibility.Visible;
        }

        private void OnOnboardingBackClicked(object sender, RoutedEventArgs e)
        {
            OnboardingGuidePanel.Visibility = Visibility.Collapsed;
            OnboardingSetupPanel.Visibility = Visibility.Visible;
        }

        private void OnDismissOnboardingClicked(object sender, RoutedEventArgs e)
        {
            _settings.HasSeenOnboarding = true;
            _settings.Save();
            OnboardingOverlay.Visibility = Visibility.Collapsed;
        }

        private void OnReopenOnboardingClicked(object sender, RoutedEventArgs e)
        {
            OnboardingSetupPanel.Visibility = Visibility.Visible;
            OnboardingGuidePanel.Visibility = Visibility.Collapsed;
            OnboardingOverlay.Visibility = Visibility.Visible;
        }

        private void OnUninstallClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var productCode = new System.Text.StringBuilder(39);
                if (MsiEnumRelatedProducts(MsiUpgradeCode, 0, 0, productCode) == 0)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "msiexec.exe",
                        Arguments = $"/x {productCode}",
                        UseShellExecute = true
                    });
                    Application.Current.Exit();
                }
                else
                {
                    Process.Start(new ProcessStartInfo { FileName = "ms-settings:appsfeatures", UseShellExecute = true });
                }
            }
            catch { }
        }
    }
}
