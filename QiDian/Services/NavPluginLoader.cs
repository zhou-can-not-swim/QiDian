using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using QiDian.Contracts;

namespace QiDian.Services;

/// <summary>
/// 导航插件加载器：扫描 Navs 文件夹下的所有 dll，发现并实例化 INavModule 实现。
/// 在主程序 IHost 构建之前调用，以便把插件类型注册进依赖注入。
/// </summary>
public static class NavPluginLoader
{
    public static IReadOnlyList<INavModule> LoadModules(string pluginsDir)
    {
        var modules = new List<INavModule>();
        if (!Directory.Exists(pluginsDir)) return modules;

        foreach (var dll in Directory.GetFiles(pluginsDir, "*.dll"))
        {
            Assembly asm;
            try
            {
                asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
            }
            catch (Exception)
            {
                continue;   // 无法加载的 dll（非托管/损坏）直接跳过
            }

            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 依赖缺失时 GetTypes 会抛此异常，尽量取能取到的类型
                types = ex.Types.Where(t => t != null).ToArray();
            }

            foreach (var t in types)
            {
                if (t is null || !t.IsClass || t.IsAbstract) continue;
                if (!typeof(INavModule).IsAssignableFrom(t)) continue;
                if (t.GetConstructor(Type.EmptyTypes) == null) continue;
                if (Activator.CreateInstance(t) is INavModule m) modules.Add(m);
            }
        }

        return modules.OrderBy(m => m.Order).ToList();
    }
}
