using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UngDungOnThiBangLai.Models;

namespace UngDungOnThiBangLai.Controllers.Web
{
    public class QuestionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public QuestionController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- HELPER METHODS ---
        private async Task<string?> SaveFile(IFormFile? file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            string folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", subFolder);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/uploads/{subFolder}/{fileName}";
        }

        private void DeleteFile(string? fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, fileUrl.TrimStart('/'));
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }

        // ================== INDEX ==================
        public async Task<IActionResult> Index(string searchString, int? categoryId, int? topicId, int page = 1)
        {
            int pageSize = 20;

            // 1. Eager Loading qua bảng trung gian
            var query = _context.Questions
                .Include(q => q.QuestionTopics)
                    .ThenInclude(qt => qt.QuestionTopic)
                        .ThenInclude(t => t.LicenseCategory)
                .AsQueryable();

            // 2. Lọc dữ liệu
            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(q => q.QuestionText.Contains(searchString));

            // Truy vấn xuyên qua bảng trung gian QuestionTopicQuestion
            if (categoryId.HasValue)
                query = query.Where(q => q.QuestionTopics.Any(qt => qt.QuestionTopic.LicenseCategoryId == categoryId));

            if (topicId.HasValue)
                query = query.Where(q => q.QuestionTopics.Any(qt => qt.QuestionTopicId == topicId));

            // 3. Tính toán phân trang
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            page = page < 1 ? 1 : page;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // 4. Map sang ViewModel
            var result = await query
                .OrderByDescending(q => q.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new QuestionListViewModel
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    ImageUrl = q.ImageUrl,
                    IsCritical = q.IsCritical,
                    QuestionType = q.QuestionType,
                    // Vì 1 câu hỏi có thể thuộc NHIỀU hạng/chương, ta nối chuỗi lại để hiển thị
                    CategoryName = string.Join(", ", q.QuestionTopics.Select(qt => qt.QuestionTopic.LicenseCategory.Name).Distinct()),
                    TopicName = string.Join(", ", q.QuestionTopics.Select(qt => qt.QuestionTopic.Name).Distinct())
                }).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;
            ViewBag.TopicId = topicId;

            ViewBag.Categories = await _context.LicenseCategories.ToListAsync();
            if (categoryId.HasValue)
            {
                ViewBag.Topics = await _context.QuestionTopics
                    .Where(t => t.LicenseCategoryId == categoryId.Value)
                    .ToListAsync();
            }

            return View(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetTopicsByCategory(int categoryId)
        {
            var topics = await _context.QuestionTopics
                .Where(t => t.LicenseCategoryId == categoryId)
                .Select(t => new { id = t.Id, name = t.Name })
                .ToListAsync();
            return Json(topics);
        }

        // ================== CREATE ==================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.LicenseCategories.ToListAsync(), "Id", "Name");

            return View(new CreateQuestionViewModel
            {
                Answers = new List<AnswerViewModel> { new AnswerViewModel(), new AnswerViewModel() }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQuestionViewModel model)
        {
            if (ModelState.IsValid)
            {
                string? questionImageUrl = await SaveFile(model.ImageFile, "questions");
                if (string.IsNullOrEmpty(questionImageUrl))
                {
                    questionImageUrl = Request.Form["ImageUrl"];
                }

                // Khởi tạo câu hỏi mới (Không còn LicenseCategoryId hay QuestionTopicId)
                var question = new Question
                {
                    QuestionText = model.QuestionText,
                    Explanation = model.Explanation,
                    IsCritical = model.IsCritical,
                    QuestionType = "MultipleChoice",
                    ImageUrl = questionImageUrl,
                    Answers = new List<Answer>(),
                    QuestionTopics = new List<QuestionTopicQuestion>() // Khởi tạo List cho bảng trung gian
                };

                // Nếu người dùng chọn Topic từ giao diện, thêm mapping vào bảng trung gian
                if (model.QuestionTopicId > 0)
                {
                    question.QuestionTopics.Add(new QuestionTopicQuestion
                    {
                        QuestionTopicId = model.QuestionTopicId
                    });
                }

                if (model.Answers != null)
                {
                    foreach (var a in model.Answers)
                    {
                        string? answerImageUrl = await SaveFile(a.ImageFile, "answers");
                        question.Answers.Add(new Answer
                        {
                            AnswerText = a.AnswerText,
                            IsCorrect = a.IsCorrect,
                            ImageUrl = answerImageUrl
                        });
                    }
                }

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.LicenseCategories.ToListAsync(), "Id", "Name", model.LicenseCategoryId);
            return View(model);
        }

        // ================== EDIT ==================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Cần Include QuestionTopics để lấy thông tin mapping hiện tại
            var question = await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.QuestionTopics)
                    .ThenInclude(qt => qt.QuestionTopic)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound();

            // Giả định UI Edit hiện tại chỉ hỗ trợ gán vào 1 Topic (Lấy topic đầu tiên để bind lên UI)
            var currentMapping = question.QuestionTopics.FirstOrDefault();
            int currentTopicId = currentMapping?.QuestionTopicId ?? 0;
            int currentCategoryId = currentMapping?.QuestionTopic?.LicenseCategoryId ?? 0;

            var categories = await _context.LicenseCategories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", currentCategoryId);
            ViewBag.ImageUrl = question.ImageUrl;

            var model = new CreateQuestionViewModel
            {
                Id = question.Id,
                LicenseCategoryId = currentCategoryId,
                QuestionTopicId = currentTopicId,
                QuestionText = question.QuestionText,
                Explanation = question.Explanation,
                IsCritical = question.IsCritical,
                QuestionType = question.QuestionType,
                Answers = question.Answers.Select(a => new AnswerViewModel
                {
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect,
                    imgUrl = a.ImageUrl
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateQuestionViewModel model)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.QuestionTopics) // Phải Include để sửa bảng trung gian
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // --- XỬ LÝ ẢNH CÂU HỎI ---
                    if (model.ImageFile != null)
                    {
                        if (!string.IsNullOrEmpty(question.ImageUrl) && question.ImageUrl.Contains("/uploads/"))
                            DeleteFile(question.ImageUrl);

                        question.ImageUrl = await SaveFile(model.ImageFile, "questions");
                    }
                    else
                    {
                        string? urlFromLibrary = Request.Form["ImageUrl"];
                        if (!string.IsNullOrEmpty(urlFromLibrary))
                        {
                            if (question.ImageUrl != urlFromLibrary && !string.IsNullOrEmpty(question.ImageUrl) && question.ImageUrl.Contains("/uploads/"))
                                DeleteFile(question.ImageUrl);

                            question.ImageUrl = urlFromLibrary;
                        }
                        else if (string.IsNullOrEmpty(urlFromLibrary) && model.ImageFile == null)
                        {
                            if (!string.IsNullOrEmpty(question.ImageUrl) && question.ImageUrl.Contains("/uploads/"))
                                DeleteFile(question.ImageUrl);

                            question.ImageUrl = null;
                        }
                    }

                    // --- CẬP NHẬT THÔNG TIN CƠ BẢN ---
                    question.QuestionText = model.QuestionText;
                    question.Explanation = model.Explanation;
                    question.IsCritical = model.IsCritical;

                    // --- CẬP NHẬT QUAN HỆ NHIỀU-NHIỀU (TOPIC) ---
                    // Xóa các liên kết topic cũ
                    question.QuestionTopics.Clear();

                    // Thêm liên kết topic mới từ giao diện
                    if (model.QuestionTopicId > 0)
                    {
                        question.QuestionTopics.Add(new QuestionTopicQuestion
                        {
                            QuestionTopicId = model.QuestionTopicId
                        });
                    }

                    // --- XỬ LÝ ĐÁP ÁN ---
                    if (question.Answers != null && question.Answers.Any())
                    {
                        _context.Answers.RemoveRange(question.Answers);
                    }

                    if (model.Answers != null)
                    {
                        foreach (var a in model.Answers)
                        {
                            string? finalAnsImg = a.ImageFile != null
                                ? await SaveFile(a.ImageFile, "answers")
                                : a.imgUrl;

                            question.Answers.Add(new Answer
                            {
                                AnswerText = a.AnswerText,
                                IsCorrect = a.IsCorrect,
                                ImageUrl = finalAnsImg
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu: " + ex.Message);
                }
            }

            var categories = await _context.LicenseCategories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", model.LicenseCategoryId);
            ViewBag.ImageUrl = question.ImageUrl;

            return View(model);
        }

        // ================== DELETE ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                // Không cần Include QuestionTopics vì Entity Framework Core 
                // tự động xử lý Cascade Delete trên bảng trung gian nếu đã cấu hình
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound();

            try
            {
                if (!string.IsNullOrEmpty(question.ImageUrl))
                {
                    DeleteFile(question.ImageUrl);
                }

                if (question.Answers != null && question.Answers.Any())
                {
                    foreach (var answer in question.Answers)
                    {
                        if (!string.IsNullOrEmpty(answer.ImageUrl))
                        {
                            DeleteFile(answer.ImageUrl);
                        }
                    }
                    _context.Answers.RemoveRange(question.Answers);
                }

                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã xóa câu hỏi và toàn bộ hình ảnh liên quan thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa câu hỏi: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}