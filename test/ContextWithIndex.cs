using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace test
{

    public class ContextWithIndex : DbContext
    {
        public DbSet<CatWithIndex> Cats { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("test");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.CreateFluidNames(this);
        }
    }
    public class CatWithIndex
    {
        [Key]
        public int Id {get;set;}
        [Index("name", true)]
        public string Name {get;set;}
        [Index("age", false)]
        public int Age {get;set;}
    }
}