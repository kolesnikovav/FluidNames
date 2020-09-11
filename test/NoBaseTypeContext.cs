using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace test
{

    public class NoBaseTypeContext : DbContext
    {
        public DbSet<CatTest1> Cats { get; set; }
        public DbSet<DogTest1> Dogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("test");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.CreateFluidNames(this);
        }
    }
    [NoBaseType]
    public class AnimalTest1
    {
        [Key]
        public int Id {get;set;}

    }

    public class CatTest1: AnimalTest1
    {
        public string CatName {get;set;}
    }

    public class DogTest1: AnimalTest1
    {
        public string DogName {get;set;}
    }
}