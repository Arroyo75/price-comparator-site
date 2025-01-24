using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace price_comparator_site.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string Nickname { get; set; }
    }
}
