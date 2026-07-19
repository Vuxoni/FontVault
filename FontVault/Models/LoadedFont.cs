using Microsoft.UI.Xaml.Media;
using System;

namespace FontVault.Models
{
    public class LoadedFont
    {
        public string Name { get; set; } = "";
        public string FamilyName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public bool IsPermanent { get; set; }
        public bool PreviewAvailable { get; set; } = true;
        public string Status =>
            IsPermanent ? "Installed" : "Temporary";
        public FontFamily PreviewFontFamily
        {
            get
            {
                if (!PreviewAvailable ||
                    string.IsNullOrWhiteSpace(FamilyName))
                {
                    return new FontFamily("Segoe UI");
                }

                return new FontFamily(FamilyName);
            }
        }
    }
}