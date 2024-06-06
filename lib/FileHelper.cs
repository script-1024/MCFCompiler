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
            if (file.Exists)
            {
                Program.PrintLog($"删除文件: {file.Name}");
                try
                {
                    file.Delete();
                }
                catch
                {
                    Program.PrintLog($"删除指定文件 \"{file.Name}\" 时发生错误");
                }
            }
        }

        foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
        {
            RemoveDirectoryAndAllFiles(subDir, name + '\\');
        }

        try
        {
            dirInfo.Delete();
        }
        catch
        {
            Program.PrintLog($"删除指定目录 \"{dirInfo.Name}\" 时发生错误");
        }
    }
}
