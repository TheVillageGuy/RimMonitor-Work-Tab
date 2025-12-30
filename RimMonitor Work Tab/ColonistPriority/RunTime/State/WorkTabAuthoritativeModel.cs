using System.Collections.Generic;

namespace RimMonitorWorkTab.ColonistPriority.RunTime.State
{
    /// <summary>
    /// Worker-owned authoritative model.
    /// Pure data, mutable, delta-driven.
    /// Never exposed to web or JSON.
    /// </summary>
    internal sealed class WorkTabAuthoritativeModel
    {
        public bool ManualPrioritiesEnabled;

        // Stable ordering captured once
        public readonly List<WorkTypeEntry> WorkTypes = new List<WorkTypeEntry>();
        public readonly Dictionary<string, int> WorkTypeIndex =
            new Dictionary<string, int>();

        public readonly List<MapEntry> Maps = new List<MapEntry>();

        public MapEntry GetOrAddMap(int mapId, string label)
        {
            for (int i = 0; i < Maps.Count; i++)
            {
                if (Maps[i].MapId == mapId)
                    return Maps[i];
            }

            var map = new MapEntry(mapId, label);
            Maps.Add(map);
            return map;
        }
    }

    internal sealed class WorkTypeEntry
    {
        public string Id;
        public string Label;
        public string ShortLabel;
    }

    internal sealed class MapEntry
    {
        public readonly int MapId;
        public string Label;

        public readonly List<PawnEntry> Pawns = new List<PawnEntry>();
        public readonly Dictionary<int, PawnEntry> PawnById =
            new Dictionary<int, PawnEntry>();

        public MapEntry(int mapId, string label)
        {
            MapId = mapId;
            Label = label;
        }

        public PawnEntry GetOrAddPawn(int pawnThingId, string name)
        {
            if (!PawnById.TryGetValue(pawnThingId, out var pawn))
            {
                pawn = new PawnEntry(pawnThingId, name);
                PawnById.Add(pawnThingId, pawn);
                Pawns.Add(pawn);
            }
            return pawn;
        }
    }

    internal sealed class PawnEntry
    {
        public readonly int PawnThingId;
        public string Name;

        // Indexed by WorkTypeIndex
        public CellEntry[] Cells;

        public PawnEntry(int pawnThingId, string name)
        {
            PawnThingId = pawnThingId;
            Name = name;
        }

        public void EnsureCellCapacity(int workTypeCount)
        {
            if (Cells == null || Cells.Length < workTypeCount)
            {
                var newCells = new CellEntry[workTypeCount];
                if (Cells != null)
                    Cells.CopyTo(newCells, 0);
                Cells = newCells;
            }
        }
    }

    internal struct CellEntry
    {
        public int Priority;
        public int SkillLevel;
        public int Passion;
        public bool IdeologyOpposed;
        public bool Available;
    }
}
