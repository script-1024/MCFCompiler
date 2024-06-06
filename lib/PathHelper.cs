using System.Diagnostics.CodeAnalysis;

namespace MCFCompiler.lib;

public static class PathHelper
{
    public static string DatapackRootPath = string.Empty;
    public static string CurrentNamespace = string.Empty;
    public static string NamespaceHomePath = string.Empty;

    public static FileInfo GetFileInfo(this string path) => new(path);
    public static DirectoryInfo GetDirectoryInfo(this string path) => new(path);

    public static bool IsPathCharactersLegal(string? str)
    {
        if (str is null) return false;
        if (str.Contains(':') && (str.Length < 3 || str[1] != ':')) return false;
        if (str.Contains(['*', '\"', '<', '>', '|', '?'])) return false;
        return true;
    }

    public static bool IsSubDirectoryOrFile(string root, string target, bool isFile = false)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(target)) return false;

        var rootInfo = root.GetDirectoryInfo();
        var targetInfo = (isFile) ? target.GetFileInfo().Directory! : target.GetDirectoryInfo();

        if (targetInfo.FullName.StartsWith(rootInfo.FullName)) return true;
        else return false;
    }

    /// <summary>
    /// 评估指定的路径
    /// </summary>
    /// <returns>
    /// <list type="table">
    /// <term>null</term> <description>路径不合法</description><br/>
    /// <term>true/false</term> <description>路径指向的目标 存在/不存在</description>
    /// </list>
    /// </returns>
    public static bool? TryGetPath(this string? input, [MaybeNull] out string path, bool isFile = false, bool allowedAccessToExt = false)
    {
        path = null;

        if (input is null) return null;
        else path = input.Replace("/", "\\");

        if (path.StartsWith(':')) { path = null; return null; }

        // 家目录 --> \data\<namespace>\
        if (path.StartsWith('~'))
        {
            if (!string.IsNullOrWhiteSpace(NamespaceHomePath)) path = NamespaceHomePath + ((path.Length > 1) ? path[1..] : string.Empty);
            else { path = null; return null; }
        }

        // 根目录 --> \
        if (path.StartsWith('\\'))
        {
            if (!string.IsNullOrWhiteSpace(DatapackRootPath)) path = DatapackRootPath + path;
            else { path = null; return null; }
        }

        try
        {
            if (isFile)
            {
                var info = path.GetFileInfo();
                path = info.FullName;
                if (allowedAccessToExt || IsSubDirectoryOrFile(DatapackRootPath, path)) return info.Exists;
                else return null; // 不可访问至数据包根目录之外
            }
            else
            {
                var info = path.GetDirectoryInfo();
                path = info.FullName;
                if (allowedAccessToExt || IsSubDirectoryOrFile(DatapackRootPath, path)) return info.Exists;
                else return null; // 不可访问至数据包根目录之外
            }
        }
        catch { return null; }
    }

    public static void RemoveAllFiles(DirectoryInfo dirInfo, string parent = "", bool deleteSelf = true)
    {
        string name = dirInfo.Name;
        name = $"{parent}{name}";

        if (!dirInfo.Exists)
        {
            Program.PrintLog($"指定目录 \"{name}\" 不存在，操作未完成");
            return;
        }

        if (deleteSelf) Program.PrintLog($"删除目录: {name}");
        else Program.PrintLog($"清空目录: {name}");

        foreach (FileInfo file in dirInfo.GetFiles())
        {
            if (file.Exists)
            {
                Program.PrintLog($"删除文件: {file.Name}");
                file.Delete();
            }
        }

        foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
        {
            RemoveAllFiles(subDir, name + '\\');
        }

        if (deleteSelf) dirInfo.Delete();
    }
}
