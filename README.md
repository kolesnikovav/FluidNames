# Why
Some database providers doesn't allow long table, fields, keys names. If you have > 20 several entity types, manualy creating table/ field names may be tediously.
This library allow you set start symblos for tables and fields and create unique fluid names for each entity.

# How to use

`FluidNames` will autonumerate you entities  and automaticly setup table names and field.

1. Install FluidNames Package

Run the following command in the `Package Manager Console` to install Microsoft.EntityFrameworkCore.FluidNames

`PM> Install-Package FluidNames`

2. Adjust you entities

```csharp
    [FluidName("Animal")] // setup start table names
    [FluidPropertyName("field")] // setup start field names
    public class Animal
    {
        [Key]
        [NoFluidName] // mark field name as constant (no fluid)
        public Guid Id {get;set;}
        [NoFluidName]
        public string Code {get;set;}
        [NoFluidName]
        public string Name {get;set;}
    }
    public class Dog: Animal
    {
        public string Kind {get;set;}

        public int Age {get;set;}
    }
    public class Cat: Animal
    {
        public string Nick {get;set;}

        public int Age {get;set;}
    }
```
In this case, tables will be `Animal1`... `Animal(n)` and fields will be `Id`,`Code`,`Name`...,`field1`,...,`field(n)`

3. Create fluid names in OnModelCreating method

```csharp
public class MyDBContext : DbContext
{
    public MyDBContext(DbContextOptions<MyDBContext> options)
        : base(options)
    { }

    public DbSet<Dog> Dogs { get; set; }
    public DbSet<Cat> Cats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // create fluid names for you entities.
        modelBuilder.CreateFluidNames( modelBuilder, this);
    }
}
```

