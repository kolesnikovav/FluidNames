using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        // [Fact]
        // public void EnsureIndex_Test()
        // {
        //     using (var db1 = new ContextWithIndex())
        //     {
        //         var indexes = db1.Model.FindRuntimeEntityType(typeof(CatWithIndex)).GetDeclaredIndexes().OrderBy(v => v.GetName()).ToArray();
        //         Assert.False(indexes[0].IsUnique);
        //         Assert.True(indexes[1].IsUnique);
        //     }
        // }

        // [Fact]
        // public void EnsureVariableType_Test()
        // {
        //     using (var db = new VariableTypeContext())
        //     {
        //         // var indexes = db.Model.FindRuntimeEntityType(typeof(CatWithIndex)).GetDeclaredIndexes().OrderBy(v => v.GetName()).ToArray();
        //         // Assert.False(indexes[0].IsUnique);
        //         // Assert.True(indexes[1].IsUnique);
        //     }
        // }
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
