namespace UngDungOnThiBangLai.Models.dtos
{
    public class QuestionJsonDto
    {
        public string questionText { get; set; }
        public string explanation { get; set; }
        public List<AnswerJsonDto> answers { get; set; }
    }

    public class AnswerJsonDto
    {
        public bool isCorrect { get; set; }
        public string text { get; set; }
    }

    public class CategoryMappingDto
    {
        public List<int> question { get; set; } = new List<int>(); // Lưu ý: Tên key trong json là 'question' không có 's'
        public List<int> topic { get; set; } = new List<int>();
        public List<string> topicName { get; set; } = new List<string>();
        public List<int> formatExam { get; set; } = new List<int>();
        public int criticalQuestion { get; set; }
        public int minimumScore { get; set; }
        public int timeExam { get; set; }
    }
}
