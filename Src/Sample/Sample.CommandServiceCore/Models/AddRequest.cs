using System.ComponentModel.DataAnnotations;
using Sample.DTO;

namespace Sample.CommandServiceCore.Models
{
    public class AddRequest
    {
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string File { get; set; }
        public CommonStatus Status { get; set; }
    }
}
