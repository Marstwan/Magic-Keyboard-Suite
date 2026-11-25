// Archivo generado por shared/tools/generate.py. NO EDITAR: cambia shared/ y vuelve a generar.
using System.Collections.Generic;

namespace MagicKeyboardSuite.Shared
{
    public sealed class AppleLayoutDefinition
    {
        public string Klid { get; }
        public string Name { get; }
        public string DllName { get; }
        public string LayoutId { get; }
        public string ComponentId { get; }
        public string LangRegex { get; }

        public AppleLayoutDefinition(string klid, string name, string dllName, string layoutId, string componentId, string langRegex)
        {
            Klid = klid;
            Name = name;
            DllName = dllName;
            LayoutId = layoutId;
            ComponentId = componentId;
            LangRegex = langRegex;
        }

        public static readonly IReadOnlyList<AppleLayoutDefinition> All = new AppleLayoutDefinition[]
        {
            new AppleLayoutDefinition("a000040a", "Español / Català (Apple)", "SpanishA.dll", "00c5", "C3364C7C44BC444A88A50459135D35B5", "^(es|ca)"),
            new AppleLayoutDefinition("a0020409", "United States-International (Apple)", "IntlEngA.dll", "00d0", "241A34D0-06DB-405e-8B4E-8CA2FC34D1C7", "^en-INTL"),
            new AppleLayoutDefinition("a0000409", "United States (Apple)", "USA.dll", "00d1", "4E82857C983B44E5A9352052127677D5", "^en-US"),
            new AppleLayoutDefinition("a0000809", "United Kingdom (Apple)", "BritishA.dll", "00c0", "1A4D378083AD454BB4FE02F208614EB6", "^en-GB"),
            new AppleLayoutDefinition("a000040c", "Français (Apple)", "FrenchA.dll", "00c2", "2ECD3C77364749B18E910F9196B420FA", "^fr"),
            new AppleLayoutDefinition("a0000407", "Deutsch (Apple)", "GermanA.dll", "00c3", "B616E2191BF048D4A554E5C6BE224AB4", "^de"),
            new AppleLayoutDefinition("a0000410", "Italiano (Apple)", "ItalianA.dll", "00c4", "6401AAA6058F431181B445C26BEF22D9", "^it"),
            new AppleLayoutDefinition("a0000416", "Português (Apple)", "PortuguA.dll", "00ce", "326773935C8C4597B0738FE2084D44AD", "^pt"),
            new AppleLayoutDefinition("a0000413", "Nederlands (Apple)", "DutchA.dll", "00c1", "3844B95343FB43D68E9695D6E88F016E", "^nl"),
            new AppleLayoutDefinition("a000041d", "Svenska (Apple)", "SwedishA.dll", "00c7", "8CC8067A1BFF4A0FAD38708DE4CD4BF1", "^sv"),
            new AppleLayoutDefinition("a0000406", "Dansk (Apple)", "DanishA.dll", "00cc", "C3996498F423440FB9CE2732A821E7D9", "^da"),
            new AppleLayoutDefinition("a0000414", "Norsk (Apple)", "NorwayA.dll", "00c9", "74BE397ABD8143E4960D38111394D1A3", "^(nb|nn|no)"),
            new AppleLayoutDefinition("a000040b", "Suomi (Apple)", "FinnishA.dll", "00cb", "ECE9937799D242F5AE0CAA446EDEDC62", "^fi"),
            new AppleLayoutDefinition("a000100c", "Swiss (Apple)", "SwissA.dll", "00c6", "CE4C7E2419DE400B8A553E1A5C3DCD04", "^(de|fr|it)-CH"),
            new AppleLayoutDefinition("a0000813", "Belgian (Apple)", "BelgiumA.dll", "00cd", "D70C1682E8F24ED4B5B70AAD37B1BA42", "^(nl|fr)-BE"),
            new AppleLayoutDefinition("a0000415", "Polski (Apple)", "PolishA.dll", "00cf", "D3D2841618E34D09ABBCA0DA34A60FAE", "^pl"),
            new AppleLayoutDefinition("a0000419", "Russian (Apple)", "RussianA.dll", "00c8", "B0F62A69BE9446488ED502E800DBC36C", "^ru"),
        };
    }
}
