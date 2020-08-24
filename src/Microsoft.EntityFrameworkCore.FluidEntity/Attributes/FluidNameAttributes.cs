using System;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Marks Entity type enable fluid table name and set the start symbols of database table name
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FluidNameAttribute: Attribute
    {
        /// <summary>
        /// Sets start substring for entity names and makes enable fluid names functionality
        /// </summary>
        [NotNull]
        public string StartsWith {get;set;}
        /// <summary>
        /// Sets start substring for entity names and makes enable fluid names functionality
        /// </summary>
        public FluidNameAttribute(string startsWith)
        {
            this.StartsWith = startsWith;
        }
    }
    /// <summary>
    /// Define starts symbols of properties fields name
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Property)]
    public class FluidPropertyNameAttribute: Attribute
    {
        /// <summary>
        /// Sets start substring for field names
        /// </summary>
        [NotNull]
        public string StartsWith {get;set;}
        /// <summary>
        /// Sets start substring for field names
        /// </summary>
        public FluidPropertyNameAttribute(string startsWith)
        {
            this.StartsWith = startsWith;
        }
    }
    /// <summary>
    /// Disable fluid names for property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NoFluidNameAttribute: Attribute
    {
        /// <summary>
        /// Disable fluid names for property
        /// </summary>
        public bool NoApply {get;set;}
        /// <summary>
        /// NoFluidName attribute constructor.
        /// Fluid names functionality will not be applied if val = true
        /// </summary>
        public NoFluidNameAttribute(bool val)
        {
            this.NoApply = val;
        }
        /// <summary>
        /// NoFluidName attribute default constructor.
        /// Fluid names functionality will not be applied
        /// </summary>
        public NoFluidNameAttribute()
        {
            this.NoApply = true;
        }
    }
}
