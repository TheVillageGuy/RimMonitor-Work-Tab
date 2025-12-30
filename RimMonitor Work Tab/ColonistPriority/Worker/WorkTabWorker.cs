using RimMonitorWorkTab.ColonistPriority.RunTime;
using RimMonitorWorkTab.ColonistPriority.RunTime.Capture;
using RimMonitorWorkTab.ColonistPriority.RunTime.Deltas;
using RimMonitorWorkTab.ColonistPriority.RunTime.State;
using System.Collections.Generic;
using System.Threading;

namespace RimMonitorWorkTab.ColonistPriority.Worker
{
    internal sealed class WorkTabWorker
    {
        private readonly Thread thread;
        private readonly AutoResetEvent wake;

        private readonly object lockObj = new object();
        private WorkTabRawSnapshot latestBootstrapRaw;

        private WorkTabAuthoritativeModel model;

        private readonly List<WorkTabDelta> deltaBuf = new List<WorkTabDelta>(256);

        public WorkTabWorker()
        {
            wake = new AutoResetEvent(false);

            thread = new Thread(Run);
            thread.IsBackground = true;
            thread.Name = "WorkTabWorker";
            thread.Start();
        }

        /// <summary>
        /// Called only for bootstrap / explicit resync.
        /// </summary>
        public void SetLatest(WorkTabRawSnapshot raw)
        {
            lock (lockObj)
            {
                latestBootstrapRaw = raw;
            }
        }

        public void Wake()
        {
            wake.Set();
        }

        private void Run()
        {
            while (true)
            {
                wake.WaitOne();

                // 1) Bootstrap/resync if provided
                WorkTabRawSnapshot bootstrap;
                lock (lockObj)
                {
                    bootstrap = latestBootstrapRaw;
                    latestBootstrapRaw = null;
                }

                if (bootstrap != null)
                {
                    model = WorkTabStateBuilderWorker.BuildAuthoritativeModel(bootstrap);

                    WorkTabWorldState bootState = WorkTabStateBuilderWorker.BuildWorldState(model);
                    WorkTabPage.PublishStateFromWorker(bootState);
                    WorkTabDirtyState.Clear();

                    // Drain any deltas that piled up during bootstrap
                    deltaBuf.Clear();
                    WorkTabDeltaQueue.DrainTo(deltaBuf);
                    deltaBuf.Clear();

                    continue;
                }

                if (model == null)
                    continue; // no model yet; wait for bootstrap

                // 2) Apply deltas
                deltaBuf.Clear();
                int n = WorkTabDeltaQueue.DrainTo(deltaBuf);
                if (n == 0)
                    continue;

                for (int i = 0; i < deltaBuf.Count; i++)
                {
                    ApplyDelta(deltaBuf[i]);
                }

                // 3) Publish snapshot
                WorkTabWorldState state = WorkTabStateBuilderWorker.BuildWorldState(model);
                WorkTabPage.PublishStateFromWorker(state);
                WorkTabDirtyState.Clear();
            }
        }

        private void ApplyDelta(WorkTabDelta d)
        {
            switch (d.Kind)
            {
                case WorkTabDeltaKind.ManualPrioritiesChanged:
                    model.ManualPrioritiesEnabled = d.BoolValue;
                    return;

                case WorkTabDeltaKind.PriorityChanged:
                    ApplyPriorityChanged(d.PawnThingId, d.WorkTypeId, d.Priority);
                    return;

                case WorkTabDeltaKind.PawnWorkSettingsReapplied:
                    model = null; // force resync
                    return;
            }
        }


        private void ApplyPriorityChanged(int pawnThingId, string workTypeId, int priority)
        {
            if (pawnThingId == 0 || string.IsNullOrEmpty(workTypeId))
                return;

            if (!model.WorkTypeIndex.TryGetValue(workTypeId, out int col))
                return;

            // Find pawn across maps (small list). If you want O(1), add a global pawn index later.
            for (int m = 0; m < model.Maps.Count; m++)
            {
                var map = model.Maps[m];
                if (!map.PawnById.TryGetValue(pawnThingId, out var pawn))
                    continue;

                pawn.EnsureCellCapacity(model.WorkTypes.Count);

                var ce = pawn.Cells[col];
                ce.Priority = priority;
                pawn.Cells[col] = ce;
                return;
            }
        }
    }
}
