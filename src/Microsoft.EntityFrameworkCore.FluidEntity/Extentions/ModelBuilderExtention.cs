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
            internal Dictionary<string,string> TableFields {get;set;} = new Dictionary<string, string>();
        }
        internal static Dictionary<string,ModelDataNames> getEntities(DbContext context)
        {
            var res = new Dictionary<string,ModelDataNames>();
            var allDBProps = context.GetType().GetProperties();
            Type tDBSet = typeof(DbSet<>);
            Type tFluidNameAttr = typeof(FluidNameAttribute);
            Type tFluidPropAttr = typeof(FluidPropertyNameAttribute);
            Type tNoFluidNameAttr = typeof(NoFluidNameAttribute);
            // enumerate all DBSet<> (entity)
            int k = 1;
            foreach( var prop in allDBProps.Where(p => p.PropertyType.IsGenericType))
            {
                var genericDBSetType = tDBSet.MakeGenericType(prop.PropertyType.GenericTypeArguments);
                if (prop.PropertyType.IsEquivalentTo(genericDBSetType))
                {
                    Type entityType = prop.PropertyType.GenericTypeArguments[0];
                    var FluidName = entityType.GetCustomAttributesData().Where(v => v.AttributeType == tFluidNameAttr).FirstOrDefault();
                    var FluidNameProp = entityType.GetCustomAttributesData().Where(v => v.AttributeType == tFluidPropAttr).FirstOrDefault();
                    if (FluidName != null)
                    {
                        string fname = (FluidName.ConstructorArguments[0].Value as string);
                        string fnameProp = (FluidNameProp.ConstructorArguments[0].Value as string);
                        var entityDescribtor = new ModelDataNames();
                        if (!String.IsNullOrWhiteSpace(fname))
                        {
                            entityDescribtor.EntityType = entityType;
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
                            res.Add(prop.Name,entityDescribtor);
                        }
                    }
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
                eB = RelationalEntityTypeBuilderExtensions.ToTable(eB as EntityTypeBuilder, entity.Value.EntityTableName);
                Type eBGenericType = typeof(EntityTypeBuilder<>).MakeGenericType(new Type[] {entity.Value.EntityType});
                MethodInfo mi = eBGenericType.GetMethods().Where(v => v.Name == "Property" && !v.IsGenericMethod).FirstOrDefault();                
                foreach(var pName in entity.Value.TableFields)
                {
                    var pBuilder = mi.Invoke(eB, new object[] {pName.Key});
                    pBuilder = RelationalPropertyBuilderExtensions.HasColumnName(pBuilder as PropertyBuilder, pName.Value);
                }
            }
            return modelBuilder;
        }
    }
}
