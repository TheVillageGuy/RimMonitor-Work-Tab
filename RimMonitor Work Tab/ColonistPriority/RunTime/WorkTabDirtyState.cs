using System.Threading;

namespace RimMonitorWorkTab.ColonistPriority.RunTime
{
    internal static class WorkTabDirtyState
    {
        private static int dirty;

        public static bool IsDirty => dirty != 0;

        // returns true only on clean → dirty
        public static bool MarkDirty()
        {
            return Interlocked.Exchange(ref dirty, 1) == 0;
        }

        public static void Clear()
        {
            Volatile.Write(ref dirty, 0);
        }
    }
}
