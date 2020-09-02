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
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Microsoft.EntityFrameworkCore
{

    internal static class XMLUtils
    {
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
        /// <summary>
        /// Read context information to avoid unnessesary rename tables and fields
        /// </summary>              
        internal static void ReadExistingNames(string filePath, Dictionary<string, ModelDataNames> existingTableNames)
        {
            existingTableNames.Clear();
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            XmlNode node = doc.DocumentElement.SelectSingleNode("/fluidnames/entities");
            foreach (XmlNode cNode in node.ChildNodes)
            {
                var entityName = cNode.Attributes["name"].Value;
                var entityType = cNode.Attributes["type"].Value;
                var tablename = cNode.Attributes["tablename"].Value;
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
                                mdn.TableFields.Add(fieldName, new ModelFields { Name = assignedName });
                            }
                        }
                    }
                    else if (nChld.Name == "indexes")
                    {
                        foreach (XmlNode nidx in nChld.ChildNodes)
                        {
                            var idxName = nidx.Attributes["name"].Value;
                            var idxfields = nidx.Attributes["fields"].Value;
                            if (!String.IsNullOrWhiteSpace(idxName))
                            {
                                mdn.Indexes.Add(idxName, new ModelIndex { Fields = idxfields });
                            }
                        }
                    }
                }
                existingTableNames.Add(entityName, mdn);
            }
        }

        /// <summary>
        /// Save context information to avoid unnessesary rename tables and fields
        /// </summary>
        internal static void SaveExistingNames(string filePath, Dictionary<string, ModelDataNames> contextEntities)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode root = doc.CreateElement("fluidnames");
            XmlNode node = doc.CreateElement("entities");
            foreach (var entity in contextEntities)
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
    }
}