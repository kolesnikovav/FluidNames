using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Represents a plugin for Microsoft.EntityFrameworkCore to support automatically set fluid names.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        internal class ModelDataNames
        {
            internal Type EntityType {get;set;}
            internal string EntityTableName {get;set;}
            internal bool EntityHasNoBaseType {get;set;}
            internal Dictionary<string,string> TableFields {get;set;} = new Dictionary<string, string>();
            internal Dictionary<string,string> EntityKeys {get;set;} = new Dictionary<string, string>();
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
                    if (entityType.GetCustomAttribute(tNoBaseTypeAttr) != null)
                    {
                        entityDescribtor.EntityHasNoBaseType = true;
                    }
                    var FluidName = entityType.GetCustomAttribute(tFluidNameAttr);
                    var FluidNameProp = entityType.GetCustomAttribute(tFluidPropAttr);
                    if (FluidName != null)
                    {
                        string fname = (FluidName as FluidNameAttribute).StartsWith;
                        string fnameProp = (FluidNameProp == null) ? "" : (FluidNameProp as FluidPropertyNameAttribute).StartsWith;
                        
                        if (!String.IsNullOrWhiteSpace(fname))
                        {
                            
                            entityDescribtor.EntityTableName = fname.ToUpper()+ k.ToString();
                            k++;
                            if (!String.IsNullOrWhiteSpace(fnameProp))
                            {
                                Dictionary<string,string> props = new Dictionary<string, string>();
                                int i = 1;
                                foreach(var pInfo in entityType.GetProperties())
                                {
                                    var p = pInfo.GetCustomAttributesData().Where(v => v.AttributeType == tNoFluidNameAttr).FirstOrDefault();
                                    var pCustomPropName = pInfo.GetCustomAttributesData().Where(v => v.AttributeType == tFluidPropAttr).FirstOrDefault();
                                    string fnamePropCustom = fnameProp;
                                    if (p == null)
                                    {
                                        if (pCustomPropName != null)
                                        {
                                            fnamePropCustom = (pCustomPropName.ConstructorArguments[0].Value as string);
                                        }
                                        props.Add(pInfo.Name, fnamePropCustom.ToUpper()+ i.ToString());
                                        i++;
                                    }
                                }
                                entityDescribtor.TableFields = props;
                            }
                        }
                    }
                    // KeyPart attribute proccessing!
                    foreach( var currentProp in entityType.GetProperties())
                    {
                        var IsPropertyPartOfKey = currentProp.GetCustomAttributesData().Where(v => v.AttributeType == tKeyPartAttr).FirstOrDefault();
                        if (IsPropertyPartOfKey != null)
                        {
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
                if (!String.IsNullOrWhiteSpace(entity.Value.EntityTableName))
                {
                    eB = RelationalEntityTypeBuilderExtensions.ToTable(eB as EntityTypeBuilder, entity.Value.EntityTableName);
                    string comment = (eB as EntityTypeBuilder).Metadata.GetComment();
                    comment += String.IsNullOrWhiteSpace(comment) ? " ": "";
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
                    var pBuilder = mi.Invoke(eB, new object[] { pName.Key });
                    pBuilder = RelationalPropertyBuilderExtensions.HasColumnName(pBuilder as PropertyBuilder, pName.Value).HasComment(pName.Key);
                }
            }
            return modelBuilder;
        }
    }
}
