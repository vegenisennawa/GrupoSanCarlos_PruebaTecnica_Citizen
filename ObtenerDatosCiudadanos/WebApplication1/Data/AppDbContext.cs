using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Controllers;

namespace WebApplication1.Data
{
    /// <summary>
    /// Tabla usuarios
    /// </summary>
    [Table("Usuarios")]
    public class Usuario
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// El puente entre .NET y SQL Server
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Lead> Leads { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // LA MAGIA: Le enseñamos a EF Core a castear la fecha automáticamente
            modelBuilder.Entity<Lead>()
                .Property(l => l.Fecha_Nac)
                .HasConversion(
                    v => DateTime.ParseExact(v, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    v => v.ToString("dd/MM/yyyy")
                );
        }
    }
}