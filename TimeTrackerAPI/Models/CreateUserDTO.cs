using System.ComponentModel.DataAnnotations;

namespace TimeTrackerAPI.Models
{
    public class CreateUserDTO
    {
        [Required]
        [MinLength(4, ErrorMessage = "Login must be at least 4 characters long.")]
        [RegularExpression(@"^[a-zA-Z]{4,}$", ErrorMessage = "Login must be at least 4 characters long and contain only Latin letters.")]
        public string Login { get; set; }

        [Required]         
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*\d)[a-z\d]{8,}$", ErrorMessage = "Password must be at least 8 characters long, contain only lowercase Latin letters, and include at least one number.")]
        public string Password { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; }
    }
}
