using LibVLCSharp.Shared;
using System.IO;

namespace QiDian.Nav.DM;

/// <summary>
/// LibVLC 核心实例。libvlc 实例较重，同一个进程内应尽量只建一个，因此全插件共享。
/// 原生库（libvlc.dll / libvlccore.dll / plugins 等）由构建目标随插件一起复制到 Navs 目录，
/// 首次使用时从本插件 DLL 所在目录加载。
/// </summary>
public static class VlcProvider
{
    private static readonly Lazy<LibVLC> _libVlc = new(() =>
    {
        // 本插件 DLL 所在目录即原生库所在目录（构建时复制过去）
        var pluginDir = Path.GetDirectoryName(typeof(VlcProvider).Assembly.Location);

        if (!string.IsNullOrEmpty(pluginDir))
        {
            Core.Initialize(pluginDir);

            // 保险起见显式指定插件目录，避免 libvlc 找不到解码模块
            Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", Path.Combine(pluginDir, "plugins"));
        }

        // --network-caching 提高网页流（m3u8/mp4）的缓冲稳定性
        return new LibVLC("--no-video-title-show", "--network-caching=300");
    });

    public static LibVLC LibVLC => _libVlc.Value;
}
