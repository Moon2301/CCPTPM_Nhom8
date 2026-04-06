using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using BCrypt.Net;
using UngDungOnThiBangLai.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<LicenseCategory> LicenseCategories { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionTopicQuestion> QuestionTopicQuestions { get; set; } // Bảng trung gian cho mối quan hệ N-N giữa Question và QuestionTopic
    public DbSet<QuestionTopic> QuestionTopics { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<TrafficSign> TrafficSigns { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamQuestion> ExamQuestions { get; set; }
    public DbSet<ExamResult> ExamResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Cấu hình bảng LicenseCategory
        modelBuilder.Entity<LicenseCategory>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);

            // Quan hệ với QuestionTopic (1 - N)
            entity.HasMany(e => e.QuestionTopics)
                  .WithOne(t => t.LicenseCategory)
                  .HasForeignKey(t => t.LicenseCategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 2. Cấu hình bảng Question
        modelBuilder.Entity<Question>(entity => {
            entity.HasKey(e => e.Id);

            // ĐÃ XÓA: Quan hệ trực tiếp với LicenseCategory (Vì 600 câu là dùng chung)
            // ĐÃ XÓA: Quan hệ trực tiếp với QuestionTopic (Chuyển sang bảng trung gian)

            // Quan hệ với TrafficSign (N - 1) - Có thể null
            entity.HasOne(q => q.TrafficSign)
                  .WithMany(t => t.Questions)
                  .HasForeignKey(q => q.TrafficSignId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // TẠO MỚI: Cấu hình bảng trung gian QuestionTopicQuestion (N - N)
        modelBuilder.Entity<QuestionTopicQuestion>(entity => {
            // Khóa chính kép (Composite Key)
            entity.HasKey(qtq => new { qtq.QuestionId, qtq.QuestionTopicId });

            entity.HasOne(qtq => qtq.Question)
                  .WithMany(q => q.QuestionTopics) // Sửa List trong model Question thành List<QuestionTopicQuestion>
                  .HasForeignKey(qtq => qtq.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(qtq => qtq.QuestionTopic)
                  .WithMany(t => t.Questions) // Sửa List trong model QuestionTopic thành List<QuestionTopicQuestion>
                  .HasForeignKey(qtq => qtq.QuestionTopicId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. Cấu hình bảng Answer
        modelBuilder.Entity<Answer>(entity => {
            entity.HasKey(e => e.Id);

            // Quan hệ với Question (N - 1)
            entity.HasOne(a => a.Question)
                  .WithMany(q => q.Answers)
                  .HasForeignKey(a => a.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ với TrafficSign (N - 1) - Có thể null
            entity.HasOne(a => a.TrafficSign)
                  .WithMany(t => t.Answers)
                  .HasForeignKey(a => a.TrafficSignId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 4. Cấu hình bảng TrafficSign
        modelBuilder.Entity<TrafficSign>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        // 5. Cấu hình bảng User
        modelBuilder.Entity<User>(entity => {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // 6. Cấu hình bảng QuestionTopic
        modelBuilder.Entity<QuestionTopic>(entity => {
            entity.HasKey(e => e.Id);
        });

        // 7. Cấu hình bảng Exam
        modelBuilder.Entity<Exam>(entity => {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.LicenseCategory)
                  .WithMany()
                  .HasForeignKey(e => e.LicenseCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 8. Cấu hình bảng trung gian ExamQuestion
        modelBuilder.Entity<ExamQuestion>(entity => {
            entity.HasKey(eq => eq.Id);

            entity.HasOne(eq => eq.Exam)
                  .WithMany(e => e.ExamQuestions)
                  .HasForeignKey(eq => eq.ExamId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(eq => eq.Question)
                  .WithMany()
                  .HasForeignKey(eq => eq.QuestionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 9. Cấu hình bảng ExamResult
        modelBuilder.Entity<ExamResult>(entity => {
            entity.HasKey(er => er.Id);
            entity.Property(er => er.RawData).HasColumnType("nvarchar(max)");
        });

        // --- SEED DATA ---
        // Hạng bằng lái
        modelBuilder.Entity<LicenseCategory>().HasData(
            new LicenseCategory { Id = 1, Name = "A1", Description = "Xe máy dưới 175cc", TotalQuestions = 25, TimeLimit = 19, MinimumPassScore = 21, TotalCriticalQuestions = 1 },
            new LicenseCategory { Id = 2, Name = "B2", Description = "Ô tô con số sàn", TotalQuestions = 35, TimeLimit = 22, MinimumPassScore = 32, TotalCriticalQuestions = 1 }
        );

        // Tài khoản Admin mặc định
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Email = "admin@onthi.com",
                Role = "Admin",
                CreatedAt = new DateTime(2024, 1, 1),
                Credit = 0
            }
        );
    }

}