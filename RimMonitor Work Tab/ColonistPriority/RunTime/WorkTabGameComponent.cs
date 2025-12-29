using RimMonitorWorkTab.ColonistPriority.RunTime.Capture;
using RimMonitorWorkTab.ColonistPriority.Worker;
using Verse;

namespace RimMonitorWorkTab.Core
{
    public sealed class WorkTabGameComponent : GameComponent
    {
        private static WorkTabWorker worker;

        public WorkTabGameComponent(Game game)
        {
            if (worker == null)
                worker = new WorkTabWorker();
        }

        public static void RequestRebuild()
        {
            Game game = Current.Game;
            if (game == null)
                return;

            WorkTabGameComponent comp = game.GetComponent<WorkTabGameComponent>();
            if (comp != null)
                comp.Rebuild();
        }

        private void Rebuild()
        {
            WorkTabRawSnapshot raw = WorkTabRawSnapshotBuilder.Capture();
            worker.SetLatest(raw);
            worker.Wake();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            // Game is fully loaded, pawns exist now
            RequestRebuild();
        }
    }
}
