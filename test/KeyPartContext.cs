using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace test
{

    public class KeyPartContext : DbContext
    {
        public DbSet<CatWithKeyPart> Cats { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("test");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.CreateFluidNames(this);
        }
    }
    public class CatWithKeyPart
    {
        [KeyPart]
        public string Name {get;set;}
        [KeyPart]
        public int Age {get;set;}
    }
}