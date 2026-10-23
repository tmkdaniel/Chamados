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

        public DbSet<AnexoAndamento> AnexosAndamento => Set<AnexoAndamento>();

        public DbSet<Orcamento> Orcamentos => Set<Orcamento>();

        public DbSet<AnexoOrcamento> AnexosOrcamento => Set<AnexoOrcamento>();

        public DbSet<VersaoOrcamento> VersoesOrcamento => Set<VersaoOrcamento>();

        public DbSet<HistoricoOrcamento> HistoricoOrcamento => Set<HistoricoOrcamento>();

        public DbSet<PerguntaOrcamento> PerguntasOrcamento => Set<PerguntaOrcamento>();

        public DbSet<RespostaPergunta> RespostasPergunta => Set<RespostaPergunta>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Nome)
                .IsUnique();

            modelBuilder.Entity<Empresa>()
                .HasIndex(e => e.Nome)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .Property(u => u.Email)
                .HasMaxLength(255);

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Empresa)
                .WithMany()
                .HasForeignKey(u => u.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .Property(t => t.MessageId)
                .HasMaxLength(998);

            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.MessageId);

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

            modelBuilder.Entity<AnexoAndamento>()
                .HasIndex(a => a.AndamentoId)
                .IsUnique();

            modelBuilder.Entity<AnexoAndamento>()
                .HasOne<Andamento>()
                .WithMany()
                .HasForeignKey(a => a.AndamentoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Orcamento>()
                .Property(o => o.Titulo)
                .HasMaxLength(200);

            modelBuilder.Entity<Orcamento>()
                .HasIndex(o => new { o.EmpresaId, o.Status });

            modelBuilder.Entity<Orcamento>()
                .HasOne<Empresa>()
                .WithMany()
                .HasForeignKey(o => o.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Orcamento>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(o => o.CriadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Orcamento>()
                .HasIndex(o => o.TicketId);

            modelBuilder.Entity<Orcamento>()
                .HasOne<Ticket>()
                .WithMany()
                .HasForeignKey(o => o.TicketId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AnexoOrcamento>()
                .HasIndex(a => a.OrcamentoId);

            modelBuilder.Entity<AnexoOrcamento>()
                .HasOne<Orcamento>()
                .WithMany()
                .HasForeignKey(a => a.OrcamentoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Orcamento>()
                .Property(o => o.ValorAtual)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Orcamento>()
                .Property(o => o.ValidadeAtual)
                .HasColumnType("date");

            modelBuilder.Entity<VersaoOrcamento>()
                .Property(v => v.Valor)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<VersaoOrcamento>()
                .Property(v => v.Validade)
                .HasColumnType("date");

            modelBuilder.Entity<VersaoOrcamento>()
                .HasIndex(v => new { v.OrcamentoId, v.Numero })
                .IsUnique();

            modelBuilder.Entity<VersaoOrcamento>()
                .HasOne<Orcamento>()
                .WithMany()
                .HasForeignKey(v => v.OrcamentoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VersaoOrcamento>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(v => v.CriadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoricoOrcamento>()
                .HasIndex(h => h.OrcamentoId);

            modelBuilder.Entity<HistoricoOrcamento>()
                .HasOne<Orcamento>()
                .WithMany()
                .HasForeignKey(h => h.OrcamentoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HistoricoOrcamento>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(h => h.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PerguntaOrcamento>()
                .Property(p => p.Texto)
                .IsRequired()
                .HasMaxLength(PerguntaOrcamento.TamanhoMaximoTexto);

            modelBuilder.Entity<PerguntaOrcamento>()
                .HasIndex(p => p.OrcamentoId);

            modelBuilder.Entity<PerguntaOrcamento>()
                .HasOne<Orcamento>()
                .WithMany()
                .HasForeignKey(p => p.OrcamentoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PerguntaOrcamento>()
                .HasOne(p => p.Autor)
                .WithMany()
                .HasForeignKey(p => p.AutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RespostaPergunta>()
                .Property(r => r.Texto)
                .IsRequired()
                .HasMaxLength(PerguntaOrcamento.TamanhoMaximoTexto);

            modelBuilder.Entity<RespostaPergunta>()
                .HasIndex(r => r.PerguntaId);

            modelBuilder.Entity<RespostaPergunta>()
                .HasOne<PerguntaOrcamento>()
                .WithMany(p => p.Respostas)
                .HasForeignKey(r => r.PerguntaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RespostaPergunta>()
                .HasOne(r => r.Autor)
                .WithMany()
                .HasForeignKey(r => r.AutorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
