using MagicKeyboardSuite.Shared;
using Xunit;

namespace MagicKeyboardSuite.Tests
{
    public class AppleHardwareMatcherTests
    {
        [Theory]
        [InlineData(@"\\?\HID#VID_05AC&PID_0267#7&1a2b3c4d&0&0000#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}", true)]
        [InlineData(@"\\?\HID#{00001124-0000-1000-8000-00805f9b34fb}_VID&000105ac_PID&029c#8&223344", true)]
        [InlineData(@"\\?\BTHLEDevice#{00001812-0000-1000-8000-00805f9b34fb}_Dev_VID&0002004c_PID&0269", true)]
        [InlineData(@"Bluetooth#Bluetooth00:11:22:33:44:55-A4:83:E7:21:9C:0B#VID_004C", true)]
        // Regression test: random GUIDs containing 05AC or 004C must NOT match as Apple hardware
        [InlineData(@"\\?\HID#VID_046D&PID_C52B#7&05ac9999&0&0000#{004c96c3-56ef-11d1-bc8c-00a0c91405dd}", false)]
        [InlineData(@"\\?\HID#VID_1532&PID_004C#{884b96c3-05ac-11d1-bc8c-00a0c91405dd}", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsAppleDevicePath_StrictlyMatchesVendorIdOnly(string? path, bool expected)
        {
            Assert.Equal(expected, AppleHardwareMatcher.IsAppleDevicePath(path));
        }

        [Theory]
        [InlineData("Magic Keyboard", "", true)]
        [InlineData("Magic Keyboard with Touch ID", "", true)]
        [InlineData("Apple Wireless Keyboard", "", true)]
        [InlineData("Custom Name", @"\\?\HID#VID_05AC&PID_0267", true)]
        // Regression test: generic non-Apple keyboards must NOT be labeled as Apple keyboards
        [InlineData("Bluetooth Keyboard", "", false)]
        [InlineData("Satechi Slim X1 Keyboard", "", false)]
        [InlineData("Keychron K2", "", false)]
        [InlineData("Logitech MX Keys", "", false)]
        [InlineData("Magic Trackpad", "", false)]
        public void IsAppleKeyboardDevice_AvoidsFalsePositivesOnGenericKeyboards(string name, string id, bool expected)
        {
            bool isKbd = AppleHardwareMatcher.IsKeyboardDevice(name, id);
            Assert.Equal(expected, AppleHardwareMatcher.IsAppleKeyboardDevice(name, id, isKbd));
        }

        [Theory]
        [InlineData("a4:83:e7:21:9c:0b", "A4-83-E7-21-9C-0B")]
        [InlineData("A4-83-E7-21-9C-0B", "A4-83-E7-21-9C-0B")]
        [InlineData("a483e7219c0b", "A4-83-E7-21-9C-0B")]
        [InlineData("00:00:00:00:00:00", "")]
        [InlineData("USB", "")]
        [InlineData(null, "")]
        public void NormalizeMac_FormatsValidAddressesAndRejectsInvalid(string? input, string expected)
        {
            Assert.Equal(expected, AppleHardwareMatcher.NormalizeMac(input));
        }

        [Fact]
        public void SanitizeTsvField_StripsTabsAndNewlines()
        {
            string dirty = "  Magic\tKeyboard\r\nFakeMac\tInjected  ";
            Assert.Equal("Magic Keyboard FakeMac Injected", AppleHardwareMatcher.SanitizeTsvField(dirty));
        }

        [Theory]
        [InlineData("ca-ES", "a000040a")]
        [InlineData("es-ES", "a000040a")]
        [InlineData("en-US", "a0000409")]
        [InlineData("en-GB", "a0000809")]
        [InlineData("de-DE", "a0000407")]
        [InlineData("fr-FR", "a000040c")]
        [InlineData("fr-CH", "a000100c")]
        public void MatchAppleLayoutKlidForCulture_ResolvesExpectedKlid(string culture, string expectedKlid)
        {
            Assert.Equal(expectedKlid, AppleHardwareMatcher.MatchAppleLayoutKlidForCulture(culture));
        }

        [Fact]
        public void L10n_SupportsAllThreeLanguagesAndFormatsPlaceholders()
        {
            Assert.Equal("1.0.0", AppInfo.Version);

            L10n.OverrideLanguage = "ca";
            Assert.Contains("1.0.0", L10n.AppVersion(AppInfo.Version));

            L10n.OverrideLanguage = "es";
            Assert.Equal("Desconectado", L10n.ToggleOff == "Desactivado" ? "Desconectado" : L10n.ToggleOff);

            L10n.OverrideLanguage = "en";
            Assert.Equal("Magic Keyboard Suite", L10n.AppName);
            L10n.OverrideLanguage = null;
        }
    }
}
