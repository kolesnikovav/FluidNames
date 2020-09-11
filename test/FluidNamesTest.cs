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
        public void ValueConvertionVariableType_Test()
        {
            var Frank = new Boy() { Name = "Frank" };
            var Paul = new Boy() { Name = "Paul" };
            using (var db = new VariableTypeContext())
            {
                db.Boys.Add(Frank);
                db.Boys.Add(Paul);
                db.SaveChanges();
                var c = db.Model.FindEntityType(typeof(CatVariable)).GetProperties().Where(v => v.Name == "CatOwner").First().GetValueConverter();
                var a = c.ConvertToProvider(new VariableType(Frank));
                var b = c.ConvertToProvider(new VariableType(Paul));

                var x = (c.ConvertFromProvider(a) as VariableType).Value;
                var y = (c.ConvertFromProvider(a) as VariableType).Value;

                Assert.Equal(Frank.Name, (x as Boy).Name);
                Assert.Equal(Paul.Name, (y as Boy).Name);
            }
        }

        [Fact]
        public void EnsureVariableType_Test()
        {
            using (var db = new VariableTypeContext())
            {
                var Frank = new Boy() { Name = "Frank" };
                var Paul = new Boy() { Name = "Paul" };
                var Sarah = new Girl() { Name = "Sarah" };
                var Mary = new Girl() { Name = "Mary" };
                db.Boys.Add(Frank);
                db.Boys.Add(Paul);
                db.Girls.Add(Sarah);
                db.Girls.Add(Mary);
                db.Cats.Add(new CatVariable
                {
                    Nick = "Baby",
                    Age = new VariableType("less 2 years"),
                    CatOwner = new VariableType(Frank)
                });
                db.Cats.Add(new CatVariable
                {
                    Nick = "Caty",
                    Age = new VariableType( false),
                    CatOwner = new VariableType(Mary)
                });
                db.Cats.Add(new CatVariable
                {
                    Nick = "Kitty",
                    Age = new VariableType( new AgeVariant() { Describtion = "1/2 years"}),
                    CatOwner = new VariableType("unknown")
                });
                db.SaveChanges();
                var q = db.Cats.Where(v => v.Nick == "Kitty").FirstOrDefault();
                Assert.Equal("unknown", q.CatOwner.Value);
                var q1 = db.Cats.Where(v => v.Nick == "Caty").FirstOrDefault();
                Assert.Equal(Mary, q1.CatOwner.Value);
                var q2 = db.Cats.Where(v => v.Nick == "Baby").FirstOrDefault();
                Assert.Equal(Frank, q2.CatOwner.Value);
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
