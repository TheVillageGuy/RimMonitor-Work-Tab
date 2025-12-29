namespace RimMonitorWorkTab.ColonistPriority.Changes
{
    /// <summary>
    /// Represents a single change to a pawn's work settings.
    ///
    /// Produced by the worker thread.
    /// Applied on the main thread only.
    ///
    /// The worker is never allowed to touch RimWorld state directly.
    /// </summary>
    internal struct ColonistPriorityChange
    {
        /// <summary>
        /// Stable pawn id (ThingIDNumber).
        /// </summary>
        public int PawnId;

        /// <summary>
        /// Index of the work type this change applies to.
        /// </summary>
        public int WorkTypeIndex;

        /// <summary>
        /// Whether this change modifies the priority value.
        /// </summary>
        public bool HasPriorityChange;

        /// <summary>
        /// New priority value (only valid if HasPriorityChange is true).
        /// </summary>
        public byte NewPriority;

        /// <summary>
        /// Whether this change modifies the enabled/disabled state.
        /// </summary>
        public bool HasEnabledChange;

        /// <summary>
        /// New enabled state (only valid if HasEnabledChange is true).
        /// </summary>
        public bool NewEnabled;
    }
}
