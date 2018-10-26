using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.CommandServiceCore.Models
{
    public class AddRequest
    {
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string File { get; set; }
    }
}
