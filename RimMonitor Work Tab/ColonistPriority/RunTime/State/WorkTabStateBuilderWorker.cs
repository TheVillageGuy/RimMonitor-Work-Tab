using RimMonitorWorkTab.ColonistPriority.RunTime.Capture;

namespace RimMonitorWorkTab.ColonistPriority.RunTime.State
{
    internal static class WorkTabStateBuilderWorker
    {
        public static WorkTabWorldState Build(WorkTabRawSnapshot raw)
        {
            var state = new WorkTabWorldState
            {
                ManualPrioritiesEnabled = raw.ManualPrioritiesEnabled
            };

            foreach (var wt in raw.WorkTypes)
            {
                state.WorkTypes.Add(new WorkTabWorkType
                {
                    Id = wt.DefName,
                    Label = wt.Label,
                    ShortLabel = wt.ShortLabel
                });
            }

            foreach (var map in raw.Maps)
            {
                var mapState = new WorkTabMapState
                {
                    MapId = map.MapId,
                    MapLabel = map.MapLabel
                };

                foreach (var pawn in map.Pawns)
                {
                    var pawnState = new WorkTabPawnState
                    {
                        PawnThingId = pawn.PawnThingId,
                        Name = pawn.Name
                    };

                    foreach (var kv in pawn.CellsByWorkType)
                    {
                        var c = kv.Value;

                        pawnState.CellsByWorkType[kv.Key] = new WorkTabCellState
                        {
                            Priority = c.Priority,
                            SkillLevel = c.SkillLevel,
                            Passion = c.Passion,
                            IdeologyOpposed = c.IdeologyOpposed,
                            Available = c.Available
                        };
                    }

                    mapState.Pawns.Add(pawnState);
                }

                state.Maps.Add(mapState);
            }

            return state;
        }
    }
}
