namespace OmniBot.VR.UI.Selection
{
    /// <summary>
    /// The four steps of the spatial "add robot" flow, mirroring the website's
    /// <c>RobotSelector</c> (categories → types → models → name + confirm).
    /// The <see cref="RobotSelectionController"/> walks through these in order;
    /// the Back button steps to the previous step.
    /// </summary>
    public enum SelectionStep
    {
        Category,
        Type,
        Model,
        Name,
        Confirm,
    }

    /// <summary>
    /// Mutable state accumulated as the user walks the selection flow — the
    /// chosen category id, robot type id, hardware model id, and the user-typed
    /// name. On <c>Confirm</c> this is handed to
    /// <c>GarageClient.AddUserRobot</c> to write a row to Supabase
    /// <c>user_robots</c>.
    /// </summary>
    public class SelectionState
    {
        public string CategoryId;
        public string RobotTypeId;
        public string HardwareModelId;
        public string RobotName;
        public SelectionStep Step = SelectionStep.Category;
    }
}
