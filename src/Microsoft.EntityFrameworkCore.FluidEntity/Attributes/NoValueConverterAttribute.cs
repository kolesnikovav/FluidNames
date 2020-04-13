using System;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Disable create ValueConverter for entity/property 
    /// </summary>       
    [AttributeUsage(AttributeTargets.Class| AttributeTargets.Property)]
    public class NoValueConverterAttribute: Attribute
    {
        /// <summary>
        /// Disable create ValueConverter for entity/property.
        /// </summary>         
        public NoValueConverterAttribute(){}        
    }    
}