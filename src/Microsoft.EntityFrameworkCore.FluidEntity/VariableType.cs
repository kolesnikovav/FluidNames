using System;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Represents variable type value
    /// </summary>
    public class VariableType
    {
        /// <summary>
        /// Type of value instance
        /// </summary>
        public Type InstanceType {get;set;}
        /// <summary>
        /// Value of instance
        /// </summary>
        public object Value {get;set;}
        /// <summary>
        /// Default constructor
        /// </summary>
        public VariableType(){}
        /// <summary>
        /// Default constructor
        /// </summary>
        public VariableType (Type type, object val)
        {
            InstanceType = type;
            Value = val;
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        public VariableType(object val)
        {
            InstanceType = val.GetType();
            Value = val;
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        public void SetVal<T> (T val)
        {
            InstanceType = typeof(T);
            Value = val;
        }
        /// <summary>
        /// Retrive stored value
        /// </summary>
        public T GetVal<T> ()
        {
            return (T)Value;
        }
    }
}