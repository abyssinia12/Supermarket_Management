using System.ComponentModel.DataAnnotations;

namespace SuperMarket.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = null!;
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
    }
}
