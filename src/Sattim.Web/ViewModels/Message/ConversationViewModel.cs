namespace Sattim.Web.ViewModels.Message
{
    public class ConversationViewModel
    {
        public string OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string? OtherUserImage { get; set; }
        public List<MessageViewModel> Messages { get; set; } = new();
        public int UnreadCount { get; set; }
        public DateTime? LastMessageDate { get; set; }
    }
}
