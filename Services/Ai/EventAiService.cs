namespace GameStore.Services
{
    // Trợ lý AI tư vấn TRONG PHẠM VI SỰ KIỆN (dùng Gemini)
    public interface EventAiService
    {
        // Hỏi AI về 1 sự kiện cụ thể; trả về (thành công, câu trả lời)
        Task<(bool ok, string reply)> AskAsync(int eventId, int userId, string question);
    }
}
