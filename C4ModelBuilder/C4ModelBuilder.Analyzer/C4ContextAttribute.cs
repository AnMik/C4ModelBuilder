using System;

namespace Afisha.Tickets.Core.C4
{
    /// <summary>
    /// Level 1
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class C4ContextAttribute : Attribute { }

    /// <summary>
    /// Level 2
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class C4ContainerAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public C4ContainerAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Level 3
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
    public class C4ComponentAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public C4ComponentAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Level 4
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class C4CodeAttribute : Attribute { }



    [C4Context]
    public class OnlineStoreContext { }

    [C4Container("Web Application", "The main web application for users to interact with the store.")]
    public class WebApplication { }

    [C4Component("Product Service", "Service for managing product information.")]
    public class ProductService
    {
        [C4Code]
        public void GetProductById(int id) { }
    }
}
