using RimMonitorWorkTab.ColonistPriority.RunTime.Capture;
using RimMonitorWorkTab.ColonistPriority.Worker;
using Verse;

namespace RimMonitorWorkTab.Core
{
    public sealed class WorkTabGameComponent : GameComponent
    {
        private static WorkTabWorker worker;

        public static void WakeWorker()
        {
            worker?.Wake();
        }

        public WorkTabGameComponent(Game game)
        {
            if (worker == null)
                worker = new WorkTabWorker();
        }

        /// <summary>
        /// One-shot bootstrap after game load.
        /// This is the ONLY automatic full capture.
        /// </summary>
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Bootstrap();
        }

        /// <summary>
        /// Explicit full resync.
        /// Called only from:
        /// - web UI refresh
        /// - user pause toggle
        /// Never from patches.
        /// </summary>
        public static void ForceResync()
        {
            if (Current.Game == null)
                return;

            var comp = Current.Game.GetComponent<WorkTabGameComponent>();
            comp?.Bootstrap();
        }

        private void Bootstrap()
        {
            // Full capture ONCE, on main thread
            WorkTabRawSnapshot raw = WorkTabRawSnapshotBuilder.Capture();

            // Hand off to worker
            worker.SetLatest(raw);
            worker.Wake();
        }
    }
}
