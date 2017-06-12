using System.Collections.Generic;

namespace Overlay.Hook
{
    public static class HookManager
    {
        private static readonly List<int> HookedProcesses = new List<int>();

        public static void AddHookedProcess(int processId)
        {
            lock (HookedProcesses)
            {
                HookedProcesses.Add(processId);
            }
        }

        public static void RemoveHookedProcess(int processId)
        {
            lock (HookedProcesses)
            {
                HookedProcesses.Remove(processId);
            }
        }

        public static bool IsHooked(int processId)
        {
            lock (HookedProcesses)
            {
                return HookedProcesses.Contains(processId);
            }
        }
    }
}
