using System.Reflection;

namespace PNETGuard;

internal static class AppBranding
{
    public static Icon? GetAppIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "image.ico");
            return File.Exists(iconPath) ? new Icon(iconPath) : null;
        }
    }

    public static Image? GetLogoImage()
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream("PNETGuard.Assets.image.png");
            if (stream is not null)
            {
                using var temporary = Image.FromStream(stream);
                return new Bitmap(temporary);
            }
        }
        catch
        {
            // Fallback abaixo.
        }

        string imagePath = Path.Combine(AppContext.BaseDirectory, "assets", "image.png");
        return File.Exists(imagePath) ? Image.FromFile(imagePath) : null;
    }
}
