using System.Collections.Generic;

namespace RimMonitorWorkTab.ColonistPriority.RunTime.Deltas
{
    internal enum WorkTabDeltaKind : byte
    {
        PriorityChanged = 1,
        ManualPrioritiesChanged = 2,
        PawnWorkSettingsReapplied = 3
    }

    internal struct WorkTabDelta
    {
        public WorkTabDeltaKind Kind;

        public int PawnThingId;     // PriorityChanged + PawnWorkSettingsReapplied
        public string WorkTypeId;   // PriorityChanged (defName)
        public int Priority;        // PriorityChanged
        public bool BoolValue;      // ManualPrioritiesChanged
    }

    /// <summary>
    /// Main-thread writes, worker reads. Simple lock-based queue for correctness first.
    /// (If you want ring-buffer later, we can swap it without touching patches/worker.)
    /// </summary>
    internal static class WorkTabDeltaQueue
    {
        private static readonly object lockObj = new object();
        private static readonly List<WorkTabDelta> q = new List<WorkTabDelta>(256);

        public static void Enqueue(WorkTabDelta d)
        {
            lock (lockObj)
            {
                q.Add(d);
            }
        }

        public static int DrainTo(List<WorkTabDelta> sink)
        {
            if (sink == null) return 0;

            lock (lockObj)
            {
                int count = q.Count;
                if (count == 0) return 0;

                sink.AddRange(q);
                q.Clear();
                return count;
            }
        }

        public static void Clear()
        {
            lock (lockObj)
            {
                q.Clear();
            }
        }
    }
}
