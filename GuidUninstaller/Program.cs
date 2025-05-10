using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace GuidUninstaller
{
    class Program
    {
        static void Main()
        {
            // 文本文件路径，每行一个 GUID，可带或不带花括号
            string filePath = @"C:\Users\qw941\OneDrive\桌面\WEBEX\WebexGUID.txt.txt";

            // 如果文件不存在，则提示并退出
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine("找不到 GUID 文件，程序退出。");
                return;
            }

            // 逐行读取 GUID 并尝试卸载
            foreach (var line in System.IO.File.ReadAllLines(filePath))
            {
                string raw = line.Trim();
                if (string.IsNullOrEmpty(raw))
                    continue;   // 跳过空行

                // 确保 GUID 带花括号
                string guid = raw.StartsWith("{") ? raw : "{" + raw + "}";
                Console.WriteLine($"\n处理 GUID: {guid}");

                // 获取卸载命令
                string uninstallCmd = GetUninstallCommand(guid);
                if (string.IsNullOrEmpty(uninstallCmd))
                {
                    Console.WriteLine("未找到对应卸载命令，跳过。");
                    continue;
                }

                Console.WriteLine("卸载命令：" + uninstallCmd);

                try
                {
                    // 以隐藏窗口方式执行卸载命令
                    var psi = new ProcessStartInfo("cmd.exe", "/c " + uninstallCmd)
                    {
                        UseShellExecute = false,              // 必须为 false 才能隐藏窗口
                        CreateNoWindow = true,                // 不创建窗口
                        WindowStyle = ProcessWindowStyle.Hidden // 隐藏窗口
                    };

                    var process = Process.Start(psi);
                    process.WaitForExit();
                    Console.WriteLine("卸载完成，退出码：" + process.ExitCode);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("执行失败：" + ex.Message);
                }
            }

            Console.WriteLine("\n所有 GUID 处理完毕，按任意键退出。");
            Console.ReadKey();
        }

        // 仅在 64 位注册表中查找卸载命令
        static string GetUninstallCommand(string guid)
        {
            string path = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{guid}";
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                                       .OpenSubKey(path))
            {
                var value = key?.GetValue("UninstallString") as string;
                if (value != null)
                {
                    // 如果是 MSI 卸载串，统一静默卸载
                    if (value.IndexOf("msiexec", StringComparison.OrdinalIgnoreCase) >= 0)
                        return $"msiexec /x {guid} /quiet /norestart";
                    return value;
                }
            }
            return null; // 未找到卸载信息
        }
    }
}
