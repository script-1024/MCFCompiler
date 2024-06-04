using MCFCompiler.lib;
using System.Diagnostics.CodeAnalysis;

namespace MCFCompiler;
public class Program
{
    public static bool IsDebugMode = false;
    public static bool IsReadFileMode = false;
    public const string Minecraft_Tags_Functions = "minecraft\\tags\\functions\\";

    public static void PrintLog(string msg)
    {
        if (!IsDebugMode) return;
        Console.WriteLine($"[Debug] {msg}");
    }

    public static void PrintAndPause(string msg)
    {
        Console.WriteLine(msg);
        Console.Read();
    }

    public static void PrintAndPause(string[] msg)
    {
        foreach (string str in msg)
        {
            Console.WriteLine(str);
        }

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
        foreach (string arg in args)
        {
            if (IsReadFileMode)
            {
                if (File.Exists(arg))
                {
                    opendFiles++;
                    ParseFile(arg);
                    continue;
                }
                else PrintAndPause($"无法开启文件: {arg}");
                return;
            }

            if (arg == "-h" || arg == "--help") ShowHelp();
            if (arg == "-d" || arg == "--debug") IsDebugMode = true;
            if (arg == "-f" || arg == "--file") IsReadFileMode = true;
        }

        if (opendFiles == 0) PrintAndPause("未指定任何文件");
    }

    static void ShowHelp()
    {
        string[] help = 
        [
            "使用说明：",
            "-h | --help 显示此说明",
            "-d | --debug 进入除错模式",
            "-f | --file 将后续所有参数视为文件名，任意文件无法开启都将导致整个程序结束运行"
        ];

        PrintAndPause(help);
    }

    static void ParseFile(string fileName)
    {
        var content = File.ReadAllLines(fileName);
        Stack<string> openedFiles = new();

        // 取得指定文件所在的目录信息
        // 由于先前已通过 File.Exist(fileName) 检查，此处不可能返回 null
        string rootDirectory = fileName.GetFileInfo().Directory!.FullName + '\\';
        string currentNamespace = string.Empty, datapackRootPath = string.Empty;

        Directory.SetCurrentDirectory(rootDirectory);
        PrintLog($"当前目录: {rootDirectory}");

        foreach (var line in content)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // 由 '#>' 开头表示编译器指令
            if (line.StartsWith("#> "))
            {
                string[] cmd = line[3..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (!cmd.TryGetValue(0, out var action)) continue;
                switch (action)
                {
                    case "root":
                        Directory.SetCurrentDirectory(rootDirectory);
                        PrintLog("切换至根目录");
                        break;

                    case "home":
                        if (!string.IsNullOrWhiteSpace(datapackRootPath))
                        {
                            Directory.SetCurrentDirectory(datapackRootPath);
                            PrintLog("切换至数据包主目录");
                        }
                        else PrintLog($"未指定数据包主目录");
                        break;

                    case "sethome":
                        {
                            if (cmd.TryGetValue(1, out var path))
                            {
                                path = path!.Replace('/', '\\');
                                datapackRootPath = path.GetDirectoryInfo().FullName;
                                PrintLog($"已指定数据包主目录: {datapackRootPath}");
                            }
                            else
                            {
                                datapackRootPath = Directory.GetCurrentDirectory().GetDirectoryInfo().FullName;
                                PrintLog($"已指定当前目录为数据包主目录: {datapackRootPath}");
                            }
                            break;
                        }

                    case "init":
                        {
                            if (cmd.TryGetValue(1, out var namesp))
                            {
                                currentNamespace = namesp!;
                                cmd.TryGetValue(2, out var pack_format);
                                cmd.TryGetValue(3, out var description);
                                InitializeDatapack(pack_format, description);
                                PrintLog($"在当前目录以命名空间 \"{namesp}\" 初始化数据包");
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                            break;
                        }

                    case "cd": 
                        {
                            if (cmd.TryGetValue(1, out var dir))
                            {
                                dir = dir!.Replace('/', '\\');
                                if (dir.GetDirectoryInfo().Exists)
                                {
                                    Directory.SetCurrentDirectory(dir);
                                    PrintLog($"切换至目录: {dir}");
                                }
                                else PrintLog("无法访问指定目录，操作未完成");
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                            break;
                        }

                    case "mkdir":
                        {
                            if (cmd.TryGetValue(1, out var subdir))
                            {
                                subdir = subdir!.Replace('/', '\\');
                                Directory.GetCurrentDirectory().GetDirectoryInfo().CreateSubdirectory(subdir);
                                PrintLog($"创建子目录: {subdir}");
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                            break;
                        }

                    case "rmdir":
                        {
                            if (cmd.TryGetValue(1, out var dir))
                            {
                                FileHelper.RemoveDirectoryAndAllFiles(new(dir!));
                                PrintLog("指定目录中的所有文件均已删除完毕");
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                            break;
                        }

                    case "open":
                        {
                            if (cmd.TryGetValue(1, out var file))
                            {
                                file = file!.Replace('/', '\\');
                                openedFiles.Push(file);
                                PrintLog($"开启文件: {file}");
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                        }
                        break;

                    case "close":
                        {
                            if (openedFiles.TryPop(out var closedFileName))
                            {
                                PrintLog($"关闭文件: {closedFileName}");
                            }
                            else PrintLog("所有文件均已关闭");
                            break;
                        }

                    case "del":
                        {
                            if (cmd.TryGetValue(1, out var file))
                            {
                                file = file!.Replace('/', '\\');
                                if (File.Exists(file))
                                {
                                    file.GetFileInfo().Delete();
                                    PrintLog($"删除文件: {file}");
                                }
                                else PrintLog("指定文件不存在，操作未完成");
                            }
                            else PrintLog($"命令 \"{action}\" 缺少必要参数");
                        }
                        break;

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

        void TryToWriteFile(string line)
        {
            if (!openedFiles.TryPeek(out var path)) return;
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!File.Exists(path)) return;
            File.AppendAllText(path, line);
        }

        void InitializeDatapack(string? pack_format, string? description)
        {
            pack_format ??= "4";
            description ??= "A simple datapack";

            Directory.GetCurrentDirectory().GetDirectoryInfo().CreateSubdirectory(currentNamespace);
            Directory.SetCurrentDirectory(currentNamespace);
            Directory.GetCurrentDirectory().GetDirectoryInfo().CreateSubdirectory("data");

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
            datapackRootPath = Directory.GetCurrentDirectory().GetDirectoryInfo().FullName;
            Directory.GetCurrentDirectory().GetDirectoryInfo().CreateSubdirectory(Minecraft_Tags_Functions);
            File.WriteAllText(
                $"{Minecraft_Tags_Functions}tick.json",
                $"{{ \"values\": [ \"{currentNamespace}:tick\"] }}");
            File.WriteAllText(
                $"{Minecraft_Tags_Functions}load.json",
                $"{{ \"values\": [ \"{currentNamespace}:load\"] }}");
            Directory.SetCurrentDirectory(datapackRootPath);
            Directory.GetCurrentDirectory().GetDirectoryInfo().CreateSubdirectory($"{currentNamespace}\\functions");
            Directory.SetCurrentDirectory($"{currentNamespace}\\functions");
        }
    }
}

public static class Extensions
{
    public static bool TryGetValue<T>(this T[] arr, int index, [MaybeNull] out T value)
    {
        if (arr is null || index < 0 || index >= arr.Length) { value = default; return false; }
        value = arr[index]; return true;
    }
}