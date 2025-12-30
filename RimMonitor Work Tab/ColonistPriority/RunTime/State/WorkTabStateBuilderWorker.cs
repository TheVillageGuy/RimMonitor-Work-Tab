using RimMonitorWorkTab.ColonistPriority.RunTime.Capture;
using RimMonitorWorkTab.ColonistPriority.RunTime.State;

namespace RimMonitorWorkTab.ColonistPriority.RunTime
{
    internal static class WorkTabStateBuilderWorker
    {
        /// <summary>
        /// One-shot bootstrap from raw snapshot into authoritative model.
        /// Called only when model is null or invalidated.
        /// </summary>
        public static WorkTabAuthoritativeModel BuildAuthoritativeModel(
            WorkTabRawSnapshot raw)
        {
            var model = new WorkTabAuthoritativeModel
            {
                ManualPrioritiesEnabled = raw.ManualPrioritiesEnabled
            };

            // WorkTypes (canonical order)
            // WorkTypes (canonical order)
            for (int i = 0; i < raw.WorkTypes.Count; i++)
            {
                var wt = raw.WorkTypes[i];

                model.WorkTypeIndex[wt.DefName] = i;

                model.WorkTypes.Add(new WorkTypeEntry
                {
                    Id = wt.DefName,
                    Label = wt.Label,
                    ShortLabel = wt.ShortLabel
                });
            }


            // Maps / pawns / cells
            foreach (var rawMap in raw.Maps)
            {
                var map = model.GetOrAddMap(rawMap.MapId, rawMap.MapLabel);

                foreach (var rawPawn in rawMap.Pawns)
                {
                    var pawn = map.GetOrAddPawn(
                        rawPawn.PawnThingId,
                        rawPawn.Name);

                    pawn.EnsureCellCapacity(model.WorkTypes.Count);

                    foreach (var kv in rawPawn.CellsByWorkType)
                    {
                        if (!model.WorkTypeIndex.TryGetValue(kv.Key, out int col))
                            continue;

                        var c = kv.Value;

                        pawn.Cells[col] = new CellEntry
                        {
                            Priority = c.Priority,
                            SkillLevel = c.SkillLevel ?? 0,
                            Passion = c.Passion ?? 0,
                            IdeologyOpposed = c.IdeologyOpposed,
                            Available = c.Available
                        };
                    }
                }
            }

            return model;
        }

        /// <summary>
        /// Build immutable web snapshot from authoritative model.
        /// Allocation-heavy by design; worker-only.
        /// </summary>
        public static WorkTabWorldState BuildWorldState(
            WorkTabAuthoritativeModel model)
        {
            var state = new WorkTabWorldState
            {
                ManualPrioritiesEnabled = model.ManualPrioritiesEnabled
            };

            // WorkTypes
            foreach (var wt in model.WorkTypes)
            {
                state.WorkTypes.Add(new WorkTabWorkType
                {
                    Id = wt.Id,
                    Label = wt.Label,
                    ShortLabel = wt.ShortLabel
                });
            }

            // Maps
            foreach (var map in model.Maps)
            {
                var mapState = new WorkTabMapState
                {
                    MapId = map.MapId,
                    MapLabel = map.Label
                };

                foreach (var pawn in map.Pawns)
                {
                    var pawnState = new WorkTabPawnState
                    {
                        PawnThingId = pawn.PawnThingId,
                        Name = pawn.Name
                    };

                    for (int i = 0; i < model.WorkTypes.Count; i++)
                    {
                        var wtId = model.WorkTypes[i].Id;
                        var c = pawn.Cells[i];

                        pawnState.CellsByWorkType[wtId] =
                            new WorkTabCellState
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
