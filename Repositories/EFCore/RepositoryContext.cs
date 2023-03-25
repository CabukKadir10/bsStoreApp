using Entities.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Repositories.EFCore.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.EFCore // veri tabanı oluşturmak ve yönetmek için kullnaılır. IdentityDbContext ile kimlik doğrulaması işlevi kazandırır api'ye
{//
    public class RepositoryContext : IdentityDbContext<User>
    {
        public RepositoryContext(DbContextOptions options) :
            base(options)
        {

        }
        public DbSet<Book> Books { get; set; } // Book tablosuna erişmek için set etmemiz lazim
        public DbSet<Category> Categories { get; set; }//Categories tablosuna erişmek için set yapmamiz lazım

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);// veri tabanı modellemesi için kullanılır. bu yüzden bookConfig örnek alır
           // modelBuilder.ApplyConfiguration(new BookConfig());
           // modelBuilder.ApplyConfiguration(new RoleConfiguration());
           modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}