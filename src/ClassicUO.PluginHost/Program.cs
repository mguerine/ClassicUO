using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ClassicUO.PluginHost
{
    internal static class Program
    {
        private const string ConfigFileName = "PluginHostConfig.json";

        [STAThread]
        static int Main(string[] args)
        {
            string pluginPath = GetPluginPath(args);
            if (string.IsNullOrWhiteSpace(pluginPath))
            {
                Console.WriteLine("Usage: PluginHost.exe [--plugin \"path\\to\\ClassicAssist.dll\"]");
                Console.WriteLine("Or set PluginPath in PluginHostConfig.json next to the exe.");
                return 1;
            }

            if (!File.Exists(pluginPath))
            {
                Console.WriteLine("Plugin not found: " + pluginPath);
                return 1;
            }

            string pipeName = $"ClassicUO.PluginHost.{Process.GetCurrentProcess().Id}";
            Console.WriteLine($"Pipe: {pipeName}");
            using (var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Thread serverThread = new Thread(() =>
                {
                    try
                    {
                        pipe.WaitForConnection();
                        byte[] buf = Encoding.UTF8.GetBytes($"Connected|{pipeName}");
                        pipe.Write(buf, 0, buf.Length);
                        pipe.Flush();
                        while (pipe.IsConnected)
                        {
                            Thread.Sleep(500);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Pipe error: " + ex.Message);
                    }
                });
                serverThread.IsBackground = true;
                serverThread.Start();

                try
                {
                    Assembly asm = Assembly.LoadFrom(pluginPath);
                    Type engineType = asm.GetType("Assistant.Engine");
                    if (engineType == null)
                    {
                        foreach (Type t in asm.GetExportedTypes())
                        {
                            MethodInfo m = t.GetMethod("Install", BindingFlags.Public | BindingFlags.Static);
                            if (m != null && m.GetParameters().Length == 1)
                            {
                                engineType = t;
                                break;
                            }
                        }
                    }

                    if (engineType != null)
                    {
                        MethodInfo install = engineType.GetMethod("Install", BindingFlags.Public | BindingFlags.Static);
                        if (install != null)
                        {
                            IntPtr zero = IntPtr.Zero;
                            install.Invoke(null, new object[] { zero });
                            Console.WriteLine("Plugin Install() called (stub mode). Use ClassicUO with External Plugin Host and connect to this process.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Load plugin: " + ex.Message);
                }

                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }

            return 0;
        }

        private static string GetPluginPath(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "--plugin", StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1].Trim('"');
                }
            }

            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
            string configPath = Path.Combine(exeDir, ConfigFileName);
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    int idx = json.IndexOf("\"PluginPath\"", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        idx = json.IndexOf('"', idx + 12) + 1;
                        int end = json.IndexOf('"', idx);
                        if (end > idx)
                        {
                            string path = json.Substring(idx, end - idx).Trim();
                            if (!string.IsNullOrEmpty(path))
                            {
                                if (!Path.IsPathRooted(path))
                                    path = Path.Combine(exeDir, path);
                                return path;
                            }
                        }
                    }
                }
                catch { }
            }

            return null;
        }
    }
}
