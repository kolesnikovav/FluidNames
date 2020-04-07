using System;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Mark property as a part of entity key 
    /// </summary>       
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyPartAttribute: Attribute
    {
        /// <summary>
        /// KeyPart attribute default constructor.
        /// Mark property as a part of entity key 
        /// </summary>         
        public KeyPartAttribute(){}        
    }    
}