using System;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Sets all availible types for property that it can be
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CanBeAttribute : Attribute
    {
        /// <summary>
        /// Sets all availible types for property that it can be
        /// </summary>
        [NotNull]
        public Type[] AvailibleTypes {get;set;}
        /// <summary>
        /// Sets all availible types for property that it can be
        /// </summary>
        public CanBeAttribute( Type[] availibleTypes)
        {
            this.AvailibleTypes = availibleTypes;
        }
    }
}