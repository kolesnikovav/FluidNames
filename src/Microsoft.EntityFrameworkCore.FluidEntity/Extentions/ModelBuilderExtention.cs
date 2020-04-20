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
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Represents a plugin for Microsoft.EntityFrameworkCore to support automatically set fluid names.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        private static readonly Dictionary<string, ModelDataNames> existingTableNames = new Dictionary<string, ModelDataNames>();
        private static readonly Dictionary<string,ModelDataNames> contextEntities = new Dictionary<string, ModelDataNames>();

        private static bool contextEntitiesExists = false;
        internal class ModelIndex
        {
            internal string IndexName {get;set;}
            internal bool IsUnique {get;set;} = false;
            internal List<string> Properties {get;set;} = new List<string>();
            internal string Fields;
        }
        internal class ModelFields
        {  
            internal string Name {get;set;}
            internal Type Type {get;set;}
            internal bool RenameField {get;set;} = false;
            internal bool NoValueConverter {get;set;} = false;

            internal bool? IsRequired {get;set;} = null;
            internal string DefaultSQLValueForReference {get;set;}
            internal ValueConverter ValueConverterProperty {get;set;}
        }              

        internal class ModelDataNames
        {
            internal Type EntityType {get;set;}
            internal string TypeFullName {get;set;}
            internal ValueConverter ValueConverter {get;set;}
            internal string EntityTableName {get;set;}
            internal bool RenameTable {get;set;} = false;
            internal bool EntityHasNoBaseType {get;set;}
            internal bool UseXminAsConcurrencyToken {get;set;}
            internal Dictionary<string,ModelFields> TableFields {get;set;} = new Dictionary<string, ModelFields>();
            internal Dictionary<string,string> EntityKeys {get;set;} = new Dictionary<string, string>();
            internal Dictionary<string,ModelIndex> Indexes {get;set;} = new Dictionary<string, ModelIndex>();
        }
        internal static string GetStringFromList(List<string> content)
        {
            string result = "";
            for (int i = 0; i < content.Count; i++)
            {
                string next = (i < content.Count - 1) ? "," : "";
                result += content[i] + next;
            }
            return result;
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
        internal static ValueConverter GetConverter(Type TModel, DbContext context, string nameDBSet, ValueConverterMethod modelClrMethod = ValueConverterMethod.FirstOrDefault )
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
            LambdaExpression ExpressionCLRModel = null;
            ParameterExpression parameterModelFind = Expression.Parameter(TModel, "a");
            ParameterExpression parameterClrFind = Expression.Parameter(TClr, "v");
            MemberExpression propertyModelFind = Expression.Property(parameterModelFind, KeyName);
            Expression FindPredicate = Expression.Equal(propertyModelFind, parameterClrFind);
            var eFirstOrDefault = Expression.Lambda(tFuncModelFind, FindPredicate, new ParameterExpression[] { parameterModelFind });
            MethodInfo mFirstOrDefault = null;
            MethodInfo mGFirstOrDefault = null;

            if (modelClrMethod == ValueConverterMethod.FirstOrDefault)
            {
                mFirstOrDefault = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(v => v.Name == "FirstOrDefault" && v.GetParameters().Count() == 2).First();
                mGFirstOrDefault = mFirstOrDefault.MakeGenericMethod(TModel);
            }
            else if (modelClrMethod == ValueConverterMethod.First)
            {
                mFirstOrDefault = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(v => v.Name == "First" && v.GetParameters().Count() == 2).First();
                mGFirstOrDefault = mFirstOrDefault.MakeGenericMethod(TModel);
            }

            MemberExpression propertyDBSet = Expression.Property(Expression.Constant(context), nameDBSet);
            Expression callExpr = Expression.Call(
                                    mGFirstOrDefault,
                                    new Expression[] {
                                        propertyDBSet,
                                        eFirstOrDefault
                                    }
                                );
            ExpressionCLRModel = Expression.Lambda(callExpr, parameterClrFind);
            var res = Activator.CreateInstance(gVConverter, new object[] { ExpressionModelCLR, ExpressionCLRModel, null });
            return res as ValueConverter;
        }
        internal static void ReadExistingNames(string filePath)
        {
            existingTableNames.Clear();
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            XmlNode node = doc.DocumentElement.SelectSingleNode("/fluidnames/entities");
            foreach (XmlNode cNode in node.ChildNodes)
            {
                var entityName = cNode.Attributes["name"].Value;
                var entityType = cNode.Attributes["type"].Value;
                var tablename  = cNode.Attributes["tablename"].Value;
                ModelDataNames mdn = new ModelDataNames();
                mdn.EntityTableName = tablename;
                mdn.TypeFullName = entityType;
                mdn.TableFields = new Dictionary<string, ModelFields>();
                var nodeFields = cNode.ChildNodes;
                foreach (XmlNode nChld in nodeFields)
                {
                    if (nChld.Name == "fields")
                    {
                        foreach (XmlNode nfield in nChld.ChildNodes)
                        {
                            var fieldName = nfield.Attributes["propname"].Value;
                            var assignedName = nfield.Attributes["name"].Value;
                            var fieldType = nfield.Attributes["type"].Value;
                            if (!String.IsNullOrWhiteSpace(assignedName))
                            {
                                mdn.TableFields.Add(fieldName, new ModelFields {Name = assignedName });
                            }
                        }
                    } else if (nChld.Name == "indexes")
                    {
                        foreach (XmlNode nidx in nChld.ChildNodes)
                        {
                            var idxName = nidx.Attributes["name"].Value;
                            var idxfields = nidx.Attributes["fields"].Value;
                            if (!String.IsNullOrWhiteSpace(idxName))
                            {
                                mdn.Indexes.Add(idxName, new ModelIndex {Fields = idxfields});
                            }
                        }                    
                    }
                }
                existingTableNames.Add(entityName, mdn);
            }
        }
        internal static void SaveExistingNames(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode root = doc.CreateElement("fluidnames");
            XmlNode node = doc.CreateElement("entities");
            foreach( var entity in contextEntities)
            {
                XmlNode nodeEntity = doc.CreateElement("entity");
                var a = doc.CreateAttribute("name");
                a.Value = entity.Key;
                var b = doc.CreateAttribute("type");
                b.Value = entity.Value.EntityType.FullName;
                var c = doc.CreateAttribute("tablename");
                c.Value = entity.Value.EntityTableName;                
                nodeEntity.Attributes.Append(a);
                nodeEntity.Attributes.Append(b);
                nodeEntity.Attributes.Append(c);

                XmlNode fldNode = doc.CreateElement("fields");
                foreach (var fld in entity.Value.TableFields)
                {
                    var fname = doc.CreateAttribute("name");
                    fname.Value = fld.Value.Name;
                    var ftype = doc.CreateAttribute("type");
                    ftype.Value = fld.Value.Type.FullName; 

                    var fn = doc.CreateAttribute("propname");
                    fn.Value = fld.Key;                                       
                    XmlNode fldNodeCurrent = doc.CreateElement("field");
                    fldNodeCurrent.Attributes.Append(fn);
                    fldNodeCurrent.Attributes.Append(fname);
                    fldNodeCurrent.Attributes.Append(ftype);
                    fldNode.AppendChild(fldNodeCurrent);
                }
                nodeEntity.AppendChild(fldNode);                
                
                XmlNode idxNode = doc.CreateElement("indexes");
                foreach (var idx in entity.Value.Indexes)
                {
                    var i = doc.CreateAttribute("fields");
                    i.Value = GetStringFromList(idx.Value.Properties);
                    var id = doc.CreateAttribute("name");
                    id.Value = idx.Value.IndexName;                    
                    XmlNode idxNodeCurrent = doc.CreateElement("index");
                    idxNodeCurrent.Attributes.Append(id);
                    idxNodeCurrent.Attributes.Append(i);
                    idxNode.AppendChild(idxNodeCurrent);
                }
                nodeEntity.AppendChild(idxNode);
                node.AppendChild(nodeEntity);
            }
            root.AppendChild(node);
            doc.AppendChild(root);
            doc.Save(filePath);
        }        
        internal static void getEntities(DbContext context)
        {
            contextEntities.Clear();
            // this code is to avoid unnessesary renaming existing entities/fields 
            var currentInfoPath = Path.Combine(Directory.GetCurrentDirectory(),context.GetType().Name+".info.xml");
            if (File.Exists(currentInfoPath))
            {
                ReadExistingNames(currentInfoPath);             
            }
            //var res = new Dictionary<string,ModelDataNames>();
            var allDBProps = context.GetType().GetProperties();
            Type tDBSet = typeof(DbSet<>);
            Type tFluidNameAttr = typeof(FluidNameAttribute);
            Type tFluidPropAttr = typeof(FluidPropertyNameAttribute);
            Type tNoFluidNameAttr = typeof(NoFluidNameAttribute);
            Type tKeyPartAttr = typeof(KeyPartAttribute);
            Type tNoBaseTypeAttr = typeof(NoBaseTypeAttribute);
            Type tIndexAttr = typeof(IndexAttribute);
            Type tNoValueConverterAttr = typeof(NoValueConverterAttribute);
            Type tValueConverterMethodAttr = typeof(ValueConverterMethodAttribute);
            Type tIsRequiredAsReferenceAttr = typeof(IsRequiredAsReferenceAttribute);
            Type tDefaultSQLValueForReferenceAttr = typeof(DefaultSQLValueForReferenceAttribute);
            Type tUseXminAsConcurrencyTokenAttr = typeof(UseXminAsConcurrencyTokenAttribute);
            // enumerate all DBSet<> (entity)
            int k = 1;
            int idxNumber=1;
            foreach( var prop in allDBProps.Where(p => p.PropertyType.IsGenericType))
            {
                var genericDBSetType = tDBSet.MakeGenericType(prop.PropertyType.GenericTypeArguments);
                if (prop.PropertyType.IsEquivalentTo(genericDBSetType))
                {
                    Type entityType = prop.PropertyType.GenericTypeArguments[0];
                    var entityDescribtor = new ModelDataNames();
                    entityDescribtor.EntityType = entityType;
                    entityDescribtor.TypeFullName = entityType.FullName;
                    if (entityType.GetCustomAttribute(tUseXminAsConcurrencyTokenAttr) != null)
                    {
                        entityDescribtor.UseXminAsConcurrencyToken = true;
                    }
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
                    KeyValuePair<string,ModelDataNames> nameExist;
                    if (FluidName != null)
                    {
                        fname = (FluidName as FluidNameAttribute).StartsWith;
                        
                        if (!String.IsNullOrWhiteSpace(fname))
                        {
                            nameExist = existingTableNames.FirstOrDefault(v => v.Value.TypeFullName == entityType.FullName);
                            if (nameExist.Key != null)
                            {
                                entityDescribtor.RenameTable = true;
                                entityDescribtor.EntityTableName = nameExist.Value.EntityTableName;
                                k++;
                            }
                            else 
                            {
                                while (true)
                                {
                                    string currentName = fname.ToUpper()+ k.ToString().Replace(" ","");
                                    var nameExistCurrent = existingTableNames.FirstOrDefault(v => v.Value.EntityTableName == currentName);
                                    if (nameExistCurrent.Key == null)
                                    {
                                        entityDescribtor.RenameTable = true;
                                        entityDescribtor.EntityTableName = currentName;
                                        // add this to existingTableNames
                                        break;
                                    }
                                    k++;
                                }
                            }
                        }
                    }
                    fnameProp = (FluidNameProp == null) ? "" : (FluidNameProp as FluidPropertyNameAttribute).StartsWith;
                    Dictionary<string,ModelFields> props = new Dictionary<string, ModelFields>();
                    int i = 1;
                    nameExist = existingTableNames.FirstOrDefault(v => v.Value.TypeFullName == entityType.FullName);
                    foreach(var pInfo in entityType.GetProperties())
                    {
                        ModelFields fldDescribtion = new ModelFields();
                        fldDescribtion.Type = pInfo.PropertyType;
                        // is property type entity?
                        bool typeIsEntity = context.GetType().GetProperties()
                            .Where(v => v.PropertyType.GenericTypeArguments.Contains(pInfo.PropertyType)).FirstOrDefault() != null;
                        if (typeIsEntity)
                        {
                            bool IsRequired = pInfo.PropertyType.GetCustomAttribute(tIsRequiredAsReferenceAttr) != null;
                            fldDescribtion.IsRequired = IsRequired;
                            DefaultSQLValueForReferenceAttribute a = pInfo.PropertyType.GetCustomAttribute(tDefaultSQLValueForReferenceAttr) as DefaultSQLValueForReferenceAttribute;
                            if (a != null) 
                            {
                                fldDescribtion.DefaultSQLValueForReference = a.SQLExpression;
                            }
                        }
                        var NoFluidName = pInfo.GetCustomAttribute(tNoFluidNameAttr);
                        if (NoFluidName == null && !String.IsNullOrWhiteSpace(fnameProp))
                        {
                            var pCustomPropName = pInfo.GetCustomAttribute(tFluidPropAttr);
                            // find assigned property field
                            bool propIsAssigned = false;                                                                          
                            if (nameExist.Key != null)
                            {
                                propIsAssigned = nameExist.Value.TableFields.ContainsKey(pInfo.Name);
                            }
                            if (propIsAssigned && !String.IsNullOrWhiteSpace(nameExist.Value.TableFields[pInfo.Name].Name))
                            {
                                fldDescribtion.RenameField = true;
                                fldDescribtion.Name = nameExist.Value.TableFields[pInfo.Name].Name;
                            } 
                            else 
                            {
                                string fnamePropCustom = fnameProp;
                                if (pCustomPropName != null) {
                                   fnamePropCustom = (pCustomPropName as FluidPropertyNameAttribute).StartsWith; 
                                }
                                if (nameExist.Key != null && nameExist.Value != null && nameExist.Value.TableFields != null)
                                {
                                    while (true)
                                    {
                                        fnamePropCustom = fnamePropCustom.ToUpper() + i.ToString().Replace(" ", "");
                                        if (nameExist.Value.TableFields.FirstOrDefault(v => (v.Value.Name == fnamePropCustom)).Key == null)
                                        {
                                            // name not exists
                                            fldDescribtion.RenameField = true;
                                            fldDescribtion.Name = fnamePropCustom;
                                            break;
                                        }
                                        i++;
                                    }
                                } else {
                                    fnamePropCustom = fnamePropCustom.ToUpper() + i.ToString().Replace(" ", "");
                                    fldDescribtion.RenameField = true;
                                    fldDescribtion.Name = fnamePropCustom;
                                    i++;
                                }                        
                            }                                                                                         
                        }
                        
                        if (pInfo.GetCustomAttribute(tNoValueConverterAttr) != null) 
                        {
                            fldDescribtion.NoValueConverter = true;
                        } 
                        else if (pInfo.GetCustomAttribute(tValueConverterMethodAttr) != null)
                        {
                            var method = pInfo.GetCustomAttribute(tValueConverterMethodAttr) as ValueConverterMethodAttribute;
                            var DbContextPropName = context.GetType().GetProperties()
                            .Where(v => v.PropertyType.GenericTypeArguments.Contains(pInfo.PropertyType)).FirstOrDefault();
                            if (DbContextPropName != null)
                            {
                                var vConverter = GetConverter(pInfo.PropertyType, context, DbContextPropName.Name, method.Method);
                                fldDescribtion.ValueConverterProperty = vConverter;
                            }
                        }
                        props.Add(pInfo.Name, fldDescribtion);
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
                                mIdx.IndexName = (IndexProp as IndexAttribute).StartsWith + "_" + idxNumber.ToString();
                                idxNumber++;
                                mIdx.IsUnique = (IndexProp as IndexAttribute).IsUnique;
                                mIdx.Properties.Add(currentProp.Name);
                                entityDescribtor.Indexes.Add((IndexProp as IndexAttribute).StartsWith, mIdx);
                            }
                            entityDescribtor.EntityKeys.Add(currentProp.Name, entityType.Name);
                        }                        
                    }
                    contextEntities.Add(prop.Name,entityDescribtor);
                    if (!existingTableNames.ContainsKey(prop.Name))
                    {
                        existingTableNames.Add(prop.Name, entityDescribtor);

                    }
                }
            }
            SaveExistingNames(currentInfoPath);        
        }
        /// <summary>
        /// Create fluid names for each entity and field.
        /// </summary>
        /// <param name="modelBuilder">The <see cref="ModelBuilder"/> to enable fluid names feature.</param>
        /// <param name="context">The <see cref="DbContext"/> Instance of you DBContext to be configured.</param>
        /// <returns>The <see cref="ModelBuilder"/> had enabled fluid names feature.</returns>
        public static ModelBuilder CreateFluidNames(this ModelBuilder modelBuilder, DbContext context)
        {
            if (!contextEntitiesExists)
            {
                object locker = new object();
                lock (locker)
                {
                    getEntities(context);
                    contextEntitiesExists = true;
                }
            }
            MethodInfo entityBuilderMethod = modelBuilder.GetType().GetMethods()
            .Where(n => n.Name == "Entity" && n.IsGenericMethod)
            .Where(n => n.GetParameters().Count() == 0).FirstOrDefault();
            if (entityBuilderMethod == null)
            {
                throw(new Exception("Not found ModelBuilder<Entity>() method"));
            }
            foreach( var entity in contextEntities)
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
                // this is only npgsql feature!!!
                if (entity.Value.UseXminAsConcurrencyToken)
                {
                    (eB as EntityTypeBuilder).UseXminAsConcurrencyToken();
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
                    bool isReq = pName.Value.IsRequired == null ? false : true;
                    if (isReq)
                    {
                        (eB as EntityTypeBuilder).Property(pName.Key).IsRequired(true);
                    }
                    if (!String.IsNullOrWhiteSpace(pName.Value.DefaultSQLValueForReference))
                    {
                        (eB as EntityTypeBuilder).Property(pName.Key).HasDefaultValueSql(pName.Value.DefaultSQLValueForReference);
                    }
                    // set ValueConverter if it present!
                    if (pName.Value.ValueConverterProperty != null)
                    {
                        (eB as EntityTypeBuilder).Property(pName.Key).HasConversion(pName.Value.ValueConverterProperty);
                    }
                    else
                    {
                        var VConverter = contextEntities.Values.FirstOrDefault(v => v.EntityType == pName.Value.Type);
                        if (VConverter != null && !pName.Value.NoValueConverter)
                        {
                            
                            (eB as EntityTypeBuilder).Property(pName.Key).HasConversion(VConverter.ValueConverter);
                        }
                    }
                }
                // Indexes
                foreach(var idx in entity.Value.Indexes)
                {
                    (eB as EntityTypeBuilder).HasIndex(idx.Value.Properties.ToArray()).HasName(idx.Value.IndexName).IsUnique(idx.Value.IsUnique);
                }                
            }
            return modelBuilder;
        }
    }
}
