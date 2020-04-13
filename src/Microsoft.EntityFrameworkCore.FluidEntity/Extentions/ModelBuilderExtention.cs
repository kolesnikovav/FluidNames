using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Represents a plugin for Microsoft.EntityFrameworkCore to support automatically set fluid names.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        internal class ModelIndex
        {  
            internal bool IsUnique {get;set;} = false;
            internal List<string> Properties {get;set;} = new List<string>();
        }
        internal class ModelFields
        {  
            internal string Name {get;set;}
            internal Type Type {get;set;}
            internal bool RenameField {get;set;} = false;
            internal bool NoValueConverter {get;set;} = false;
        }              

        internal class ModelDataNames
        {
            internal Type EntityType {get;set;}
            internal ValueConverter ValueConverter {get;set;}
            internal string EntityTableName {get;set;}
            internal bool RenameTable {get;set;} = false;
            internal bool EntityHasNoBaseType {get;set;}
            internal Dictionary<string,ModelFields> TableFields {get;set;} = new Dictionary<string, ModelFields>();
            internal Dictionary<string,string> EntityKeys {get;set;} = new Dictionary<string, string>();
            internal Dictionary<string,ModelIndex> Indexes {get;set;} = new Dictionary<string, ModelIndex>();
        }
        internal static bool GetKeyPropertyOfEntity(Type TModel, out Type clrType, out string propName)
        {
            clrType = null;
            propName = null;
            Type tKeyAttr = typeof(KeyAttribute);
            foreach (var pInfo in TModel.GetProperties())
            {
                var a = pInfo.GetCustomAttribute(tKeyAttr);
                if (a != null)
                {
                    clrType = pInfo.PropertyType;
                    propName = pInfo.Name;
                    return true;
                }
            }
            return false;
        }
        internal static ValueConverter GetConverter(Type TModel, DbContext context, string nameDBSet )
        {
            Type TClr = null;
            string KeyName = null;
            if (!GetKeyPropertyOfEntity(TModel, out TClr, out KeyName)) return null;
            if (TClr == TModel) return null;
            Type tVConverter = typeof(ValueConverter<,>);
            Type gVConverter  = tVConverter.MakeGenericType(new Type[] {TModel, TClr});
            Type tFunc = typeof(Func<,>);
            Type tFuncModelCLR = tFunc.MakeGenericType(new Type[] {TModel, TClr});
            Type tFuncCLRModel = tFunc.MakeGenericType(new Type[] {TClr, TModel});
            Type tFuncModelFind = tFunc.MakeGenericType(new Type[] {TModel, typeof(bool)});
            Type tIQueryable = typeof(IQueryable<>);
            Type tIQueryableModel = tIQueryable.MakeGenericType(TModel);
            //**** model - clr conversion ************
            ParameterExpression parameterModel = Expression.Parameter(TModel, "v");
            MemberExpression propertyModel = Expression.Property(parameterModel, KeyName);
            var ExpressionModelCLR = Expression.Lambda(tFuncModelCLR, propertyModel, parameterModel); 
            //**** clr - model conversion ************
            ParameterExpression parameterModelFind = Expression.Parameter(TModel, "a");
            MemberExpression propertyModelFind = Expression.Property(parameterModelFind, KeyName); 
            ParameterExpression parameterClrFind = Expression.Parameter(TClr, "v");
            Expression FindPredicate = Expression.Equal(propertyModelFind,parameterClrFind); 
            var eFirstOrDefault = Expression.Lambda(tFuncModelFind, FindPredicate, new ParameterExpression[] {parameterModelFind});           
            MethodInfo mFirstOrDefault = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(v => v.Name == "FirstOrDefault" && v.GetParameters().Count() == 2).First();
            MethodInfo mGFirstOrDefault = mFirstOrDefault.MakeGenericMethod(TModel); 
            MemberExpression propertyDBSet = Expression.Property(Expression.Constant(context), nameDBSet);
            Expression callExpr = Expression.Call(
                                    mGFirstOrDefault ,
                                    new Expression[] {
                                        propertyDBSet,
                                        eFirstOrDefault
                                    }
                                );            
            var ExpressionCLRModel = Expression.Lambda(callExpr,parameterClrFind);
            var res = Activator.CreateInstance(gVConverter,new object[] {ExpressionModelCLR, ExpressionCLRModel, null});
            return res as ValueConverter;

        }
        internal static Dictionary<string,ModelDataNames> getEntities(DbContext context)
        {
            var res = new Dictionary<string,ModelDataNames>();
            var allDBProps = context.GetType().GetProperties();
            Type tDBSet = typeof(DbSet<>);
            Type tFluidNameAttr = typeof(FluidNameAttribute);
            Type tFluidPropAttr = typeof(FluidPropertyNameAttribute);
            Type tNoFluidNameAttr = typeof(NoFluidNameAttribute);
            Type tKeyPartAttr = typeof(KeyPartAttribute);
            Type tNoBaseTypeAttr = typeof(NoBaseTypeAttribute);
            Type tIndexAttr = typeof(IndexAttribute);
            Type tNoValueConverterAttr = typeof(NoValueConverterAttribute);
            // enumerate all DBSet<> (entity)
            int k = 1;
            foreach( var prop in allDBProps.Where(p => p.PropertyType.IsGenericType))
            {
                var genericDBSetType = tDBSet.MakeGenericType(prop.PropertyType.GenericTypeArguments);
                if (prop.PropertyType.IsEquivalentTo(genericDBSetType))
                {
                    Type entityType = prop.PropertyType.GenericTypeArguments[0];
                    var entityDescribtor = new ModelDataNames();
                    entityDescribtor.EntityType = entityType;
                    var NoValueConverter = entityType.GetCustomAttribute(tNoValueConverterAttr);
                    if (entityType.GetCustomAttribute(tNoValueConverterAttr) == null)
                    {
                        try
                        {
                            entityDescribtor.ValueConverter = GetConverter(entityType, context, prop.Name);
                        }
                        catch(Exception e)
                        {
                            // TODO information!!!
                        }
                    }
                    if (entityType.GetCustomAttribute(tNoBaseTypeAttr) != null)
                    {
                        entityDescribtor.EntityHasNoBaseType = true;
                    }
                    var FluidName = entityType.GetCustomAttribute(tFluidNameAttr);
                    var FluidNameProp = entityType.GetCustomAttribute(tFluidPropAttr);
                    string fname = null;
                    string fnameProp = null;
                    if (FluidName != null)
                    {
                        fname = (FluidName as FluidNameAttribute).StartsWith;
                        fnameProp = (FluidNameProp == null) ? "" : (FluidNameProp as FluidPropertyNameAttribute).StartsWith;
                        if (!String.IsNullOrWhiteSpace(fname))
                        {
                            entityDescribtor.RenameTable = true;
                            entityDescribtor.EntityTableName = fname.ToUpper()+ k.ToString();
                            k++;                            
                        }
                    }
                    Dictionary<string,ModelFields> props = new Dictionary<string, ModelFields>();
                    int i = 1;
                    foreach(var pInfo in entityType.GetProperties())
                    {
                        ModelFields fldDescribtion = new ModelFields();
                        fldDescribtion.Type = pInfo.PropertyType;
                        var NoFluidName = pInfo.GetCustomAttribute(tNoFluidNameAttr);
                        if (NoFluidName != null && !String.IsNullOrWhiteSpace(fnameProp))
                        {
                            string fnamePropCustom = fnameProp;
                            var pCustomPropName = pInfo.GetCustomAttribute(tFluidPropAttr);
                            if (pCustomPropName != null)
                            {
                                fnamePropCustom = (pCustomPropName as FluidPropertyNameAttribute).StartsWith;
                            }
                            fnamePropCustom = fnamePropCustom.ToUpper()+ i.ToString();
                            fldDescribtion.RenameField = true;
                            fldDescribtion.Name = fnamePropCustom;
                        }
                        if (pInfo.GetCustomAttribute(tNoValueConverterAttr) != null) 
                        {
                            fldDescribtion.NoValueConverter = true;
                        }
                        props.Add(pInfo.Name, fldDescribtion);
                        i++;
                    } 
                    entityDescribtor.TableFields = props;                 

                    // KeyPart && Index attribute proccessing!
                    foreach( var currentProp in entityType.GetProperties())
                    {
                        var IsPropertyPartOfKey = currentProp.GetCustomAttributesData().Where(v => v.AttributeType == tKeyPartAttr).FirstOrDefault();
                        if (IsPropertyPartOfKey != null)
                        {
                            entityDescribtor.EntityKeys.Add(currentProp.Name, entityType.Name);
                        }
                        var IndexProp = currentProp.GetCustomAttribute(tIndexAttr);
                        if (IndexProp != null)
                        {
                            ModelIndex mIdx = null;
                            if (entityDescribtor.Indexes.TryGetValue((IndexProp as IndexAttribute).StartsWith, out mIdx))
                            {
                                mIdx.IsUnique = (IndexProp as IndexAttribute).IsUnique;
                                mIdx.Properties.Add(currentProp.Name);
                                entityDescribtor.Indexes[(IndexProp as IndexAttribute).StartsWith] = mIdx;
                            }
                            else 
                            {
                                mIdx = new ModelIndex();
                                mIdx.IsUnique = (IndexProp as IndexAttribute).IsUnique;
                                mIdx.Properties.Add(currentProp.Name);
                                entityDescribtor.Indexes.Add((IndexProp as IndexAttribute).StartsWith, mIdx);
                            }
                            entityDescribtor.EntityKeys.Add(currentProp.Name, entityType.Name);
                        }                        
                    }
                    res.Add(prop.Name,entityDescribtor);
                }
            }         
            return res;
        }
        /// <summary>
        /// Create fluid names for each entity and field.
        /// </summary>
        /// <param name="modelBuilder">The <see cref="ModelBuilder"/> to enable fluid names feature.</param>
        /// <param name="context">The <see cref="DbContext"/> Instance of you DBContext to be configured.</param>
        /// <returns>The <see cref="ModelBuilder"/> had enabled fluid names feature.</returns>
        public static ModelBuilder CreateFluidNames(this ModelBuilder modelBuilder, DbContext context)
        {
            var entities = getEntities(context);
            MethodInfo entityBuilderMethod = modelBuilder.GetType().GetMethods()
            .Where(n => n.Name == "Entity" && n.IsGenericMethod)
            .Where(n => n.GetParameters().Count() == 0).FirstOrDefault();
            if (entityBuilderMethod == null)
            {
                throw(new Exception("Not found ModelBuilder<Entity>() method"));
            }
            foreach( var entity in entities)
            {
                MethodInfo EntityBuilderMethod = entityBuilderMethod.MakeGenericMethod(new Type[] { entity.Value.EntityType });
                var eB = EntityBuilderMethod.Invoke(modelBuilder, null);
                if (!String.IsNullOrWhiteSpace(entity.Value.EntityTableName) && entity.Value.RenameTable)
                {
                    eB = RelationalEntityTypeBuilderExtensions.ToTable(eB as EntityTypeBuilder, entity.Value.EntityTableName);
                    string comment = (eB as EntityTypeBuilder).Metadata.GetComment();
                    comment += String.IsNullOrWhiteSpace(comment) ? "": " ";
                    (eB as EntityTypeBuilder).HasComment(comment +  entity.Key);
                }
                if (entity.Value.EntityHasNoBaseType)
                {
                    (eB as EntityTypeBuilder).HasBaseType((Type)null);
                }
                // multipart key
                if (entity.Value.EntityKeys.Count > 0)
                {
                    (eB as EntityTypeBuilder).HasKey(entity.Value.EntityKeys.Keys.ToArray());
                }
               
                foreach(var pName in entity.Value.TableFields)
                {
                    Type eBGenericType = typeof(EntityTypeBuilder<>).MakeGenericType(new Type[] { entity.Value.EntityType });
                    MethodInfo mi = eBGenericType.GetMethods().Where(v => v.Name == "Property" && !v.IsGenericMethod).FirstOrDefault();
                    if (pName.Value.RenameField)
                    {
                        var pBuilder = mi.Invoke(eB, new object[] { pName.Key });
                        pBuilder = RelationalPropertyBuilderExtensions.HasColumnName(pBuilder as PropertyBuilder, pName.Value.Name).HasComment(pName.Key);
                    }
                    // set ValueConverter if it present!
                    var VConverter = entities.Values.FirstOrDefault(v => v.EntityType == pName.Value.Type);
                    if (VConverter != null && !pName.Value.NoValueConverter)
                    {
                        (eB as EntityTypeBuilder).Property(pName.Key).HasConversion(VConverter.ValueConverter);
                    }
                }
                // Indexes
                foreach(var idx in entity.Value.Indexes)
                {
                    (eB as EntityTypeBuilder).HasIndex(idx.Value.Properties.ToArray()).HasName(idx.Key).IsUnique(idx.Value.IsUnique);
                }                
            }
            return modelBuilder;
        }
    }
}
