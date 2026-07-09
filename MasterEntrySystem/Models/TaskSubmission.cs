using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterEntrySystem.Models
{
    public class TaskSubmission
    {
        public int Id { get; set; }

        [Required]
        public int WorkerId { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public AppUser? Worker { get; set; }

        [Required]
        public int TaskAssignmentId { get; set; }

        [ForeignKey(nameof(TaskAssignmentId))]
        public TaskAssignment? TaskAssignment { get; set; }

        // Persist the computed week bucket so the worker dropdown remains stable.
        // We compute it from TaskAssignment.StartDate/EndDate (Mon-Sun).
        [Required]
        public DateTime WeekStart { get; set; }

        [Required]
        public DateTime WeekEnd { get; set; }

        [Required]
        [MaxLength(10000)]
        public string CommentText { get; set; } = string.Empty;

        [Required]
        public bool IsDraft { get; set; }

        public DateTime? SubmittedAt { get; set; }
    }
}
