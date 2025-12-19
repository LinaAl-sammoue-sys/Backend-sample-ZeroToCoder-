namespace SignUP1_test.DTO
{
    public class SyllabusWeekDto
    {
        public int Week { get; set; }
        public string? Title { get; set; }
        public List<LessonDto> Lessons { get; set; } = new();
    }
}
