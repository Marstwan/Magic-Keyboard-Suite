using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace MagicKeyboardSuite.WinUI3.Views.Controls
{
    /// <summary>
    /// Tarjeta reutilizable de ajustes (paridad estructural con SettingsCard en macOS SwiftUI).
    /// </summary>
    [ContentProperty(Name = nameof(ActionContent))]
    public sealed partial class SettingsCard : UserControl
    {
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(
                nameof(Glyph),
                typeof(string),
                typeof(SettingsCard),
                new PropertyMetadata("", OnGlyphChanged));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(SettingsCard),
                new PropertyMetadata("", OnTitleChanged));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(
                nameof(Subtitle),
                typeof(string),
                typeof(SettingsCard),
                new PropertyMetadata("", OnSubtitleChanged));

        public static readonly DependencyProperty ActionContentProperty =
            DependencyProperty.Register(
                nameof(ActionContent),
                typeof(object),
                typeof(SettingsCard),
                new PropertyMetadata(null, OnActionContentChanged));

        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public object? ActionContent
        {
            get => GetValue(ActionContentProperty);
            set => SetValue(ActionContentProperty, value);
        }

        public SettingsCard()
        {
            this.InitializeComponent();
            CardIcon.Glyph = Glyph;
            TitleText.Text = Title;
            SubtitleText.Text = Subtitle;
            ActionPresenter.Content = ActionContent;
        }

        private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card && card.CardIcon != null)
            {
                card.CardIcon.Glyph = e.NewValue as string ?? "";
            }
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card && card.TitleText != null)
            {
                card.TitleText.Text = e.NewValue as string ?? "";
            }
        }

        private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card && card.SubtitleText != null)
            {
                card.SubtitleText.Text = e.NewValue as string ?? "";
            }
        }

        private static void OnActionContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card && card.ActionPresenter != null)
            {
                card.ActionPresenter.Content = e.NewValue;
            }
        }
    }
}
