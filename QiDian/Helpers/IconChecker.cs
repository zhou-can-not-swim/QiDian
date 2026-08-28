using System;
using System.Runtime.InteropServices;
using System.Text;

public class IconChecker
{
    // 导入 Windows API
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr FindResource(IntPtr hModule, string lpName, string lpType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, string lpType);

    // 枚举资源相关
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EnumResourceNames(IntPtr hModule, string lpType,EnumResNameProc lpEnumFunc, IntPtr lParam);

    private delegate bool EnumResNameProc(IntPtr hModule, string lpType, IntPtr lpName, IntPtr lParam);

    private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
    private const string RT_GROUP_ICON = "#3";  // RT_GROUP_ICON 的资源类型ID

    /// <summary>
    /// 判断exe文件是否包含图标资源
    /// </summary>
    public static bool HasIcon(string filePath)
    {
        // 加载文件为数据文件（不会执行代码）
        IntPtr hModule = LoadLibraryEx(filePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        if (hModule == IntPtr.Zero)
        {
            return false;  // 文件无法加载（可能不是有效的PE文件或路径错误）
        }

        try
        {
            bool hasIcon = false;
            // 使用回调函数枚举所有图标组资源
            EnumResNameProc callback = (hMod, type, name, param) =>
            {
                hasIcon = true;
                return false;  // 找到一个就停止枚举
            };

            // 开始枚举 RT_GROUP_ICON 类型的资源
            bool result = EnumResourceNames(hModule, RT_GROUP_ICON, callback, IntPtr.Zero);

            // 注意：EnumResourceNames返回false不一定代表失败，也可能是回调函数返回false导致的
            // 所以主要靠hasIcon变量来判断
            return hasIcon;
        }
        finally
        {
            FreeLibrary(hModule);  // 记得释放
        }
    }

    public static bool HasIconSimple(string filePath)
    {
        IntPtr hModule = LoadLibraryEx(filePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        if (hModule == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            // 查找第一个图标资源 (ID = 1)
            IntPtr hRes = FindResource(hModule, (IntPtr)1, RT_GROUP_ICON);
            return hRes != IntPtr.Zero;
        }
        finally
        {
            FreeLibrary(hModule);
        }
    }
}