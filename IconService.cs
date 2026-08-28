using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DockManager;

internal static class IconService
{
    // =====================================================
    // CARTELLA DELLE ICONE
    // =====================================================

    private static string DockManagerFolder =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "DockManager");

    public static string IconsFolder =>
        Path.Combine(
            DockManagerFolder,
            "icons");


    // =====================================================
    // RISOLUZIONE PERCORSO ICONE PERSONALIZZATE
    // =====================================================

    public static string ResolvePath(
        string icon)
    {
        if (Path.IsPathRooted(icon))
            return icon;

        return Path.GetFullPath(
            Path.Combine(
                DockManagerFolder,
                icon));
    }


    // =====================================================
    // IMPORTAZIONE ICONA
    // =====================================================

    public static string ImportIcon(
        string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                "Icona non trovata.",
                sourcePath);
        }

        string extension =
            Path.GetExtension(
                sourcePath)
            .ToLowerInvariant();

        Directory.CreateDirectory(
            IconsFolder);


        // -------------------------------------------------
        // ICO / PNG
        // -------------------------------------------------

        if (extension == ".ico" ||
            extension == ".png")
        {
            string fileName =
                $"icon_{Guid.NewGuid():N}{extension}";

            string destination =
                Path.Combine(
                    IconsFolder,
                    fileName);

            File.Copy(
                sourcePath,
                destination,
                true);

            return Path.Combine(
                "icons",
                fileName);
        }


        // -------------------------------------------------
        // EXE / LNK
        // -------------------------------------------------

        if (extension == ".exe" ||
            extension == ".lnk")
        {
            string fileName =
                $"icon_{Guid.NewGuid():N}.png";

            string destination =
                Path.Combine(
                    IconsFolder,
                    fileName);

            if (ExtractIconToPng(
                    sourcePath,
                    destination))
            {
                return Path.Combine(
                    "icons",
                    fileName);
            }

            throw new InvalidOperationException(
                "Non è stato possibile estrarre l'icona.");
        }


        throw new NotSupportedException(
            "Sono supportati file .ico, .png, .exe e .lnk.");
    }


    // =====================================================
    // CARICAMENTO ICONA PERSONALIZZATA
    // =====================================================

    public static ImageSource? Load(
        string? icon,
        int decodeSize = 64)
    {
        if (string.IsNullOrWhiteSpace(icon))
            return null;

        string path =
            ResolvePath(icon);

        if (!File.Exists(path))
            return null;

        try
        {
            BitmapImage image = new();

            image.BeginInit();

            image.UriSource =
                new Uri(
                    Path.GetFullPath(path),
                    UriKind.Absolute);

            image.CacheOption =
                BitmapCacheOption.OnLoad;

            image.DecodePixelWidth =
                decodeSize;

            image.DecodePixelHeight =
                decodeSize;

            image.EndInit();

            image.Freeze();

            return image;
        }
        catch
        {
            return null;
        }
    }


    // =====================================================
    // ESTRAZIONE ICONA DA EXE / LNK
    // =====================================================

    private static bool ExtractIconToPng(
        string sourcePath,
        string destinationPath)
    {
        SHFILEINFO info = new();

        IntPtr result =
            SHGetFileInfo(
                sourcePath,
                0,
                ref info,
                (uint)Marshal.SizeOf(
                    typeof(SHFILEINFO)),
                SHGFI_ICON |
                SHGFI_LARGEICON);

        if (result == IntPtr.Zero ||
            info.hIcon == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            ImageSource icon =
                Imaging.CreateBitmapSourceFromHIcon(
                    info.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

            icon.Freeze();

            if (icon is not BitmapSource bitmap)
                return false;

            PngBitmapEncoder encoder =
                new();

            encoder.Frames.Add(
                BitmapFrame.Create(bitmap));

            using FileStream stream =
                new(
                    destinationPath,
                    FileMode.Create,
                    FileAccess.Write);

            encoder.Save(stream);

            return true;
        }
        finally
        {
            DestroyIcon(
                info.hIcon);
        }
    }


    // =====================================================
    // COSTANTI SHGETFILEINFO
    // =====================================================

    private const uint SHGFI_ICON =
        0x000000100;

    private const uint SHGFI_LARGEICON =
        0x000000000;


    // =====================================================
    // STRUTTURA SHFILEINFO
    // =====================================================

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;

        public int iIcon;

        public uint dwAttributes;

        [MarshalAs(
            UnmanagedType.ByValTStr,
            SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(
            UnmanagedType.ByValTStr,
            SizeConst = 80)]
        public string szTypeName;
    }


    // =====================================================
    // SHGETFILEINFO
    // =====================================================

    [DllImport(
        "Shell32.dll",
        CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);


    // =====================================================
    // DESTROY ICON
    // =====================================================

    [DllImport(
        "User32.dll")]
    private static extern bool DestroyIcon(
        IntPtr hIcon);


    // =====================================================
    // COSTANTI SHGETSTOCKICONINFO
    // =====================================================

    private const uint SHGSI_ICON =
        SHGFI_ICON;

    private const uint SHGSI_LARGEICON =
        SHGFI_LARGEICON;


    // =====================================================
    // STRUTTURA SHSTOCKICONINFO
    // =====================================================

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Unicode)]
    private struct SHSTOCKICONINFO
    {
        public uint cbSize;

        public IntPtr hIcon;

        public int iSysImageIndex;

        public int iIcon;

        [MarshalAs(
            UnmanagedType.ByValTStr,
            SizeConst = 260)]
        public string szPath;
    }


    // =====================================================
    // SHGETSTOCKICONINFO
    // =====================================================

    [DllImport(
        "Shell32.dll",
        CharSet = CharSet.Unicode)]
    private static extern int SHGetStockIconInfo(
        uint siid,
        uint uFlags,
        ref SHSTOCKICONINFO psii);


    // =====================================================
    // CARICAMENTO ICONA WINDOWS
    // =====================================================

    public static ImageSource? LoadWindowsIcon(
        string path)
    {
        const string thisPC =
            "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";


        // -------------------------------------------------
        // QUESTO PC
        // -------------------------------------------------

        if (path == thisPC)
        {
            return LoadStockIcon();
        }


        // -------------------------------------------------
        // FILE / CARTELLE NORMALI
        // -------------------------------------------------

        SHFILEINFO info = new();

        IntPtr result =
            SHGetFileInfo(
                path,
                0,
                ref info,
                (uint)Marshal.SizeOf(
                    typeof(SHFILEINFO)),
                SHGFI_ICON |
                SHGFI_LARGEICON);

        if (result == IntPtr.Zero ||
            info.hIcon == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            ImageSource icon =
                Imaging.CreateBitmapSourceFromHIcon(
                    info.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

            icon.Freeze();

            return icon;
        }
        finally
        {
            DestroyIcon(
                info.hIcon);
        }
    }


    // =====================================================
    // ICONA SPECIALE DI WINDOWS
    // =====================================================

    private static ImageSource? LoadStockIcon()
    {
        SHSTOCKICONINFO info =
            new()
            {
                cbSize =
                    (uint)Marshal.SizeOf(
                        typeof(SHSTOCKICONINFO))
            };


        // 94 = SIID_DESKTOPPC
        int result =
            SHGetStockIconInfo(
                94,
                SHGSI_ICON |
                SHGSI_LARGEICON,
                ref info);

        if (result != 0 ||
            info.hIcon == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            ImageSource icon =
                Imaging.CreateBitmapSourceFromHIcon(
                    info.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

            icon.Freeze();

            return icon;
        }
        finally
        {
            DestroyIcon(
                info.hIcon);
        }
    }
}