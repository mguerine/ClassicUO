using ClassicUO.Configuration;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ClassicUO
{
    internal sealed class ExternalPluginHost : IPluginHost
    {
        private readonly Process _process;
        private readonly string _pipeName;

        public Dictionary<IntPtr, GraphicsResource> GfxResources { get; } = new Dictionary<IntPtr, GraphicsResource>();

        private ExternalPluginHost(Process process)
        {
            _process = process;
            _pipeName = $"ClassicUO.PluginHost.{process.Id}";
        }

        public static IPluginHost TryCreate()
        {
            if (!Settings.GlobalSettings.UseExternalPluginHost)
                return null;
            string path = (Settings.GlobalSettings.ExternalPluginPath ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(path))
                return null;

            string exeDir = CUOEnviroment.ExecutablePath ?? ".";
            string hostExe = Path.Combine(exeDir, "PluginHost.exe");
            if (!File.Exists(hostExe))
            {
                ClassicUO.Utility.Logging.Log.Warn($"PluginHost.exe not found at {hostExe}. Build the solution to copy it.");
                return null;
            }

            string fullPluginPath = path;
            if (!Path.IsPathRooted(path))
                fullPluginPath = Path.Combine(exeDir, path);

            if (!File.Exists(fullPluginPath) && !Directory.Exists(fullPluginPath))
            {
                ClassicUO.Utility.Logging.Log.Warn($"External plugin path not found: {fullPluginPath}");
                return null;
            }

            try
            {
                var si = new ProcessStartInfo(hostExe)
                {
                    Arguments = $"--plugin \"{fullPluginPath}\"",
                    UseShellExecute = false,
                    WorkingDirectory = exeDir
                };
                Process p = Process.Start(si);
                if (p == null)
                    return null;
                ClassicUO.Utility.Logging.Log.Trace($"PluginHost started (PID {p.Id}), pipe: ClassicUO.PluginHost.{p.Id}");
                return new ExternalPluginHost(p);
            }
            catch (Exception ex)
            {
                ClassicUO.Utility.Logging.Log.Warn($"Failed to start PluginHost: {ex.Message}");
                return null;
            }
        }

        public void Initialize() { }

        public void LoadPlugin(string pluginPath) { }

        public void Tick() { }

        public void Closing()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                    _process.Kill();
            }
            catch { }
        }

        public void FocusGained() { }
        public void FocusLost() { }
        public void Connected() { }
        public void Disconnected() { }

        public bool Hotkey(int key, int mod, bool pressed) => true;

        public void Mouse(int button, int wheel) { }

        public void GetCommandList(out IntPtr listPtr, out int listCount)
        {
            listPtr = IntPtr.Zero;
            listCount = 0;
        }

        public unsafe int SdlEvent(SDL.SDL_Event* ev) => 0;

        public void UpdatePlayerPosition(int x, int y, int z) { }

        public bool PacketIn(ArraySegment<byte> buffer) => true;

        public bool PacketOut(Span<byte> buffer) => true;
    }
}
