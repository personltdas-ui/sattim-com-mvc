using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Message
{
    public class MessageViewModel
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }
        public string SenderName { get; set; }

        [Required]
        public string ReceiverId { get; set; }
        public string ReceiverName { get; set; }

        public int? ProductId { get; set; }
        public string? ProductTitle { get; set; }

        [Required(ErrorMessage = "Mesaj içeriği zorunludur")]
        [StringLength(1000, ErrorMessage = "Mesaj en fazla 1000 karakter olabilir")]
        [Display(Name = "Mesaj")]
        public string Content { get; set; }

        public bool IsRead { get; set; }
        public DateTime SentDate { get; set; }
        public DateTime? ReadDate { get; set; }
    }
}
