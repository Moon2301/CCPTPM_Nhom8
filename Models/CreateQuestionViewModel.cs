namespace UngDungOnThiBangLai.Models
{
    public class CreateQuestionViewModel
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
        public int LicenseCategoryId { get; set; }
        public string? Explanation { get; set; }
        public bool IsCritical { get; set; }
        public string QuestionType { get; set; } = "MultipleChoice";
        public int QuestionTopicId { get; set; }
        public string? ImageURL { get; set; }

        // Hứng file ảnh tải lên
        public IFormFile? ImageFile { get; set; }

        // Danh sách các đáp án đi kèm
        public List<AnswerViewModel> Answers { get; set; } = new List<AnswerViewModel>();
    }

    public class AnswerViewModel
    {
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; }
        public string? imgUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}