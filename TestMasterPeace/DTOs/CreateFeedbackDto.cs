using System.ComponentModel.DataAnnotations;

namespace TestMasterPeace.DTOs // Ensure this namespace is correct
{
    public class CreateFeedbackDto
    {
        [Required]
        public long ProductId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }
} 