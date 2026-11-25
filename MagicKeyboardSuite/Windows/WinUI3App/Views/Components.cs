using System;
using MagicKeyboardSuite.Shared;
using MagicKeyboardSuite.WinUI3.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MagicKeyboardSuite.WinUI3.Views
{
    /// <summary>
    /// Componentes visuales unificados (mismo nombre `SettingsCard` y `KeyboardCard` y mismos tokens que en macOS SwiftUI).
    /// </summary>
    public static class Components
    {
        public static Border CreateKeyboardCard(
            BluetoothKeyboardItem item,
            bool isBusy,
            Action onRelease,
            Action onConnect)
        {
            var res = Application.Current.Resources;

            var card = new Border
            {
                Background = (Brush)res["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Brush)res["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = (CornerRadius)res["MKSCardCornerRadius"],
                Padding = (Thickness)res["MKSCardPadding"]
            };

            var grid = new Grid { ColumnSpacing = (double)res["MKSSpacingL"] };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            double tileSize = (double)res["MKSSizeIconTile"];
            var iconBox = new Border
            {
                Width = tileSize,
                Height = tileSize,
                CornerRadius = (CornerRadius)res["MKSIconTileCornerRadius"],
                Background = (Brush)res["MKSIconTileBrush"],
                VerticalAlignment = VerticalAlignment.Center,
                Child = new FontIcon
                {
                    Glyph = (string)res["MKSIconKeyboard"],
                    FontSize = (double)res["MKSSizeTileIcon"],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(iconBox, 0);
            grid.Children.Add(iconBox);

            var infoStack = new StackPanel
            {
                Spacing = (double)res["MKSSpacingXs"],
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = (double)res["MKSSpacingS"]
            };

            titleRow.Children.Add(new TextBlock
            {
                Text = item.Name,
                Style = (Style)res["MKSDeviceNameTextStyle"],
                VerticalAlignment = VerticalAlignment.Center
            });

            var (fgKey, fillKey) = item.State switch
            {
                KeyboardConnectionState.Connected => ("MKSStateConnectedBrush", "MKSStateConnectedFillBrush"),
                KeyboardConnectionState.PairedDisconnected => ("MKSStatePairedBrush", "MKSStatePairedFillBrush"),
                KeyboardConnectionState.ReleasedToPeer => ("MKSStateReleasedBrush", "MKSStateReleasedFillBrush"),
                KeyboardConnectionState.NearbyVisible => ("MKSStateNearbyBrush", "MKSStateNearbyFillBrush"),
                _ => ("MKSStatePairedBrush", "MKSStatePairedFillBrush")
            };

            var badge = new Border
            {
                Background = (Brush)res[fillKey],
                CornerRadius = (CornerRadius)res["MKSBadgeCornerRadius"],
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = item.StateLabel,
                    Style = (Style)res["MKSBadgeTextStyle"],
                    Foreground = (Brush)res[fgKey]
                }
            };
            titleRow.Children.Add(badge);

            if (item.BatteryPercent is int pct && pct >= 1 && pct <= 100)
            {
                string iconKey = pct <= 20 ? "MKSIconBatteryLow" : (pct <= 60 ? "MKSIconBatteryMid" : "MKSIconBatteryFull");
                var (batFgKey, batFillKey) = pct <= 15
                    ? ("MKSStateErrorBrush", "MKSStateErrorFillBrush")
                    : (pct <= 35
                        ? ("MKSStatePairedBrush", "MKSStatePairedFillBrush")
                        : ("MKSStateConnectedBrush", "MKSStateConnectedFillBrush"));

                var batRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    VerticalAlignment = VerticalAlignment.Center
                };
                batRow.Children.Add(new FontIcon
                {
                    Glyph = (string)res[iconKey],
                    FontSize = 11,
                    Foreground = (Brush)res[batFgKey],
                    VerticalAlignment = VerticalAlignment.Center
                });
                batRow.Children.Add(new TextBlock
                {
                    Text = L10n.BtBattery(pct),
                    Style = (Style)res["MKSBadgeTextStyle"],
                    Foreground = (Brush)res[batFgKey],
                    VerticalAlignment = VerticalAlignment.Center
                });

                var batBadge = new Border
                {
                    Background = (Brush)res[batFillKey],
                    CornerRadius = (CornerRadius)res["MKSBadgeCornerRadius"],
                    Padding = new Thickness(8, 2, 8, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = batRow
                };
                titleRow.Children.Add(batBadge);
            }

            infoStack.Children.Add(titleRow);

            bool isUsb = string.Equals(item.MacAddress, "USB", StringComparison.OrdinalIgnoreCase);
            string macOrUsb = isUsb ? "USB" : L10n.BtMac(item.MacAddress);
            string subtitle = macOrUsb + (item.IsAppleKeyboard ? $"  •  {L10n.BtAppleKeyboard}" : "");
            infoStack.Children.Add(new TextBlock
            {
                Text = subtitle,
                Style = (Style)res["MKSCardBodyTextStyle"],
                Foreground = (Brush)res["TextFillColorSecondaryBrush"]
            });

            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            if (isBusy)
            {
                var ring = new ProgressRing
                {
                    IsActive = true,
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(16, 0, 16, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(ring, 2);
                grid.Children.Add(ring);
            }
            else if (!isUsb)
            {
                var actionsRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = (double)res["MKSSpacingS"],
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (item.State == KeyboardConnectionState.Connected || item.State == KeyboardConnectionState.PairedDisconnected)
                {
                    var btnReleaseContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    btnReleaseContent.Children.Add(new FontIcon { Glyph = (string)res["MKSIconRelease"], FontSize = 14 });
                    btnReleaseContent.Children.Add(new TextBlock { Text = L10n.ActionRelease, TextWrapping = TextWrapping.NoWrap });

                    var btnRelease = new Button { Content = btnReleaseContent };
                    btnRelease.Click += (_, _) => onRelease();
                    actionsRow.Children.Add(btnRelease);
                }

                if (item.State != KeyboardConnectionState.Connected)
                {
                    var btnConnectContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    btnConnectContent.Children.Add(new FontIcon { Glyph = (string)res["MKSIconConnect"], FontSize = 14 });
                    btnConnectContent.Children.Add(new TextBlock { Text = L10n.ActionConnect, TextWrapping = TextWrapping.NoWrap });

                    var btnConnect = new Button
                    {
                        Content = btnConnectContent,
                        Style = (Style)res["AccentButtonStyle"]
                    };
                    btnConnect.Click += (_, _) => onConnect();
                    actionsRow.Children.Add(btnConnect);
                }

                Grid.SetColumn(actionsRow, 2);
                grid.Children.Add(actionsRow);
            }

            card.Child = grid;
            return card;
        }

        public static Border CreateEmptyStateCard()
        {
            var res = Application.Current.Resources;
            var emptyCard = new Border
            {
                Background = (Brush)res["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Brush)res["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = (CornerRadius)res["MKSCardCornerRadius"],
                Padding = new Thickness(24, 36, 24, 36)
            };

            var emptyStack = new StackPanel
            {
                Spacing = (double)res["MKSSpacingS"],
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 540
            };

            emptyStack.Children.Add(new FontIcon
            {
                Glyph = (string)res["MKSIconKeyboard"],
                FontSize = 30,
                Foreground = (Brush)res["TextFillColorSecondaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center
            });
            emptyStack.Children.Add(new TextBlock
            {
                Text = L10n.BtEmptyTitle,
                Style = (Style)res["MKSCardTitleTextStyle"],
                HorizontalAlignment = HorizontalAlignment.Center
            });
            emptyStack.Children.Add(new TextBlock
            {
                Text = L10n.BtEmptyBody,
                Style = (Style)res["MKSCardBodyTextStyle"],
                Foreground = (Brush)res["TextFillColorSecondaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            });

            emptyCard.Child = emptyStack;
            return emptyCard;
        }
    }
}
