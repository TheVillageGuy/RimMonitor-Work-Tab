namespace RimMonitorWorkTab.ColonistPriority.Interaction
{
    internal enum WorkTabAction
    {
        IncreasePriority,
        DecreasePriority,
        SetPriority,
        ToggleEnabled
    }

    internal enum WorkTabScope
    {
        Cell,
        Row,
        Column
    }

    internal struct WorkTabIntent
    {
        public WorkTabAction Action;
        public WorkTabScope Scope;

        // Targets
        public int PawnId;          // valid for Cell / Row
        public int WorkTypeIndex;   // valid for Cell / Column

        // Payload
        public byte Priority;       // used by SetPriority
        public bool Enabled;        // used by ToggleEnabled
    }
}
