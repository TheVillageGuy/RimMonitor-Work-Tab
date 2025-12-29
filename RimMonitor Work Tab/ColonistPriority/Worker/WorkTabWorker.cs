using System.Threading;
using RimMonitorWorkTab.ColonistPriority.RunTime;
using RimMonitorWorkTab.ColonistPriority.RunTime.Capture;
using RimMonitorWorkTab.ColonistPriority.RunTime.State;

namespace RimMonitorWorkTab.ColonistPriority.Worker
{
    internal sealed class WorkTabWorker
    {
        private readonly Thread thread;
        private readonly AutoResetEvent wake;

        private readonly object lockObj = new object();
        private WorkTabRawSnapshot latestRaw;

        public WorkTabWorker()
        {
            wake = new AutoResetEvent(false);

            thread = new Thread(Run);
            thread.IsBackground = true;
            thread.Name = "WorkTabWorker";
            thread.Start();
        }

        public void SetLatest(WorkTabRawSnapshot raw)
        {
            lock (lockObj)
            {
                latestRaw = raw;
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

                WorkTabRawSnapshot raw;
                lock (lockObj)
                {
                    raw = latestRaw;
                    latestRaw = null;
                }

                if (raw == null)
                    continue;


                WorkTabWorldState state = WorkTabStateBuilderWorker.Build(raw);

                // publish state into existing WorkTabPage pipeline
                WorkTabPage.PublishStateFromWorker(state);

                WorkTabDirtyState.Clear();

            }
        }
    }
}
