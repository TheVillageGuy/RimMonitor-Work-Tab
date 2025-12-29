using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimMonitorWorkTab.RimMonitor;

namespace RimMonitorWorkTab.ColonistPriority.RunTime
{
    /// <summary>
    /// Converts RimMonitor pawn activity data into a Work Tab–specific state model.
    ///
    /// The adapter receives an opaque snapshot object from RimMonitor’s public API
    /// and inspects it using reflection. Relevant values are extracted and mapped
    /// into simple, local data structures that the Work Tab UI can consume.
    /// </summary>
    internal static class WorkTabStateAdapter
    {
        public static WorkTabState BuildState()
        {
            // RimMonitor exposes snapshot data as an opaque object to avoid
            // leaking internal DTOs to external mods.
            object snapshot = RimMonitorAPI.GetPawnActivitySnapshotOpaque();
            if (snapshot == null)
                return WorkTabState.Empty;

            var pawns = new List<WorkTabPawn>();

            // The snapshot is expected to be enumerable, but its concrete type
            // is intentionally unknown to this mod.
            foreach (object entry in Enumerate(snapshot))
            {
                var pawn = BuildPawn(entry);
                if (pawn != null)
                    pawns.Add(pawn);
            }

            return new WorkTabState
            {
                // Global flags may live on the snapshot object itself rather
                // than on individual pawn entries.
                ManualPriorities = ReadBool(snapshot, "ManualPrioritiesEnabled"),
                Pawns = pawns
            };
        }

        /* =========================================================
           Pawn mapping
           ========================================================= */

        private static WorkTabPawn BuildPawn(object entry)
        {
            if (entry == null)
                return null;

            // Property and field names used here must match RimMonitor’s
            // public snapshot shape. Reflection is centralized so changes
            // in RimMonitor only affect this adapter.
            return new WorkTabPawn
            {
                PawnId = ReadInt(entry, "PawnId"),
                Name = ReadString(entry, "ShortName"),
                AvatarUrl = ReadString(entry, "AvatarUrl"),
                Tracked = ReadBool(entry, "Track"),

                // Priorities are represented as a dictionary keyed by work type.
                // The adapter does not assume ordering or completeness.
                Priorities = ReadDictionary(entry, "Priorities")
            };
        }

        /* =========================================================
           Snapshot enumeration
           ========================================================= */

        private static IEnumerable<object> Enumerate(object snapshot)
        {
            // RimMonitor snapshots are enumerable, but not strongly typed.
            // This keeps iteration logic isolated from the rest of the adapter.
            if (snapshot is IEnumerable enumerable)
            {
                foreach (object o in enumerable)
                    yield return o;
            }
        }

        /* =========================================================
           Reflection helpers
           ========================================================= */

        private static string ReadString(object obj, string name)
        {
            object v = ReadMember(obj, name);
            return v != null ? v.ToString() : "";
        }

        private static int ReadInt(object obj, string name)
        {
            object v = ReadMember(obj, name);
            if (v is int i)
                return i;

            if (v != null && int.TryParse(v.ToString(), out i))
                return i;

            return 0;
        }

        private static bool ReadBool(object obj, string name)
        {
            object v = ReadMember(obj, name);
            if (v is bool b)
                return b;

            if (v != null && bool.TryParse(v.ToString(), out b))
                return b;

            return false;
        }

        private static Dictionary<string, int> ReadDictionary(object obj, string name)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            object v = ReadMember(obj, name);
            if (v is IDictionary dict)
            {
                foreach (DictionaryEntry e in dict)
                {
                    string key = e.Key?.ToString();
                    if (string.IsNullOrEmpty(key))
                        continue;

                    int value = 0;
                    if (e.Value is int i)
                        value = i;
                    else if (e.Value != null)
                        int.TryParse(e.Value.ToString(), out value);

                    result[key] = value;
                }
            }

            return result;
        }

        private static object ReadMember(object obj, string name)
        {
            // Properties are preferred over fields to match typical DTO design,
            // but both are supported to avoid coupling to RimMonitor internals.
            Type t = obj.GetType();

            PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null)
                return SafeGet(() => p.GetValue(obj, null));

            FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
                return SafeGet(() => f.GetValue(obj));

            return null;
        }

        private static object SafeGet(Func<object> getter)
        {
            // Snapshot access is isolated behind this helper so that
            // reflection failures do not propagate into UI code.
            try
            {
                return getter();
            }
            catch
            {
                return null;
            }
        }
    }

    /* =========================================================
       Local state models
       ========================================================= */

    internal sealed class WorkTabState
    {
        public static readonly WorkTabState Empty = new WorkTabState
        {
            ManualPriorities = false,
            Pawns = new List<WorkTabPawn>()
        };

        public bool ManualPriorities;
        public List<WorkTabPawn> Pawns;
    }

    internal sealed class WorkTabPawn
    {
        public int PawnId;
        public string Name;
        public string AvatarUrl;
        public bool Tracked;
        public Dictionary<string, int> Priorities;
    }
}
