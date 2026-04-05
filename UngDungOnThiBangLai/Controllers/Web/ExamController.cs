using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UngDungOnThiBangLai.Models;
using UngDungOnThiBangLai.Services;

namespace UngDungOnThiBangLai.Controllers
{
    public class ExamController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IExamService _examService;

        public ExamController(AppDbContext context, IExamService examService)
        {
            _context = context;
            _examService = examService;
        }

        // 1. Trang danh sách các Hạng bằng để chọn thi (A1, B2...)
        public async Task<IActionResult> Index()
        {
            var categories = await _context.LicenseCategories
                .Include(c => c.QuestionTopics)
                .ToListAsync();
            return View(categories);
        }
        // 2. Action tạo đề thi mới dựa trên cấu hình hạng bằng

        [HttpPost]
        public async Task<IActionResult> CreateExam(int categoryId)
        {
            try
            {
                // Gọi Service để bốc đề ngẫu nhiên theo ma trận
                string examName = $"Đề thi thử - {DateTime.Now:dd/MM/yyyy HH:mm}";
                var exam = await _examService.GenerateRandomExam(categoryId, examName);
                // Sau khi tạo xong, chuyển hướng đến trang làm bài thi
                return RedirectToAction("TakeExam", new { id = exam.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tạo đề thi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> TakeExam(int? id)
        {
            if (!id.HasValue) return RedirectToAction("Index");

            var exam = await _context.Exams
                .Include(e => e.LicenseCategory)
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(e => e.Id == id.Value);

            if (exam == null) return NotFound();

            exam.ExamQuestions = exam.ExamQuestions.OrderBy(eq => eq.Order).ToList();

            return View(exam);
        }

        [HttpPost]
        // SỬA Ở ĐÂY: Thêm [FromForm] cho an toàn tuyệt đối khi map dữ liệu phức tạp (Dictionary)
        public async Task<IActionResult> SubmitExam([FromForm] int ExamId, [FromForm] Dictionary<int, string> Answers)
        {
            // 1. Lấy thông tin đề thi và đáp án đúng
            var exam = await _context.Exams
                .Include(e => e.LicenseCategory)
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(e => e.Id == ExamId);

            if (exam == null) return NotFound();

            int score = 0;
            bool isFailedByCritical = false;
            var resultDetails = new List<QuestionResultViewModel>();

            // (Phần logic chấm điểm của bồ giữ nguyên, viết rất mượt và chuẩn rồi!)
            foreach (var eq in exam.ExamQuestions)
            {
                var question = eq.Question;
                var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);

                // Lấy AnswerId người dùng chọn (Dictionary key là QuestionId)
                int? userAnsId = null;
                if (Answers != null && Answers.ContainsKey(question.Id))
                {
                    if (int.TryParse(Answers[question.Id], out int parsedId))
                    {
                        userAnsId = parsedId;
                    }
                }

                bool isCorrect = (userAnsId == correctAnswer?.Id);
                if (isCorrect) score++;

                // Kiểm tra lỗi điểm liệt: Trả lời sai (hoặc không trả lời) câu hỏi điểm liệt
                if (question.IsCritical && !isCorrect)
                {
                    isFailedByCritical = true;
                }

                resultDetails.Add(new QuestionResultViewModel
                {
                    QuestionText = question.QuestionText,
                    UserAnswerId = userAnsId,
                    CorrectAnswerId = correctAnswer?.Id,
                    IsCorrect = isCorrect,
                    IsCritical = question.IsCritical,
                    Explanation = question.Explanation
                });
            }

            bool isPassed = (score >= exam.PassingScore) && !isFailedByCritical;

            ViewBag.Score = score;
            ViewBag.Total = exam.TotalQuestions;
            ViewBag.IsPassed = isPassed;
            ViewBag.IsFailedByCritical = isFailedByCritical;
            ViewBag.PassingScore = exam.PassingScore;

            return View("Result", resultDetails);
        }
    }
}