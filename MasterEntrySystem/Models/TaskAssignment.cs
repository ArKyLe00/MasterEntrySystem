using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterEntrySystem.Models
{
    public class TaskAssignment
    {
        public int Id { get; set; }

        [Required]
        public int WorkerId { get; set; }

        [ForeignKey("WorkerId")]
        public AppUser? Worker { get; set; }

        [Required]
        [MaxLength(200)]
        public string TaskName { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}
