using System;

namespace C4ModelBuilder.Attributes
{
    /// <summary>
    /// Level 2
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class C4ContainerAttribute : Attribute
    {
        public C4ContainerAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }

        public string Description { get; }
    }
}
