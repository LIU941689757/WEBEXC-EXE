using System;                         // 提供基本系统功能，例如字符串处理、文件操作等
using System.Diagnostics;             // 用于启动外部进程（执行卸载命令）
using Microsoft.Win32;                // 提供访问 Windows 注册表的功能

namespace GuidUninstaller
{
    class Program
    {
        static void Main()
        {
            // 指定存放 GUID 的文本文件路径
            string filePath = @"C:\Users\qw941\Desktop\1.txt";

            // 如果文件不存在，直接退出程序（不提示）
            if (!System.IO.File.Exists(filePath)) return;

            // 逐行读取文件中的每一个 GUID
            foreach (var line in System.IO.File.ReadAllLines(filePath))
            {
                // 去除行首尾空白字符
                string raw = line.Trim();

                // 如果当前行是空的（跳过）
                if (string.IsNullOrEmpty(raw)) continue;

                // 保证 GUID 格式以大括号包围（{GUID}），注册表键名要求如此
                string guid = raw.StartsWith("{") ? raw : "{" + raw + "}";

                // 根据 GUID 获取对应的卸载命令（从注册表中查找）
                string uninstallCmd = GetUninstallCommand(guid);

                // 如果找不到卸载命令，则跳过
                if (string.IsNullOrEmpty(uninstallCmd)) continue;

                try
                {
                    // 创建一个用于执行卸载命令的进程启动信息对象
                    var psi = new ProcessStartInfo("cmd.exe", "/c " + uninstallCmd)
                    {
                        UseShellExecute = false,                  // 禁用 shell 执行（允许隐藏窗口）
                        CreateNoWindow = true,                    // 不创建新窗口
                        WindowStyle = ProcessWindowStyle.Hidden   // 设置为隐藏窗口
                    };

                    // 启动卸载进程
                    var process = Process.Start(psi);

                    // 等待该卸载进程执行完毕
                    process.WaitForExit();
                }
                catch
                {
                    // 忽略异常（比如卸载失败或命令格式错误），继续处理下一个 GUID
                }
            }
        }

        /// <summary>
        /// 根据 GUID 从注册表中读取卸载命令（仅查找 64 位路径）
        /// </summary>
        /// <param name="guid">产品的 GUID，格式为 {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}</param>
        /// <returns>卸载命令字符串；若未找到则返回 null</returns>
        static string GetUninstallCommand(string guid)
        {
            // 构造目标注册表路径（64 位应用程序卸载项）
            string path = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{guid}";

            // 打开注册表项（使用 64 位视图）
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                                        .OpenSubKey(path))
            {
                // 读取卸载命令字符串（UninstallString 值）
                var value = key?.GetValue("UninstallString") as string;

                if (value != null)
                {
                    // 如果命令中包含 msiexec，则统一替换为静默卸载格式
                    if (value.IndexOf("msiexec", StringComparison.OrdinalIgnoreCase) >= 0)
                        return $"msiexec /x {guid} /quiet /norestart";

                    // 否则，返回原始卸载命令
                    return value;
                }
            }

            // 未找到卸载信息，返回 null
            return null;
        }
    }
}
