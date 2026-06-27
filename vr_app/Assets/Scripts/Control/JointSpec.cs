namespace OmniBot.VR.Control
{
    /// <summary>
    /// One revolute joint of a robot arm — a C# mirror of
    /// <c>website/lib/garage/robot-config.ts</c> <c>JointSpec</c>. Angles are in
    /// radians. The IK solver clamps every commanded angle to
    /// <see cref="Min"/>/<see cref="Max"/>; <see cref="Home"/> is the rest pose.
    /// </summary>
    public struct JointSpec
    {
        public string Name;
        public string Label;
        public float Min;   // radians
        public float Max;   // radians
        public float Home;  // radians

        public JointSpec(string name, string label, float min, float max, float home)
        {
            Name = name;
            Label = label;
            Min = min;
            Max = max;
            Home = home;
        }
    }
}
