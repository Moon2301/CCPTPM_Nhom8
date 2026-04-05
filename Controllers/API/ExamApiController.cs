using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UngDungOnThiBangLai.Models;
using UngDungOnThiBangLai.Services;

namespace UngDungOnThiBangLai.Controllers.API 
{
    [ApiController]
    [Route("api/Exam")] 
    public class ExamApiController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly AppDbContext _context;

        public ExamApiController(IExamService examService, AppDbContext context)
        {
            _examService = examService;
            _context = context;
        }

        // POST: https://localhost:<port>/api/Exam/CreateExam?categoryId=1
        [HttpPost("CreateExam")]
        public async Task<IActionResult> CreateExam([FromQuery] int categoryId) // Sửa string thành int
        {
            try
            {
                // 1. Gọi Service tạo đề ngẫu nhiên và lưu vào DB
                string examName = $"Đề thi thử (App) - {DateTime.Now:dd/MM/yyyy HH:mm}";
                var newExam = await _examService.GenerateRandomExam(categoryId, examName);

                // 2. Tải toàn bộ chi tiết đề thi (kèm Câu hỏi và Đáp án) từ DB
                // Do hàm GenerateRandomExam chỉ lưu ID, ta cần Include để lấy nội dung (text, image...)
                var fullExam = await _context.Exams
                    .Include(e => e.LicenseCategory)
                    .Include(e => e.ExamQuestions)
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.Answers)
                    .FirstOrDefaultAsync(e => e.Id == newExam.Id);

                if (fullExam == null)
                {
                    return NotFound(new { message = "Không thể tải chi tiết đề thi vừa tạo." });
                }

                // 3. Mapping dữ liệu sang cấu trúc JSON chuẩn mực cho React App
                var examData = new
                {
                    examId = fullExam.Id.ToString(), // React thường xử lý ID dạng chuỗi tốt hơn
                    licenseCategory = new
                    {
                        id = fullExam.LicenseCategory.Id.ToString(),
                        name = fullExam.LicenseCategory.Name
                    },
                    timeLimit = fullExam.TimeLimit,

                    // Sắp xếp đúng theo ma trận đề thi (Order) đã tạo trong Service
                    questions = fullExam.ExamQuestions.OrderBy(eq => eq.Order).Select(eq => new
                    {
                        id = eq.Question.Id.ToString(),
                        text = eq.Question.QuestionText,
                        imageUrl = eq.Question.ImageUrl,
                        isCritical = eq.Question.IsCritical,
                        explanation = eq.Question.Explanation,

                        answers = eq.Question.Answers.Select(a => new
                        {
                            id = a.Id.ToString(),
                            text = a.AnswerText,
                            imageUrl = a.ImageUrl,
                            isCorrect = a.IsCorrect
                        }).ToList()
                    }).ToList()
                };

                return Ok(examData);
            }
            catch (Exception ex)
            {
                // Bắt lỗi an toàn trả về cho Frontend
                return StatusCode(500, new { message = "Lỗi khi khởi tạo đề thi", details = ex.Message });
            }
        }
    }
}