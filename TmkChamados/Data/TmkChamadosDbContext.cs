using Microsoft.EntityFrameworkCore;
using TmkChamados.Models;

namespace TmkChamados.Data
{
    public class TmkChamadosDbContext : DbContext
    {
        public TmkChamadosDbContext(DbContextOptions<TmkChamadosDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();

        public DbSet<Chamado> Chamados => Set<Chamado>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Nome)
                .IsUnique();

            modelBuilder.Entity<Chamado>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(c => c.CriadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chamado>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(c => c.ResponsavelId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
