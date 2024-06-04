using MCFCompiler;

namespace MCFCompiler.lib;

public static class FileHelper
{
    public static FileInfo GetFileInfo(this string path) => new(path);
    public static DirectoryInfo GetDirectoryInfo(this string path) => new(path);

    public static void RemoveDirectoryAndAllFiles(DirectoryInfo dirInfo, string parent = "")
    {
        string name = dirInfo.Name;
        name = $"{parent}{name}";

        if (!dirInfo.Exists)
        {
            Program.PrintLog($"指定目录 \"{name}\" 不存在，操作未完成");
            return;
        }

        Program.PrintLog($"开始删除目录: {name}");

        foreach (FileInfo file in dirInfo.GetFiles())
        {
            file.Delete();
            Program.PrintLog($"删除文件: {file.Name}");
        }

        foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
        {
            RemoveDirectoryAndAllFiles(subDir, name + '\\');
        }

        dirInfo.Delete();
    }
}
