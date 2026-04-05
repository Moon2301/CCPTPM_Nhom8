using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UngDungOnThiBangLai.Controllers.API
{
    [ApiController]
    [Route("api/Question")] 
    public class QuestionApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuestionApiController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách câu hỏi và đáp án thuộc về một Chương (Topic) cụ thể.
        /// URL gọi: GET https://localhost:<port>/api/Question/ByTopic/5
        /// </summary>
        /// <param name="topicId">ID của Chương (Topic)</param>
        [HttpGet("ByTopic/{topicId}")]
        public async Task<IActionResult> GetQuestionsByTopic(int topicId)
        {
            try
            {
                // 1. Kiểm tra tính hợp lệ của Topic
                bool topicExists = await _context.QuestionTopics.AnyAsync(t => t.Id == topicId);
                if (!topicExists)
                {
                    return NotFound(new { message = $"Không tìm thấy Chương có ID = {topicId}" });
                }

                // 2. Truy vấn dữ liệu xuyên qua bảng trung gian (Many-to-Many)
                var questions = await _context.Questions
                    .Where(q => q.QuestionTopics.Any(qt => qt.QuestionTopicId == topicId))
                    .OrderBy(q => q.Id) // Sắp xếp theo ID để người dùng học từ trên xuống dưới
                    .Select(q => new
                    {
                        id = q.Id,
                        questionText = q.QuestionText,
                        imageUrl = q.ImageUrl,
                        isCritical = q.IsCritical,
                        explanation = q.Explanation, // Rất quan trọng cho chế độ ôn luyện

                        // Map luôn danh sách đáp án đi kèm
                        answers = q.Answers.Select(a => new
                        {
                            id = a.Id,
                            answerText = a.AnswerText,
                            isCorrect = a.IsCorrect, // Cần thiết để frontend biết câu nào đúng ngay lập tức
                            imageUrl = a.ImageUrl
                        }).ToList()
                    })
                    .ToListAsync();

                // 3. Trả về kết quả
                if (!questions.Any())
                {
                    return Ok(new { message = "Chương này hiện chưa có câu hỏi nào.", data = new List<object>() });
                }

                return Ok(questions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ khi truy xuất câu hỏi", details = ex.Message });
            }
        }
    }
}
