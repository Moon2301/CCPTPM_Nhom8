using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UngDungOnThiBangLai.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Credit { get; set; } = 0;
    }
    public class LicenseCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } // A1, A2, B1, B2...
        public string? Description { get; set; }
        public int TotalQuestions { get; set; } // Tổng số câu hỏi trong danh mục này, có thể dùng để kiểm tra khi tạo đề thi
        public int TimeLimit { get; set; } // Thời gian làm bài tối đa cho danh mục này, tính bằng phút hoặc giây
        public int MinimumPassScore { get; set; } // Điểm tối thiểu để đậu, có thể dùng để kiểm tra khi chấm điểm đề thi
        public int TotalCriticalQuestions { get; set; }

        public virtual ICollection<Question>? Questions { get; set; }
        public virtual ICollection<QuestionTopic>? QuestionTopics { get; set; } = new List<QuestionTopic>();
    }

    public class Question
    {
        public int Id { get; set; }

        [Required]
        public string QuestionText { get; set; }

        public string? ImageUrl { get; set; }
        public int? TrafficSignId { get; set; }
        public virtual TrafficSign? TrafficSign { get; set; }
        public string? Explanation { get; set; }
        public bool IsCritical { get; set; }
        public string QuestionType { get; set; }
        public virtual ICollection<QuestionTopicQuestion> QuestionTopics { get; set; } = new List<QuestionTopicQuestion>();

        public virtual ICollection<Answer> Answers { get; set; }
    }
    public class QuestionTopic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }
        public int NumberOfQuestionsInExam { get; set; }

        public int LicenseCategoryId { get; set; }
        [ForeignKey("LicenseCategoryId")]
        public virtual LicenseCategory? LicenseCategory { get; set; }

        public virtual ICollection<QuestionTopicQuestion> Questions { get; set; } = new List<QuestionTopicQuestion>();
    }

    public class QuestionTopicQuestion
    {
        public int QuestionId { get; set; }
        public virtual Question Question { get; set; }

        public int QuestionTopicId { get; set; }
        public virtual QuestionTopic QuestionTopic { get; set; }
    }

    public class Answer
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }

        // Đáp án có thể là chữ hoặc để trống nếu chỉ dùng ảnh biển báo
        public string? AnswerText { get; set; }

        // Ảnh tự do cho đáp án
        public string? ImageUrl { get; set; }

        // LIÊN KẾT BIỂN BÁO: Nếu đáp án là một hình biển báo trong thư viện
        public int? TrafficSignId { get; set; }
        public virtual TrafficSign? TrafficSign { get; set; }

        public bool IsCorrect { get; set; }

        public virtual Question Question { get; set; }
    }

    public class TrafficSign
    {
        public int Id { get; set; }
        public string Code { get; set; }         // Ví dụ: "P.102"
        public string Name { get; set; }         // Ví dụ: "Cấm đi ngược chiều"
        public string ImageUrl { get; set; }     // Đường dẫn ảnh biển báo gốc
        public string Category { get; set; }     // "Cấm", "Nguy hiểm", "Hiệu lệnh"...
        public string Description { get; set; }  // Ý nghĩa chi tiết biển báo

        // Thuộc tính này giúp tra cứu xem biển báo này xuất hiện ở những câu hỏi nào
        public virtual ICollection<Question> Questions { get; set; }
        public virtual ICollection<Answer> Answers { get; set; }
    }

    public class Exam
    {
        public int Id { get; set; }
        public string Name { get; set; } // Ví dụ: "Đề số 1", "Đề thi thử A1 - Bộ 1"
        public int LicenseCategoryId { get; set; }
        public int TotalQuestions { get; set; }
        public int TimeLimit { get; set; } // Tính bằng giây hoặc phút
        public int PassingScore { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual LicenseCategory LicenseCategory { get; set; }
        public virtual ICollection<ExamQuestion> ExamQuestions { get; set; }
    }

    public class ExamQuestion
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public int QuestionId { get; set; }
        public int Order { get; set; } // Thứ tự xuất hiện trong đề

        public virtual Exam Exam { get; set; }
        public virtual Question Question { get; set; }
    }

    public class ExamResult
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ExamId { get; set; }
        public int Score { get; set; }
        public bool IsPassed { get; set; }
        public DateTime TakenAt { get; set; }
        public string RawData { get; set; } // Lưu JSON các câu đã chọn để xem lại
    }
}
