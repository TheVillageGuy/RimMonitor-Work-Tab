using System;
using System.Collections;
using System.Collections.Generic;
using RimMonitorWorkTab.ColonistPriority.RunTime.State;
using RimMonitorWorkTab.RimMonitor;

namespace RimMonitorWorkTab.ColonistPriority.RunTime.Enrichment
{
    /// <summary>
    /// Enriches the Work Tab world state with activity information
    /// sourced from RimMonitor.
    ///
    /// The enrichment layer is display-only. It does not modify
    /// priorities or any RimWorld gameplay state.
    /// </summary>
    internal static class WorkTabRimMonitorEnricher
    {
        public static void Enrich(WorkTabWorldState state)
        {
            // RimMonitor exposes activity data as an opaque snapshot.
            // The concrete shape is intentionally not depended on here.
            object snapshot = RimMonitorAPI.GetPawnActivitySnapshotOpaque();
            if (snapshot == null)
                return;

            // Build a lookup keyed by pawn thing ID for fast matching.
            var byPawnId = BuildPawnLookup(snapshot);

            for (int m = 0; m < state.Maps.Count; m++)
            {
                var map = state.Maps[m];

                for (int p = 0; p < map.Pawns.Count; p++)
                {
                    var pawn = map.Pawns[p];

                    if (!byPawnId.TryGetValue(pawn.PawnThingId, out object rmPawn))
                        continue;

                    EnrichPawn(pawn, rmPawn);
                }
            }
        }

        /* =========================================================
           Snapshot interpretation
           ========================================================= */

        private static Dictionary<int, object> BuildPawnLookup(object snapshot)
        {
            var dict = new Dictionary<int, object>();

            if (snapshot is IEnumerable enumerable)
            {
                foreach (object entry in enumerable)
                {
                    int pawnId = ReadInt(entry, "PawnId");
                    if (pawnId > 0 && !dict.ContainsKey(pawnId))
                        dict[pawnId] = entry;
                }
            }

            return dict;
        }

        private static void EnrichPawn(WorkTabPawnState pawn, object rmPawn)
        {
            // RimMonitor may expose per-job or per-work-type statistics.
            // We only read what is present and ignore missing fields.
            
            IDictionary jobs = ReadDictionary(rmPawn, "Jobs");
            if (jobs == null)
                return;

            foreach (DictionaryEntry e in jobs)
            {
                string workTypeId = e.Key?.ToString();
                if (string.IsNullOrEmpty(workTypeId))
                    continue;

                if (!pawn.CellsByWorkType.TryGetValue(workTypeId, out var cell))
                    continue;

                object stats = e.Value;
                if (stats == null)
                    continue;

                // Activity score is treated as a normalized or relative value.
                // The exact scale is left to RimMonitor.
                cell.ActivityScore = ReadFloat(stats, "Score") ?? 0f;
                cell.RecentlyActive = ReadBool(stats, "Recent") == true;
            }
        }

        /* =========================================================
           Reflection helpers
           ========================================================= */

        private static int ReadInt(object obj, string name)
        {
            object v = ReadMember(obj, name);
            if (v is int i)
                return i;

            if (v != null && int.TryParse(v.ToString(), out i))
                return i;

            return 0;
        }

        private static float? ReadFloat(object obj, string name)
        {
            object v = ReadMember(obj, name);
            if (v is float f)
                return f;

            if (v is double d)
                return (float)d;

            if (v != null && float.TryParse(v.ToString(), out f))
                return f;

            return null;
        }

        private static bool? ReadBool(object obj, string name)
        {
            object v = ReadMember(obj, name);
            if (v is bool b)
                return b;

            if (v != null && bool.TryParse(v.ToString(), out b))
                return b;

            return null;
        }

        private static IDictionary ReadDictionary(object obj, string name)
        {
            object v = ReadMember(obj, name);
            return v as IDictionary;
        }

        private static object ReadMember(object obj, string name)
        {
            var t = obj.GetType();

            var p = t.GetProperty(name);
            if (p != null)
                return SafeGet(() => p.GetValue(obj, null));

            var f = t.GetField(name);
            if (f != null)
                return SafeGet(() => f.GetValue(obj));

            return null;
        }

        private static object SafeGet(Func<object> getter)
        {
            try { return getter(); }
            catch { return null; }
        }
    }
}
