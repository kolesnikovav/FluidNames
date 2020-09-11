using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace test
{

    public class AnimalContext : DbContext
    {
        public DbSet<Cat> Cats { get; set; }
        public DbSet<Dog> Dogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("test");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.CreateFluidNames(this);
        }
    }

    public class Animal
    {
    }

    public class Cat: Animal
    {
    }

    public class Dog: Animal
    {
    }
}
