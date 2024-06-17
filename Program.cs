using MCFCompiler.lib;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace MCFCompiler;
public partial class Program
{
    public static bool SaveLog = false;
    public static bool IsDebugMode = false;
    public static bool NeedCompress = false;
    public static bool StartedReadFiles = false;
    public static bool HasOutputPathBeenSet = false;

    public static string OutputPath = Environment.CurrentDirectory;
    public static string ProgramStartPath = Environment.CurrentDirectory;
    public static string LogFilePath = $"{ProgramStartPath}\\log\\mcfc_{DateTime.Today:yyyy_MM_dd}.txt";

    public static void PrintLog(string msg, bool forced = false)
    {
        msg = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        if (forced || IsDebugMode) Console.WriteLine(msg);
        if (forced || SaveLog)
        {
            if (!Directory.Exists($"{ProgramStartPath}\\log\\")) ProgramStartPath.GetDirectoryInfo().CreateSubdirectory("log");
            File_WriteLine(LogFilePath, msg);
        }
    }

    public static void PrintAndPause(string msg)
    {
        Console.WriteLine(msg);
        Console.Read();
    }

    public static void PrintAndPause(string[] msg)
    {
        foreach (string str in msg) Console.WriteLine(str);
        Console.Read();
    }

    static void Main(string[] args)
    {
        // 设置控制台以 Unicode 编码输出文字
        Console.OutputEncoding = System.Text.Encoding.Unicode;

        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        int opendFiles = 0;
        string prev_arg = string.Empty;
        foreach (string arg in args)
        {
            bool isFlag = arg.StartsWith('-');

            if ((prev_arg == "-o" || prev_arg == "--output") && !isFlag)
            {
                StartedReadFiles = false; // 若 -o 在 -f 之后才指定，不应继续获取文件路径
                var test = PathHelper.TryGetPath(arg, out var path, allowedAccessToExt: true);
                switch (test)
                {
                    case null:
                        PrintLog("指定的输出路径不合法，将在预设路径创建数据包");
                        break;

                    case false:
                        path!.GetDirectoryInfo().Create();
                        PrintLog($"已设置输出目录: {path}");
                        HasOutputPathBeenSet = true;
                        OutputPath = path!;
                        break;

                    case true:
                        PrintLog($"已设置输出目录: {path}");
                        HasOutputPathBeenSet = true;
                        OutputPath = path!;
                        break;
                }
            }
            else if (StartedReadFiles && !isFlag)
            {
                if (File.Exists(arg))
                {
                    opendFiles++;
                    ParseFile(arg);

                    // 重设当前目录
                    Directory.SetCurrentDirectory(ProgramStartPath);
                }
                else PrintAndPause($"无法开启文件: {arg}");
            }

            if (arg == "-h" || arg == "--help") ShowHelp();
            else if (arg == "-l" || arg == "--log") SaveLog = true;
            else if (arg == "-d" || arg == "--debug") IsDebugMode = true;
            else if (arg == "-z" || arg == "--zip") NeedCompress = true;
            else if (arg == "-f" || arg == "--file") StartedReadFiles = true;

            prev_arg = arg;
        }

        if (opendFiles == 0) PrintAndPause("未指定任何文件");
    }

    static void ShowHelp()
    {
        string[] help = 
        [
            "使用说明:",
            "",
            "  -h, --help   \t显示使用说明",
            "  -l, --log    \t输出运行日志",
            "  -d, --debug  \t打印除错信息",
            "  -z, --zip    \t导出成压缩包",
            "  -o, --output \t指定输出目录",
            "  -f, --file   \t编译指定文件",
            "",
            "呼叫方式:",
            "",
            "  mcfc.exe --file <FILE_PATH> [<FILE_PATH>] [<FILE_PATH>] ...\n",
            "  mcfc.exe -l -d -z -o <OUTPUT_DIRECTORY> -f <FILE_PATH>\n",
            "",
            "更多信息请前往 GitHub 查看: https://github.com/script-1024/MCFCompiler"
        ];

        PrintAndPause(help);
    }

    static void ParseFile(string fileName)
    {
        PrintLog($"开始编译 {fileName}");

        var content = File.ReadAllLines(fileName);
        Stack<string> openedFiles = new();

        // 取得指定文件所在的目录信息
        // 由于先前已通过 File.Exist(fileName) 检查，此处不可能返回 null
        var fileParentPath = fileName.GetFileInfo().DirectoryName!;
        if (HasOutputPathBeenSet) Directory.SetCurrentDirectory(OutputPath);
        else { Directory.SetCurrentDirectory(fileParentPath); OutputPath = fileParentPath; }

        // 使用导入文件的名称作为数据包名称
        var datapackName = fileName.Split('.').First();
        var dirInfo = datapackName.GetDirectoryInfo();
        if (dirInfo.Exists) dirInfo.Delete(true);
        dirInfo.Create();
        Directory.SetCurrentDirectory(datapackName);
        PathHelper.DatapackRootPath = dirInfo.FullName;
        PrintLog($"当前目录: {PathHelper.DatapackRootPath}");

        foreach (var line in content)
        {
            // 由 '#>' 开头表示编译器指令
            if (line.StartsWith("#> "))
            {
                string[] cmd = line[3..].SplitString();
                if (!cmd.TryGetValue(0, out var action)) continue;
                switch (action)
                {
                    case "log":
                        if (cmd.Length >= 2) PrintLog(string.Join(" ", cmd[2..]), forced: true);
                        else PrintLog("尝试打印日志，但接收到空文本", forced: true);
                        break;

                    case "root":
                        Directory.SetCurrentDirectory(PathHelper.DatapackRootPath);
                        PrintLog("切换至根目录");
                        break;

                    case "home":
                        if (!string.IsNullOrWhiteSpace(PathHelper.NamespaceHomePath))
                        {
                            Directory.SetCurrentDirectory(PathHelper.NamespaceHomePath);
                            PrintLog("切换至家目录");
                        }
                        else PrintLog($"未指定数据包主目录");
                        break;

                    case "sethome" /* [<Path/To/Directory>] */:
                        {
                            if (cmd.TryGetValue(1, out var input))
                            {
                                if (PathHelper.TryGetPath(input, out var path) == true)
                                {
                                    PathHelper.NamespaceHomePath = path.GetDirectoryInfo().FullName;
                                    PrintLog($"已设置家目录: {PathHelper.NamespaceHomePath}");
                                }
                                else PrintLog("无法访问指定目录，操作未完成");
                            }
                            else
                            {
                                PathHelper.NamespaceHomePath = Directory.GetCurrentDirectory();
                                PrintLog($"已设置当前目录为家目录: {PathHelper.NamespaceHomePath}");
                            }
                            break;
                        }

                    case "init" /* <Namespace> [<PackFormat>] [<PackDescription>] */:
                        {
                            if (cmd.TryGetValue(1, out var namesp))
                            {
                                PathHelper.CurrentNamespace = namesp!;
                                cmd.TryGetValue(2, out var pack_format);
                                cmd.TryGetValue(3, out var description);
                                InitializeDatapack(pack_format, description);
                                PrintLog($"在当前目录以命名空间 \"{namesp}\" 初始化数据包");
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                            break;
                        }

                    case "cd" /* <Path/To/Directory> */: 
                        {
                            if (cmd.TryGetValue(1, out var input))
                            {
                                if (PathHelper.TryGetPath(input, out var path) == true)
                                {
                                    Directory.SetCurrentDirectory(path!);
                                    PrintLog($"切换目录: {path}");
                                }
                                else PrintLog("无法访问指定目录，操作未完成");
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                            break;
                        }

                    case "mkdir" /* <Path/To/Directory> */:
                        {
                            if (cmd.TryGetValue(1, out var input))
                            {
                                var test = PathHelper.TryGetPath(input, out var path);
                                switch (test)
                                {
                                    case null:
                                        PrintLog("指定路径不合法");
                                        continue;

                                    case false:
                                        path!.GetDirectoryInfo().Create();
                                        PrintLog($"创建目录: {path}");
                                        break;

                                    case true:
                                        PrintLog($"目录 {path} 已存在");
                                        break;
                                }
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                            break;
                        }

                    case "rmdir" /* <Path/To/Directory> */:
                        {
                            if (cmd.TryGetValue(1, out var input))
                            {
                                if (PathHelper.TryGetPath(input, out var path) == true)
                                {
                                    PathHelper.RemoveAllFiles(new(path!));
                                    PrintLog("指定目录中的所有文件均已删除完毕");
                                }
                                PrintLog("无法访问指定目录，操作未完成");
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                            break;
                        }

                    case "clear" /* [<Path/To/Directory>] */:
                        {
                            if (cmd.TryGetValue(1, out var input))
                            {
                                if (PathHelper.TryGetPath(input, out var path) == true)
                                {
                                    PathHelper.RemoveAllFiles(new(path!), deleteSelf: false);
                                    PrintLog("已清空指定目录");
                                }
                                PrintLog("无法访问指定目录，操作未完成");
                            }
                            else
                            {
                                PathHelper.RemoveAllFiles(Directory.GetCurrentDirectory().GetDirectoryInfo(), deleteSelf: false);
                                PrintLog("已清空当前目录");
                            }
                        }
                        break;

                    case "open" /* <Path/To/File> */:
                        {
                            if (cmd.TryGetValue(1, out var input))
                            {
                                var test = PathHelper.TryGetPath(input, out var path, isFile: true);
                                switch (test)
                                {
                                    case null:
                                        PrintLog("指定路径不合法");
                                        continue;

                                    case false:
                                        File.WriteAllText(path!, "");
                                        break;
                                }

                                openedFiles.Push(path!);
                                PrintLog($"开启文件: {path}");
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                            break;
                        }

                    case "close":
                        {
                            if (openedFiles.TryPop(out var name))
                            {
                                PrintLog($"关闭文件: {name}");
                            }
                            else PrintLog("所有文件均已关闭");
                            break;
                        }

                    case "del" /* <Path/To/File> */:
                        {
                            if (cmd.TryGetValue(1, out var input))
                            {
                                var test = PathHelper.TryGetPath(input, out var path, isFile: true);
                                switch (test)
                                {
                                    case null:
                                        PrintLog("指定路径不合法");
                                        continue;

                                    case false:
                                        PrintLog("指定文件不存在，操作未完成");
                                        break;

                                    case true:
                                        path!.GetFileInfo().Delete();
                                        PrintLog($"删除文件: {path}");
                                        break;
                                }
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                            break;
                        }

                    default:
                        TryToWriteFile(line);
                        break;
                }
            }

            // 行首的 '##' 会被删去，表示非 mcfunction 文本 (通常用于JSON文件)，这只是为了不在 VSCode 中被标红
            else if (line.StartsWith("##")) TryToWriteFile(line[2..].TrimStart(' '));
            
            // 无法解读的行被视作文件内容直接输出
            else TryToWriteFile(line);
        }

        if (NeedCompress)
        {
            string zipPath = @$"{OutputPath.TrimEnd('\\')}\{datapackName}.zip";
            PrintLog("开始压缩数据包");
            if (File.Exists(zipPath)) File.Delete(zipPath); // 移除已存在的同名文件
            ZipFile.CreateFromDirectory(zipPath[..^4], $"{zipPath}");
            PrintLog($"数据包已导出，文件位置: {zipPath}");
        }

        PrintLog($"已完成 {fileName} 全部的编译操作\n");

        void TryToWriteFile(string line)
        {
            if (!openedFiles.TryPeek(out var path)) return;
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!File.Exists(path)) return;
            File_WriteLine(path, line);
        }

        void InitializeDatapack(string? pack_format, string? description)
        {
            pack_format ??= "4";
            description ??= "A simple datapack";

            PathHelper.DatapackRootPath = Directory.GetCurrentDirectory();
            PathHelper.DatapackRootPath.GetDirectoryInfo().CreateSubdirectory("data\\minecraft\\tags\\functions");
            PathHelper.DatapackRootPath.GetDirectoryInfo().CreateSubdirectory($"data\\{PathHelper.CurrentNamespace}\\functions");

            string[] pack_mcmeta = [
                "{",    
                "    \"pack\": {",
                $"        \"pack_format\": {pack_format},",
                $"        \"description\": \"{description}\"",
                "    }",
                "}"
            ];

            File.WriteAllLines("pack.mcmeta", pack_mcmeta);
            Directory.SetCurrentDirectory("data");
            
            File.WriteAllText(
                $"minecraft\\tags\\functions\\tick.json",
                $"{{ \"values\": [ \"{PathHelper.CurrentNamespace}:tick\" ] }}");
            File.WriteAllText(
                $"minecraft\\tags\\functions\\load.json",
                $"{{ \"values\": [ \"{PathHelper.CurrentNamespace}:load\" ] }}");

            Directory.SetCurrentDirectory($"{PathHelper.CurrentNamespace}");
            PathHelper.NamespaceHomePath = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory("functions");
        }
    }

    public static void File_WriteLine(string path, string line)
    {
        line = line.TrimEnd() + Environment.NewLine;
        File.AppendAllText(path, line);
    }
}

public static class Extensions
{
    public static bool TryGetValue<T>(this T[] arr, int index, [MaybeNull] out T value)
    {
        if (arr is null || index < 0 || index >= arr.Length) { value = default; return false; }
        value = arr[index]; return true;
    }

    public static bool Contains(this string str, char[] characters)
    {
        for (int i=0; i<str.Length; i++)
        {
            foreach (char c in characters) if (str[i] == c) return true;
        }
        return false;
    }

    /// <summary>
    /// 按空格分割字串，但不拆分被单双引号包围的片段
    /// </summary>
    public static string[] SplitString(this string input)
    {
        List<string> parts = new();
        Regex regex = new(@"(""[^""]*""|'[^']*'|\S+)");
        var matches = regex.Matches(input);

        foreach (Match match in matches)
        {
            var value = match.Value;
            if (value.StartsWith('\'') && value.EndsWith('\'')  /* 单引号 */
             || value.StartsWith('\"') && value.EndsWith('\"')) /* 双引号 */
            {
                value = value[1..^1]; // 去除匹配片段前后的引号
            }
            parts.Add(value);
        }
        return [..parts];
    }
}