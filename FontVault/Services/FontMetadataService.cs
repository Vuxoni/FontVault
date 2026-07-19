using System.Drawing.Text;
using System.IO;

namespace FontVault.Services
{
    public static class FontMetadataService
    {
        public static string? GetFontFamilyName(string fontPath)
        {
            if (string.IsNullOrWhiteSpace(fontPath))
                return null;
            if (!File.Exists(fontPath))
                return null;

            try
            {
                using var fontCollection = new PrivateFontCollection();
                fontCollection.AddFontFile(fontPath);

                if (fontCollection.Families.Length == 0)
                    return null;

                return fontCollection.Families[0].Name;
            }
            catch
            {
                return null;
            }
        }
    }
}