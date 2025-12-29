namespace RimMonitorWorkTab.ColonistPriority.Interaction
{
    internal sealed class WorkTabDragContext
    {
        public WorkTabAction Action;
        public WorkTabScope Scope;

        public byte Priority;
        public bool Enabled;

        public int WorkTypeIndex; // for column drags
    }
}
