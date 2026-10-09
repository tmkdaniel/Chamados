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

        public DbSet<Ticket> Tickets => Set<Ticket>();

        public DbSet<Empresa> Empresas => Set<Empresa>();

        public DbSet<Andamento> Andamentos => Set<Andamento>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Nome)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Empresa)
                .WithMany()
                .HasForeignKey(u => u.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(c => c.CriadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(c => c.ResponsavelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Andamento>()
                .HasOne<Ticket>()
                .WithMany()
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Andamento>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(a => a.CriadoPorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
