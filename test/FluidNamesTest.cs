using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace test
{
    public class FluidNamesTest
    {
        [Fact]
        public void EnsureNoBaseType_Test()
        {
            using (var db = new NoBaseTypeContext())
            {
                var tableCat = db.Model.FindRuntimeEntityType(typeof(CatTest1)).GetTableName();
                var tableDog = db.Model.FindRuntimeEntityType(typeof(DogTest1)).GetTableName();
                Assert.NotEqual(tableCat, tableDog);
            }
        }
        [Fact]
        public void EnsureKeyPart_Test()
        {
            using (var db = new KeyPartContext())
            {
                var CatWithKeyPartKeys = db.Model.FindRuntimeEntityType(typeof(CatWithKeyPart)).GetKeys();
                Assert.Single(CatWithKeyPartKeys);
            }
        }
        [Fact]
        public void EnsureIndex_Test()
        {
            using (var db1 = new ContextWithIndex())
            {
                var indexName = db1.Model.FindRuntimeEntityType(typeof(CatWithIndex)).GetDeclaredIndexes().Where(v => v.GetName().ToUpperInvariant().Contains("NAME")).FirstOrDefault();
                var indexAge = db1.Model.FindRuntimeEntityType(typeof(CatWithIndex)).GetDeclaredIndexes().Where(v => v.GetName().ToUpperInvariant().Contains("AGE")).FirstOrDefault();
                Assert.False(indexAge.IsUnique);
                Assert.True(indexName.IsUnique);
            }
        }

        [Fact]
        public void EnsureVariableType_Test()
        {
            using (var db = new VariableTypeContext())
            {
                db.Cats.Add(new CatVariable
                {
                    Age = new VariableType(typeof(string), "less 2 years")
                });
                db.Cats.Add(new CatVariable
                {
                    Age = new VariableType(typeof(bool), false)
                });
                db.SaveChanges();
                var q = db.Cats.ToList();
            }
        }
        [Fact]
        public void EnsureFluidNameWorks_Test()
        {
            using (var db = new FluidNameContext())
            {
                var cat2tablename = db.Model.FindRuntimeEntityType(typeof(CatTest2)).GetTableName();
                var cat2id = db.Model.FindRuntimeEntityType(typeof(CatTest2)).FindProperty("Id").GetColumnName();
                var cat2age = db.Model.FindRuntimeEntityType(typeof(CatTest2)).FindProperty("Age").GetColumnName();
                Assert.StartsWith("Ref".ToUpperInvariant(), cat2tablename);
                Assert.Equal("Id", cat2id);
                Assert.StartsWith("Fld".ToUpperInvariant(), cat2age);
            }
        }
    }
}
