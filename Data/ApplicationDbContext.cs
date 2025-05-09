using Microsoft.EntityFrameworkCore;
using ProjectEvaluationSystem.Models;

namespace ProjectEvaluationSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Contributor> Contributors { get; set; }
        public DbSet<Evaluation> Evaluations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ProjectImage> ProjectImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Admin kullanıcısını oluşturma
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "admin123", // Gerçek uygulamada hash'lenmiş olmalı
                    IsAdmin = true
                }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
} 