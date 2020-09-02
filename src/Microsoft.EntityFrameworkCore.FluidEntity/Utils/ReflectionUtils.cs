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
        internal static MethodInfo ContainsKeyMethod ()
        => typeof(Dictionary<Type, string>).GetMethods()
            .Where( m => !m.IsGenericMethod && m.Name == "ContainsKey").FirstOrDefault();

        internal static MethodInfo TryGetValueMethod ()
        => typeof(Dictionary<Type, string>).GetMethods()
            .Where( m => !m.IsGenericMethod && m.Name == "TryGetValue").FirstOrDefault();


        internal static MethodInfo GetTypeMethod ()
        => typeof(object).GetMethods()
            .Where( m => m.Name == "GetType").FirstOrDefault();

        internal static Expression callExpressionGetType (ParameterExpression parameterModel)
        =>  Expression.Call( parameterModel, GetTypeMethod());

        internal static Expression IsEntityExpression(ParameterExpression parameterModel, ConstantExpression entityCollection)
        {
            Expression callExprGetType = Expression.Call(
                                    parameterModel,
                                    GetTypeMethod()
                                );
            Expression callContainsKey = Expression.Call(
                entityCollection,
                ContainsKeyMethod (),
                new Expression[] {
                    callExpressionGetType (parameterModel)
                }
            );
            return Expression.Lambda(callContainsKey, parameterModel );
        }
        internal static Expression GetModelCLRExpression(Type TModel)
        {
            Type ClrType = null;
            string keyName = String.Empty;
            var q = ModelBuilderExtensions.GetKeyPropertyOfEntity(TModel, out ClrType, out keyName);

            Type tFunc = typeof(Func<,>);
            Type tFuncModelCLR = tFunc.MakeGenericType(new Type[] { TModel, ClrType });

            ParameterExpression p = Expression.Parameter(TModel,"e");
            MemberExpression propertyModel = Expression.Property(p, keyName);
            return Expression.Lambda(tFuncModelCLR, propertyModel, p);
        }
        internal static Expression GetKeyFieldExpression(ParameterExpression parameterModel, ConstantExpression entityCollection)
        {
            MethodInfo m = typeof(ReflectionUtils).GetMethod("GetEntityCollectionName");
            Expression callEntityCollectionName = Expression.Call(
                m,
                new Expression[] {
                    entityCollection,
                    callExpressionGetType (parameterModel)
                }
            );
            return callEntityCollectionName;
        }

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