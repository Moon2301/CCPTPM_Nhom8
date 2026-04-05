using Microsoft.EntityFrameworkCore;
using UngDungOnThiBangLai.Models;

namespace UngDungOnThiBangLai.Services
{
    public interface IExamService
    {
        Task<Exam> GenerateRandomExam(int categoryId, string examName);
    }

    public class ExamService : IExamService
    {
        private readonly AppDbContext _context;

        public ExamService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Exam> GenerateRandomExam(int categoryId, string examName)
        {
            // 1. Lấy cấu hình hạng bằng kèm các chương của nó
            var category = await _context.LicenseCategories
                .Include(c => c.QuestionTopics)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null) throw new Exception("Không tìm thấy hạng bằng lái!");

            var finalQuestions = new List<Question>();

            // 2. BỐC CÂU HỎI THƯỜNG TỪ TỪNG CHƯƠNG
            if (category.QuestionTopics != null)
            {
                foreach (var topic in category.QuestionTopics)
                {
                    // Truy vấn qua bảng trung gian: Tìm các câu hỏi KHÔNG LIỆT và CÓ MAP VỚI TOPIC NÀY
                    var topicQuestions = await _context.Questions
                        .Where(q => !q.IsCritical && q.QuestionTopics.Any(qt => qt.QuestionTopicId == topic.Id))
                        .OrderBy(q => Guid.NewGuid()) // Random SQL (NEWID)
                        .Take(topic.NumberOfQuestionsInExam)
                        .ToListAsync();

                    finalQuestions.AddRange(topicQuestions);
                }
            }

            // 3. BỐC CÂU ĐIỂM LIỆT (Lấy ngẫu nhiên trên toàn bộ các câu của hạng bằng này)
            // Truy vấn qua bảng trung gian: Tìm các câu LÀ CÂU LIỆT và CÓ MAP VỚI BẤT KỲ TOPIC NÀO THUỘC HẠNG BẰNG NÀY
            var criticalQuestions = await _context.Questions
                .Where(q => q.IsCritical && q.QuestionTopics.Any(qt => qt.QuestionTopic.LicenseCategoryId == categoryId))
                .OrderBy(q => Guid.NewGuid())
                .Take(category.TotalCriticalQuestions)
                .ToListAsync();

            finalQuestions.AddRange(criticalQuestions);

            // Kiểm tra an toàn: Nếu Database chưa đủ câu hỏi thì báo lỗi tránh tạo đề hỏng
            if (finalQuestions.Count < category.TotalQuestions)
                throw new Exception($"Không đủ câu hỏi trong ngân hàng! Cần {category.TotalQuestions}, hiện có {finalQuestions.Count}");

            // 4. TẠO RECORD ĐỀ THI
            var exam = new Exam
            {
                Name = examName,
                LicenseCategoryId = categoryId,
                TotalQuestions = finalQuestions.Count, // Số lượng thực tế bốc được
                TimeLimit = category.TimeLimit,
                PassingScore = category.MinimumPassScore,
                CreatedAt = DateTime.UtcNow,
                ExamQuestions = new List<ExamQuestion>()
            };

            // 5. GẮN CÂU HỎI VÀO ĐỀ & XÁO TRỘN LẦN CUỐI
            // Xáo trộn toàn bộ (câu thường + câu liệt) để câu liệt không bị lộ là luôn nằm ở cuối đề
            int order = 1;
            foreach (var q in finalQuestions.OrderBy(x => Guid.NewGuid()))
            {
                exam.ExamQuestions.Add(new ExamQuestion
                {
                    QuestionId = q.Id,
                    Order = order++ // Đánh số thứ tự từ 1 đến N
                });
            }

            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();

            return exam;
        }
    }
}