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
            await SeedTrafficSignsAsync();
            await SeedMasterPoolQuestionsAsync();
            await SeedCriticalQuestionsAsync();
            await SeedA1CategoryMappingAsync();
            await SeedB1CategoryMappingAsync();
        }

        public async Task SeedTrafficSignsAsync()
        {
            // Nếu đã có dữ liệu biển báo thì bỏ qua để tránh seed lặp
            if (await _context.TrafficSigns.AnyAsync()) return;

            // Ưu tiên đúng tên thư mục hiện có trong repo: wwwroot/Data/trafficSigns.json
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Data", "trafficSigns.json");
            if (!File.Exists(filePath))
            {
                // Fallback nếu ai đó đổi tên thư mục thành "data"
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "trafficSigns.json");
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[CẢNH BÁO] Không tìm thấy file biển báo tại: {filePath}");
                return;
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dtos = JsonSerializer.Deserialize<List<TrafficSignJsonDto>>(jsonString, options);

            if (dtos == null || !dtos.Any()) return;

            static string NormalizeCode(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return "";
                var code = raw.Trim();
                if (code.Contains('.')) return code;

                // P102 -> P.102 ; P103a -> P.103a ; DP133 -> DP.133 ...
                var m = Regex.Match(code, @"^([A-Za-z]+)(\d.*)$");
                return m.Success ? $"{m.Groups[1].Value}.{m.Groups[2].Value}" : code;
            }

            static string NormalizeCategory(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return "Biển cấm";
                var s = raw.Trim().ToLowerInvariant();

                if (s.Contains("cấm")) return "Biển cấm";
                if (s.Contains("nguy hiểm")) return "Biển báo nguy hiểm";
                if (s.Contains("hiệu lệnh")) return "Biển hiệu lệnh";
                if (s.Contains("chỉ dẫn")) return "Biển chỉ dẫn";
                if (s.Contains("biển phụ") || (s.Contains("phụ") && s.Contains("biển"))) return "Biển phụ";
                if (s.Contains("vạch") && s.Contains("kẻ")) return "Vạch kẻ đường";

                // Nếu không map được thì giữ nguyên nhưng chuẩn hoá hoa thường
                return raw.Trim();
            }

            var signs = new List<TrafficSign>(dtos.Count);
            foreach (var dto in dtos)
            {
                var code = NormalizeCode(dto.code);
                if (string.IsNullOrEmpty(code)) continue;

                var imageUrl = (dto.imageFile ?? "").Trim();
                if (string.IsNullOrEmpty(imageUrl))
                {
                    // Fallback theo convention nếu JSON thiếu imageFile
                    // VD: P.101 -> /images/trafficsigns/signP101.webp
                    var fileKey = code.Replace(".", "");
                    imageUrl = $"/images/trafficsigns/sign{fileKey}.webp";
                }

                signs.Add(new TrafficSign
                {
                    Code = code,
                    Name = (dto.name ?? "").Trim(),
                    Category = NormalizeCategory(dto.category),
                    Description = (dto.description ?? "").Trim(),
                    ImageUrl = imageUrl
                });
            }

            if (signs.Count == 0) return;

            await _context.TrafficSigns.AddRangeAsync(signs);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[THÀNH CÔNG] Đã nạp thành công {signs.Count} biển báo từ trafficSigns.json.");
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

            // 2. Lấy hoặc Tạo Hạng bằng A1 (Sử dụng dữ liệu động từ JSON)
            var a1Category = await _context.LicenseCategories.FirstOrDefaultAsync(c => c.Name == "A1");
            if (a1Category == null)
            {
                // Tính tổng số câu hỏi trong đề dựa vào ma trận formatExam
                int totalQuestionsInExam = mappingData.formatExam != null ? mappingData.formatExam.Sum() : 25;

                a1Category = new LicenseCategory
                {
                    Name = "A1",
                    Description = "Xe mô tô hai bánh dung tích xilanh dưới 175 cm3",
                    TotalQuestions = totalQuestionsInExam, // Lấy từ JSON (12 + 3 + 4 + 2 + 2 = 23 hoặc 25 tùy cấu hình)
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

        public async Task SeedB1CategoryMappingAsync()
        {
            // 1. Đọc file cấu hình
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "B1.json");
            if (!File.Exists(filePath))
            {
                Console.WriteLine("[CẢNH BÁO] Không tìm thấy file B1.json.");
                return;
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var mappingData = JsonSerializer.Deserialize<CategoryMappingDto>(jsonString, options);

            // Lưu ý: Đã đổi 'questions' thành 'question' theo đúng DTO mới
            if (mappingData == null || !mappingData.question.Any() || !mappingData.topic.Any()) return;

            // 2. Lấy hoặc Tạo Hạng bằng A1 (Sử dụng dữ liệu động từ JSON)
            var a1Category = await _context.LicenseCategories.FirstOrDefaultAsync(c => c.Name == "B1");
            if (a1Category == null)
            {
                // Tính tổng số câu hỏi trong đề dựa vào ma trận formatExam
                int totalQuestionsInExam = mappingData.formatExam != null ? mappingData.formatExam.Sum() : 25;

                a1Category = new LicenseCategory
                {
                    Name = "B1",
                    Description = "Xe ô tô 4 chỗ",
                    TotalQuestions = totalQuestionsInExam, // Lấy từ JSON (12 + 3 + 4 + 2 + 2 = 23 hoặc 25 tùy cấu hình)
                    TimeLimit = mappingData.timeExam > 0 ? mappingData.timeExam : 19, // Lấy từ JSON
                    MinimumPassScore = mappingData.minimumScore > 0 ? mappingData.minimumScore : 21, // Lấy từ JSON
                    TotalCriticalQuestions = mappingData.criticalQuestion
                };
                _context.LicenseCategories.Add(a1Category);
                await _context.SaveChangesAsync();
            }

            // 3. Chặn rác
            if (await _context.QuestionTopics.AnyAsync(t => t.LicenseCategoryId == a1Category.Id))
            {
                Console.WriteLine("[THÔNG TIN] Hạng B1 đã được map dữ liệu trước đó. Bỏ qua...");
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

            Console.WriteLine($"[THÀNH CÔNG] Đã map xong {newMappings.Count} câu hỏi vào các chương của Hạng B1.");
        }

    }
}