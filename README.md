# Why
Some database providers doesn't allow long table, fields, keys names. If you have > 20 several entity types, manualy creating table/ field names may be tediously.
This library allow you set start symblos for tables and fields and create unique fluid names for each entity.
Anover feature is creating ``` ValueConverter ``` for each entities. This is used to create relation between Entity and CLR Type. For creation of ``` ValueConverter ``` Key property of Entity is used. With this feature, you can refference the property of entity to anover entity type directly.

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




