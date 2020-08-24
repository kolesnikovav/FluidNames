using System;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Indicates, what method is being used to convert from Model to Clr type
    /// </summary>
    public enum ValueConverterMethod
    {
        /// <summary>
        /// Method FirstOrDefault is being used to convert Model to Clr. Clr type will be nullable
        /// </summary>
        FirstOrDefault,
        /// <summary>
        /// Method First is being used to convert Model to Clr.
        /// </summary>

        First
    }

    /// <summary>
    /// Indicates, what method is being used to convert from Model to Clr type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ValueConverterMethodAttribute : Attribute
    {
        /// <summary>
        /// Indicates, what method is being used to convert from Model to Clr type
        /// </summary>
        public ValueConverterMethod Method { get; set; }
        /// <summary>
        /// Sets the metod to convert Model to Clr type
        /// </summary>

        public ValueConverterMethodAttribute(ValueConverterMethod method)
        {
            this.Method = method;
        }
    }
}