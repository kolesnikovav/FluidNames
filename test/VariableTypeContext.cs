using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace test
{

    public class VariableTypeContext : DbContext
    {
        public DbSet<CatVariable> Cats { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("test");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.CreateFluidNames(this);
        }
    }
    public class CatVariable
    {
        [Key]
        public int Id {get;set;}

        [CanBe(new Type[] {typeof(bool), typeof(string)})]
        public object Age {get;set;}
    }
}