using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Text.Json;

namespace Microsoft.EntityFrameworkCore
{
    internal static class ReflectionUtils
    {
        internal static MethodInfo JSONSerializeMethod ()
        => typeof(JsonSerializer).GetMethods(BindingFlags.Static| BindingFlags.Public)
            .Where( m => m.IsGenericMethod && m.Name == "Serialize" && m.ReturnType == typeof(string)).FirstOrDefault()
            .MakeGenericMethod(new Type[] { typeof(VariableType) });
        internal static MethodInfo JSONDeserializeMethod ()
        => typeof(JsonSerializer).GetMethods(BindingFlags.Static| BindingFlags.Public)
            .Where( m => m.IsGenericMethod && m.Name == "Deserialize").FirstOrDefault()
            .MakeGenericMethod(new Type[] { typeof(VariableType) });

        internal static MethodInfo FindMethod (ValueConverterMethod mFind, Type modelType)
        {
            if (mFind == ValueConverterMethod.FirstOrDefault)
            {
                return typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(v => v.Name == "FirstOrDefault" && v.GetParameters().Count() == 2).First()
                .MakeGenericMethod(modelType);
            }
            else
            {
               return typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(v => v.Name == "First" && v.GetParameters().Count() == 2).First()
                .MakeGenericMethod(modelType);
            }            
        }
        internal static ConstructorInfo VarTypeFromObjectCtor ()
        => typeof(VariableType).GetConstructors().Where(v => v.GetParameters().Count() == 1 && v.GetParameters().First().ParameterType == typeof(object) ).First();

        internal static Type GetGenericValueConverter (Type TFrom, Type TTo)
        => typeof(ValueConverter<,>).MakeGenericType(new Type[] { TFrom, TTo });
    }
}