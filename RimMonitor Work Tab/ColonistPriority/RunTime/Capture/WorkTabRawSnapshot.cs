using System.Collections.Generic;

namespace RimMonitorWorkTab.ColonistPriority.RunTime.Capture
{
    internal sealed class WorkTabRawSnapshot
    {
        public bool ManualPrioritiesEnabled;

        public List<RawWorkType> WorkTypes;
        public List<RawMap> Maps;

        public WorkTabRawSnapshot()
        {
            WorkTypes = new List<RawWorkType>();
            Maps = new List<RawMap>();
        }
    }

    internal sealed class RawWorkType
    {
        public string DefName;
        public string Label;
        public string ShortLabel;
    }

    internal sealed class RawMap
    {
        public int MapId;
        public string MapLabel;

        public List<RawPawn> Pawns;

        public RawMap()
        {
            Pawns = new List<RawPawn>();
        }
    }

    internal sealed class RawPawn
    {
        public int PawnThingId;
        public string Name;

        public Dictionary<string, RawCell> CellsByWorkType;

        public RawPawn()
        {
            CellsByWorkType = new Dictionary<string, RawCell>();
        }
    }

    internal sealed class RawCell
    {
        public int Priority;
        public int? SkillLevel;
        public int? Passion;
        public bool IdeologyOpposed;
        public bool Available;
    }
}
