using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore
{
    internal static class ReflectionUtils
    {
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
        internal static MethodInfo DBSetMethod (Type modelType)
        => typeof(DbContext).GetMethods()
                .Where(v => v.Name == "Set" && v.IsGenericMethod).First()
                .MakeGenericMethod(modelType);
        internal static MethodInfo AsNoTrackingMethod(Type modelType)
        => typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo().GetDeclaredMethod("AsNoTracking").MakeGenericMethod(new Type[] { modelType });
        internal static Type GetGenericValueConverter (Type TFrom, Type TTo)
        => typeof(ValueConverter<,>).MakeGenericType(new Type[] { TFrom, TTo });

        internal static Expression MakeAndExpression (IEnumerable<Expression> expressions)
        {
            if (expressions.Count() == 1) return expressions.First();
            Expression e = null;
            for (int i = 0; i < expressions.Count()/2; i++)
            {
                if (e != null)
                {
                    e = Expression.And(e, Expression.Add(expressions.ElementAt(i), expressions.ElementAt(i+1)));
                }
                else
                {
                    e = Expression.Add(expressions.ElementAt(i), expressions.ElementAt(i+1));
                }
            }
            if (e.CanReduce) e = e.Reduce();
            return e;
        }
    }
}