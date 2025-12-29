namespace RimMonitorWorkTab.ColonistPriority.Model
{
    /// <summary>
    /// Global work assignment mode.
    /// Mirrors RimWorld behavior:
    /// - Manual: priorities visible and editable
    /// - Automatic: priorities hidden, values preserved
    /// </summary>
    internal enum WorkAssignmentMode
    {
        Manual,
        Automatic
    }
}
