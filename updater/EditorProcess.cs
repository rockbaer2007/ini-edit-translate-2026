using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Iet.Update
{
    internal static class EditorProcess
    {
        private const uint QueryLimitedInformation = 0x1000;
        private const uint Synchronize = 0x100000;
        private const uint Completed = 0;
        private const uint TimedOut = 258;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeProcessHandle OpenProcess(uint access, bool inheritHandle, int processId);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder name, ref uint size);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessTimes(SafeProcessHandle process, out long creation, out long exit, out long kernel, out long user);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(SafeProcessHandle process, uint milliseconds);

        internal static bool ProcessExists(int processId)
        {
            // Listing local PIDs does not open the editor's process handle or modules.
            var processes = Process.GetProcesses();
            try
            {
                foreach (var process in processes) if (process.Id == processId) return true;
                return false;
            }
            finally { foreach (var process in processes) process.Dispose(); }
        }

        internal static void WaitForAbsentProcess(int processId, uint milliseconds, Func<int, bool> exists)
        {
            var elapsed = Stopwatch.StartNew();
            while (exists(processId))
            {
                if (elapsed.ElapsedMilliseconds >= milliseconds)
                    throw new IOException("Editor process still exists; access was denied. Installation has not started.");
                Thread.Sleep((int)Math.Min(100L, Math.Max(1L, (long)milliseconds - elapsed.ElapsedMilliseconds)));
            }
        }

        internal static void WaitForExit(UpdateJob job, uint milliseconds = 120000,
            Func<uint, bool, int, SafeProcessHandle> openProcess = null)
        {
            if (job.ProcessId <= 0) throw new IOException("Invalid editor process.");
            var elapsed = Stopwatch.StartNew();
            try
            {
            // One stable handle identifies the original process, even if its PID
            // is later reused. Query the OS image path instead of reading modules
            // in another process, which fails across bitness or during shutdown.
            using (var process = (openProcess ?? OpenProcess)(QueryLimitedInformation | Synchronize, false, job.ProcessId))
            {
                if (process.IsInvalid)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 87) return; // The editor has already exited.
                    throw new Win32Exception(error);
                }
                long creation, exit, kernel, user;
                if (!GetProcessTimes(process, out creation, out exit, out kernel, out user))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                if (DateTime.FromFileTimeUtc(creation).Ticks != job.ProcessStartTicks)
                    throw new IOException("Process identity differs.");
                if (WaitForSingleObject(process, 0) == Completed) return;

                var name = new StringBuilder(32768);
                uint size = (uint)name.Capacity;
                if (!QueryFullProcessImageName(process, 0, name, ref size))
                {
                    int error = Marshal.GetLastWin32Error();
                    if (WaitForSingleObject(process, 0) == Completed) return;
                    throw new Win32Exception(error);
                }
                if (!string.Equals(Path.GetFullPath(name.ToString()), UpdateCore.Target(job.InstallDirectory, UpdateCore.MainExe), StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Process identity differs.");
                uint result = WaitForSingleObject(process, milliseconds);
                if (result == TimedOut) throw new IOException("Application is still running.");
                if (result != Completed) throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            }
            catch (Win32Exception error) when (error.NativeErrorCode == 5)
            {
                // Access denied is not proof of exit. Fail closed unless a local
                // PID snapshot confirms absence; enumeration errors propagate.
                uint remaining = (uint)Math.Max(0L, (long)milliseconds - elapsed.ElapsedMilliseconds);
                WaitForAbsentProcess(job.ProcessId, remaining, ProcessExists);
            }
        }
    }
}
