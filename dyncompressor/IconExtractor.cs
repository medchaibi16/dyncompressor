using System.Runtime.InteropServices;
using System.Drawing;

public static class IconExtractor
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_SMALLICON = 0x1;
    private const uint SHGFI_LARGEICON = 0x0;

    public static Icon GetSmallIcon(string path)
    {
        SHFILEINFO shinfo = new SHFILEINFO();
        SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo),
            SHGFI_ICON | SHGFI_SMALLICON);

        return Icon.FromHandle(shinfo.hIcon);
    }
}
