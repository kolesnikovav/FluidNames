<a href="https://www.nuget.org/packages/FluidNames">
    <img alt="Nuget (with prereleases)" src="https://img.shields.io/nuget/vpre/FluidNames">
</a>
<a href="https://www.nuget.org/packages/FluidNames">
    <img alt="Nuget" src="https://img.shields.io/nuget/dt/FluidNames">
</a>

# About
FluidNames is the Entity Framework Core plugin for create some enhancements in your EF Core database context.
## Key features:
- Create short names for entity tables, fields, indexes.
- Create ``` ValueConverter ``` expressions for references.
- Adds the ability to store several data types in one field.
- Adds some attributes for use instead of using Fluent API.

### ValueConverter:

```csharp
    public class AnimalOwner
    {
        [Key]
        public Guid Id {get;set;}
        public string Name {get;set;}
    }
    public class Dog
    {
        //... you code ...//
        public AnimalOwner Owner {get;set;} // This is valid Type, because of ValueConverter is present!
    }
```
You can disable autocreate ``` ValueConverter ``` for entity/property by setting ```[NoValueConverter]``` attribute.

### Variable property type:
If entity property can be several types, you can describe this property as object type.
This plugin understands it, and create Value converter for this case. In database, this property will store as JSON string.
If this value is reference, only key of referenced entity will be store.

```csharp
    public class AnimalOwner
    {
        [Key]
        public Guid Id {get;set;}
        public string Name {get;set;}
    }
    public class Dog
    {
        //... you code ...//
        public object Owner {get;set;} // You can store any data in this property
    }
```
You can disable autocreate ``` ValueConverter ``` for entity/property by setting ```[NoValueConverter]``` attribute.



# How to use

`FluidNames` will autonumerate you entities  and automaticly setup table names and field.

1. Install FluidNames Package

Run the following command in the `Package Manager Console` to install FluidNames

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
        modelBuilder.CreateFluidNames( this);
    }
}
```
# Additional attributes
```csharp
    [KeyPart] // mark the property as a part of key
```
This is equivalent Fluent API method, but it can be done at top hierarchy level
```csharp
    modelBuilder.Entity<>().HasKey(v => new { v.ID, v.CODE})
```

```csharp
    [NoBaseType] // equivalent Fluent API HasBaseType((Type)null) method
```
This is equivalent Fluent API method, but it can be done at top hierarchy level
```csharp
    modelBuilder.Entity<>().HasBaseType((Type)null)
```

```csharp
    [Index("IDX_Field1", true)] // designate that property is the part of index
```
This is equivalent Fluent API method, but it can be done at top hierarchy level
```csharp
    modelBuilder.Entity<>().HasIndex(...).IsUnique()
```

```csharp
    [IsRequiredAsReference] // you can set Entity, that referenced this Entity column does not allow null
```
This is equivalent Fluent API method, but you can mark all reference for this Entity as required
```csharp
    modelBuilder.Entity<>().Property(<referencing property>).IsReqiured()
```

```csharp
    [DefaultSQLValueForReference("sql expression")] // you can set default value for reference this Entity
```
For property, equivalent is
```csharp
    [DefaultSQLValue("sql expression")] // you can set default sql value for property
```
This is equivalent Fluent API method, but you can mark all reference for this Entity as required
```csharp
    modelBuilder.Entity<>().Property(<referencing property>).HasDefaultValueSql("sql expression")
```



# Postgresql specific attribute

You can set xmin system column as concurency token. For details, please refer to [Npgsql documentation](https://www.npgsql.org/efcore/modeling/concurrency.html?q=UseXminAsConcurrencyToken)
```csharp
    [UseXminAsConcurrencyToken] // set xmin system column as concurency token
```

```csharp
    modelBuilder.Entity<>().UseXminAsConcurrencyToken()
```




