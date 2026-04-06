using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using UngDungOnThiBangLai.Models;
using UngDungOnThiBangLai.Models.dtos;

namespace UngDungOnThiBangLai.Services
{
    public interface IDataSeedService
    {
        Task SeedAllAsync();
    }

    public class DataSeedService : IDataSeedService
    {
        private readonly AppDbContext _context;

        public DataSeedService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedAllAsync()
        {
            await SeedMasterPoolQuestionsAsync();
            await SeedCriticalQuestionsAsync();
            await SeedA1CategoryMappingAsync();
            await SeedB2CategoryMappingAsync();
        }

        public async Task SeedMasterPoolQuestionsAsync()
        {
            // Kiểm tra xem kho 600 câu đã được nạp chưa? Nếu có rồi thì bỏ qua.
            if (await _context.Questions.AnyAsync()) return;

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "questions.json");
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[CẢNH BÁO] Không tìm thấy file dữ liệu tại: {filePath}");
                return;
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var allQuestions = JsonSerializer.Deserialize<List<QuestionJsonDto>>(jsonString, options);

            if (allQuestions == null || !allQuestions.Any()) return;

            var newQuestions = new List<Question>();

            foreach (var dto in allQuestions)
            {
                var question = new Question
                {
                    // LƯU Ý: Không gán LicenseCategoryId hay QuestionTopicId vào đây nữa
                    QuestionText = dto.questionText,
                    Explanation = dto.explanation,
                    QuestionType = "MultipleChoice",
                    // Tự động đánh dấu câu điểm liệt nếu tiêu đề/giải thích có từ "liệt"
                    IsCritical = dto.questionText.Contains("liệt") || (dto.explanation?.Contains("liệt") ?? false),

                    Answers = dto.answers.Select(a => new Answer
                    {
                        // Regex xóa số thứ tự "1.", "2." ở đầu đáp án
                        AnswerText = Regex.Replace(a.text, @"^\d+\.?\s*", "").Trim(),
                        IsCorrect = a.isCorrect
                    }).ToList()
                };

                newQuestions.Add(question);
            }

            // Dùng AddRange để tăng tốc độ Insert (Bulk Insert) thay vì Add từng dòng
            await _context.Questions.AddRangeAsync(newQuestions);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[THÀNH CÔNG] Đã nạp thành công {newQuestions.Count} câu hỏi vào Master Pool.");
        }

        public async Task SeedCriticalQuestionsAsync()
        {
            // 1. Đọc file cấu hình điểm liệt
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "Critical.json");
            if (!File.Exists(filePath))
            {
                Console.WriteLine("[CẢNH BÁO] Không tìm thấy file Critical.json tại cấu trúc thư mục.");
                return;
            }

            string jsonString = await File.ReadAllTextAsync(filePath);

            // Deserialize trực tiếp mảng JSON thành List<int>
            var criticalIds = JsonSerializer.Deserialize<List<int>>(jsonString);

            if (criticalIds == null || !criticalIds.Any()) return;

            // 2. Truy vấn tối ưu (Optimized Query)
            // Dùng mệnh đề Contains để tạo câu lệnh SQL IN (...)
            // Đồng thời CHỈ lấy những câu hiện tại đang có IsCritical == false để tránh update thừa
            var questionsToUpdate = await _context.Questions
                .Where(q => criticalIds.Contains(q.Id) && q.IsCritical == false)
                .ToListAsync();

            // 3. Cập nhật dữ liệu
            if (questionsToUpdate.Any())
            {
                foreach (var q in questionsToUpdate)
                {
                    q.IsCritical = true;
                }

                // Lưu toàn bộ thay đổi trong 1 Transaction duy nhất
                await _context.SaveChangesAsync();
                Console.WriteLine($"[THÀNH CÔNG] Đã cập nhật thành công cờ Điểm Liệt cho {questionsToUpdate.Count} câu hỏi.");
            }
            else
            {
                Console.WriteLine("[THÔNG TIN] Toàn bộ các câu điểm liệt đã được cấu hình từ trước, không có thay đổi mới.");
            }
        }

        public async Task SeedA1CategoryMappingAsync()
        {
            // 1. Đọc file cấu hình
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "A1.json");
            if (!File.Exists(filePath))
            {
                Console.WriteLine("[CẢNH BÁO] Không tìm thấy file A1.json.");
                return;
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var mappingData = JsonSerializer.Deserialize<CategoryMappingDto>(jsonString, options);

            // Lưu ý: Đã đổi 'questions' thành 'question' theo đúng DTO mới
            if (mappingData == null || !mappingData.question.Any() || !mappingData.topic.Any()) return;

            // A1: bắt buộc đề thi đúng 25 câu (không hơn/không thiếu)
            const int A1_TOTAL_QUESTIONS = 25;
            int a1Critical = mappingData.criticalQuestion;
            int a1FormatSum = mappingData.formatExam != null ? mappingData.formatExam.Sum() : 0;
            if (a1Critical < 0) a1Critical = 0;

            if (a1FormatSum + a1Critical != A1_TOTAL_QUESTIONS)
            {
                throw new Exception($"Cấu hình A1.json không hợp lệ: formatExam({a1FormatSum}) + criticalQuestion({a1Critical}) phải = {A1_TOTAL_QUESTIONS}.");
            }

            // 2. Lấy hoặc Tạo Hạng bằng A1 (Sử dụng dữ liệu động từ JSON)
            var a1Category = await _context.LicenseCategories.FirstOrDefaultAsync(c => c.Name == "A1");
            if (a1Category == null)
            {
                // Tính tổng số câu hỏi trong đề dựa vào ma trận formatExam
                int totalQuestionsInExam = A1_TOTAL_QUESTIONS;

                a1Category = new LicenseCategory
                {
                    Name = "A1",
                    Description = "Xe mô tô hai bánh dung tích xilanh dưới 175 cm3",
                    TotalQuestions = totalQuestionsInExam,
                    TimeLimit = mappingData.timeExam > 0 ? mappingData.timeExam : 19, // Lấy từ JSON
                    MinimumPassScore = mappingData.minimumScore > 0 ? mappingData.minimumScore : 21, // Lấy từ JSON
                    TotalCriticalQuestions = mappingData.criticalQuestion
                };
                _context.LicenseCategories.Add(a1Category);
                await _context.SaveChangesAsync();
            }

            // 3. Chặn rác: Nếu đã map topic cho A1 rồi thì dừng lại (Idempotent)
            if (await _context.QuestionTopics.AnyAsync(t => t.LicenseCategoryId == a1Category.Id))
            {
                Console.WriteLine("[THÔNG TIN] Hạng A1 đã được map dữ liệu trước đó. Bỏ qua...");
                return;
            }

            // 4. Bắt đầu thuật toán Slicing để Map Topic
            var newMappings = new List<QuestionTopicQuestion>();
            int startIndex = 0;

            for (int i = 0; i < mappingData.topic.Count; i++)
            {
                int endIndex = mappingData.topic[i];
                int count = endIndex - startIndex;

                // Trích xuất an toàn Tên chương và Ma trận đề thi (Tránh lỗi IndexOutOfRange)
                string topicName = (mappingData.topicName != null && mappingData.topicName.Count > i)
                                    ? mappingData.topicName[i]
                                    : $"Chương {i + 1}";

                int formatExamCount = (mappingData.formatExam != null && mappingData.formatExam.Count > i)
                                        ? mappingData.formatExam[i]
                                        : 0;

                // 4.1 Tạo Topic với dữ liệu đầy đủ
                var topic = new QuestionTopic
                {
                    Name = topicName,
                    LicenseCategoryId = a1Category.Id,
                    NumberOfQuestionsInExam = formatExamCount // Ghi nhận số câu sẽ bốc cho chương này
                };
                _context.QuestionTopics.Add(topic);
                await _context.SaveChangesAsync(); // Cần Save để sinh Topic.Id

                // 4.2 Lấy danh sách ID câu hỏi thuộc chương này (Đã đổi thành mappingData.question)
                var topicQuestionIds = mappingData.question.Skip(startIndex).Take(count).ToList();

                // 4.3 Chuẩn bị dữ liệu cho bảng trung gian
                foreach (var qId in topicQuestionIds)
                {
                    newMappings.Add(new QuestionTopicQuestion
                    {
                        QuestionTopicId = topic.Id,
                        QuestionId = qId
                    });
                }

                // Cập nhật điểm cắt cho chương tiếp theo
                startIndex = endIndex;
            }

            // 5. Ghi một lần toàn bộ Mapping vào DB
            await _context.Set<QuestionTopicQuestion>().AddRangeAsync(newMappings);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[THÀNH CÔNG] Đã map xong {newMappings.Count} câu hỏi vào các chương của Hạng A1.");
        }

        public async Task SeedB2CategoryMappingAsync()
        {
            // 1. Đảm bảo đã có kho câu hỏi
            if (!await _context.Questions.AnyAsync()) return;

            // 2. Lấy hoặc tạo hạng bằng B2
            var b2Category = await _context.LicenseCategories.FirstOrDefaultAsync(c => c.Name == "B2");
            if (b2Category == null)
            {
                b2Category = new LicenseCategory
                {
                    Name = "B2",
                    Description = "Ô tô con số sàn",
                    TotalQuestions = 35,
                    TimeLimit = 22,
                    MinimumPassScore = 32,
                    TotalCriticalQuestions = 1
                };
                _context.LicenseCategories.Add(b2Category);
                await _context.SaveChangesAsync();
            }

            // 3. Đảm bảo luôn có ÍT NHẤT 1 chương tổng hợp cho B2
            int normalQuestionsPerExam = Math.Max(0, b2Category.TotalQuestions - b2Category.TotalCriticalQuestions);

            var topic = await _context.QuestionTopics
                .FirstOrDefaultAsync(t => t.LicenseCategoryId == b2Category.Id &&
                                          t.Name == "Toàn bộ câu hỏi lý thuyết");

            if (topic == null)
            {
                topic = new QuestionTopic
                {
                    Name = "Toàn bộ câu hỏi lý thuyết",
                    Description = "Tập hợp toàn bộ câu hỏi dùng cho hạng B2",
                    LicenseCategoryId = b2Category.Id,
                    NumberOfQuestionsInExam = normalQuestionsPerExam
                };
                _context.QuestionTopics.Add(topic);
                await _context.SaveChangesAsync(); // để có topic.Id
            }
            else
            {
                // Cập nhật lại số câu/đề (phòng trường hợp trước đó cấu hình sai)
                topic.NumberOfQuestionsInExam = normalQuestionsPerExam;
                await _context.SaveChangesAsync();
            }

            // 5. Map toàn bộ câu hỏi (kể cả điểm liệt) vào chương này
            var allQuestionIds = await _context.Questions
                .Select(q => q.Id)
                .ToListAsync();

            var existingMappings = await _context.Set<QuestionTopicQuestion>()
                .Where(m => m.QuestionTopicId == topic.Id)
                .Select(m => m.QuestionId)
                .ToListAsync();

            var newMappings = allQuestionIds
                .Where(qId => !existingMappings.Contains(qId))
                .Select(qId => new QuestionTopicQuestion
                {
                    QuestionTopicId = topic.Id,
                    QuestionId = qId
                })
                .ToList();

            if (newMappings.Any())
            {
                await _context.Set<QuestionTopicQuestion>().AddRangeAsync(newMappings);
                await _context.SaveChangesAsync();
            }

            Console.WriteLine($"[THÀNH CÔNG] Đã đảm bảo map toàn bộ câu hỏi ({allQuestionIds.Count}) vào hạng B2 (thêm mới {newMappings.Count} mapping).");
        }
    }
}