using System;
using System.Collections.Generic;

namespace RimMonitorWorkTab.ColonistPriority.RunTime.State
{
    internal sealed class WorkTabWorldState
    {
        // Monotonic revision number for client-side sync
        public int Revision;

        public bool ManualPrioritiesEnabled;

        public List<WorkTabWorkType> WorkTypes = new List<WorkTabWorkType>();

        public List<WorkTabMapState> Maps = new List<WorkTabMapState>();
    }

    internal sealed class WorkTabWorkType
    {
        public string Id;
        public string Label;
        public string ShortLabel;
    }

    internal sealed class WorkTabMapState
    {
        public int MapId;
        public string MapLabel;

        public List<WorkTabPawnState> Pawns = new List<WorkTabPawnState>();
    }

    internal sealed class WorkTabPawnState
    {
        public int PawnThingId;
        public string Name;

        // key: WorkTypeDef.defName
        public Dictionary<string, WorkTabCellState> CellsByWorkType =
            new Dictionary<string, WorkTabCellState>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class WorkTabCellState
    {
        public int Priority;

        public int? SkillLevel;
        public int? Passion;

        public bool IdeologyOpposed;

        public float ActivityScore { get; set; }
        public bool RecentlyActive { get; set; }

        public bool Available;
    }
}
