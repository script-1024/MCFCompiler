# MCF 编译器
[中文](README.md) | [English](README_EN.md)

专为编写 Minecraft 数据包设计的一站式工具

<br/>

## 命令行参数
```
-h, --help    显示使用说明
-l, --log     输出运行日志
-d, --debug   打印除错信息
-z, --zip     导出成压缩包
-o, --output  指定输出目录
-f, --file    编译指定文件
```

<br/>

## 呼叫方式

### 编译指定文件
可连续指定多个文件，编译器将按顺序处理

```bat
call mcfc.exe -f <FILE_PATH> [<FILE_PATH>] [<FILE_PATH>] ...
```

### 打印除错信息
指示编译器向控制台打印运行时的各种状态

```bat
call mcfc.exe -d -f <FILE_PATH> [<FILE_PATH>] ...
```

还推荐再添加 `--log` 参数以输出日志文件

### 设置输出目录
修改数据包编译完的输出位置

```bat
call mcfc.exe -f FILE_1 -o C:\output -f FILE_2
```

上述命令将使得 `FILE_1` 输出至预设目录（源文件所在目录）、`FILE_2` 输出至 `C:\output`

### 打包和压缩数据包
以 `.zip` 格式保存数据包

```bat
call mcfc.exe -z -f <FILE_PATH> [<FILE_PATH>] ...
```

若只想打包特定文件，可改用下方的命令，此命令只会打包 `FILE_2`

```bat
call mcfc.exe -f FILE_1 -z -f FILE_2
```