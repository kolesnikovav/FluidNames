using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace test
{
    public class FluidNameContext : DbContext
    {
        public DbSet<CatTest2> Cats { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("test");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.CreateFluidNames(this);
        }
    }
    [FluidName("Ref")]
    [FluidPropertyName("Fld")]
    [NoBaseType]
    public class AnimalTest
    {
        [Key]
        [NoFluidName]
        public int Id {get;set;}
    }    
    public class CatTest2: AnimalTest
    {
        public int Age {get;set;}
    }
}