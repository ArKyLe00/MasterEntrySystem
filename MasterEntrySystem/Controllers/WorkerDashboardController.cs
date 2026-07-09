using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MasterEntrySystem.Data;
using MasterEntrySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MasterEntrySystem.Controllers
{
    [Authorize(Roles = "Worker")]
    public class WorkerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentWorkerId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var workerId))
                throw new InvalidOperationException("Worker id claim is missing or invalid.");

            return workerId;
        }

        // Bucket week as Mon-Sun based on WeekStart.
        private static DateTime GetWeekStart(DateTime date)
        {
            // normalize to date-only
            date = date.Date;

            // In C#: DayOfWeek Sunday=0 ... Saturday=6
            // We want Monday as start.
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff);
        }

        private static DateTime GetWeekEnd(DateTime weekStart)
        {
            return weekStart.Date.AddDays(6);
        }

        private static string FormatWeek(DateTime start, DateTime end)
        {
            // e.g. Jun 3 - Jun 9
            return $"{start.ToString("MMM d", CultureInfo.InvariantCulture)} - {end.ToString("MMM d", CultureInfo.InvariantCulture)}";
        }

        public async Task<IActionResult> Dashboard(DateTime? weekStart)
        {
            var workerId = GetCurrentWorkerId();

            var assignments = await _context.TaskAssignments
                .Where(t => t.WorkerId == workerId)
                .OrderBy(t => t.StartDate)
                .ToListAsync();

            var weeks = assignments
                .Select(a => GetWeekStart(a.StartDate))
                .Distinct()
                .OrderByDescending(d => d)
                .Select(ws => new WeekOption
                {
                    WeekStart = ws,
                    WeekEnd = GetWeekEnd(ws),
                    DisplayText = FormatWeek(GetWeekStart(ws), GetWeekEnd(ws))
                })
                .ToList();

            // Temporary debug to diagnose "dashboard not showing"
            ViewBag.Debug = new
            {
                WorkerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier),
                WorkerIdParsed = workerId,
                RoleClaim = User.FindFirstValue(ClaimTypes.Role),
                TaskAssignmentsCountForWorker = assignments.Count,
                WeeksCount = weeks.Count
            };

            if (!weeks.Any())
            {
                ViewBag.Weeks = weeks;
                ViewBag.SelectedWeekStart = null;
                ViewBag.Tasks = new List<TaskAssignment>();
                ViewBag.SubmissionByAssignmentId = new Dictionary<int, TaskSubmission>();
                return View("~/Views/Worker/Dashboard.cshtml");
            }

            DateTime selectedWeekStartValue = weekStart?.Date ?? weeks.First().WeekStart;
            var selectedWeekEndValue = GetWeekEnd(selectedWeekStartValue);

            var tasksForWeek = assignments
                .Where(a =>
                {
                    // Keep it simple: include assignments that overlap the week bucket.
                    return a.StartDate.Date <= selectedWeekEndValue && a.EndDate.Date >= selectedWeekStartValue;
                })
                .OrderBy(a => a.StartDate)
                .ToList();

            var assignmentIds = tasksForWeek.Select(t => t.Id).ToList();

            var submissions = await _context.TaskSubmissions
                .Where(s => s.WorkerId == workerId && assignmentIds.Contains(s.TaskAssignmentId) && s.WeekStart == selectedWeekStartValue)
                .ToListAsync();

            var submissionByAssignmentId = submissions.ToDictionary(s => s.TaskAssignmentId, s => s);

            ViewBag.Weeks = weeks;
            ViewBag.SelectedWeekStart = selectedWeekStartValue;
            ViewBag.Tasks = tasksForWeek;
            ViewBag.SubmissionByAssignmentId = submissionByAssignmentId;

            return View("~/Views/Worker/Dashboard.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(int taskAssignmentId, DateTime weekStart, string commentText)
        {
            var workerId = GetCurrentWorkerId();

            var assignment = await _context.TaskAssignments
                .FirstOrDefaultAsync(t => t.Id == taskAssignmentId && t.WorkerId == workerId);

            if (assignment == null)
                return Unauthorized();

            var computedWeekStart = GetWeekStart(assignment.StartDate);
            if (computedWeekStart.Date != weekStart.Date)
            {
                // Force consistency: WeekStart parameter must match bucket computed from assignment.
                weekStart = computedWeekStart;
            }

            weekStart = weekStart.Date;
            var weekEnd = GetWeekEnd(weekStart);

            commentText ??= string.Empty;

            var existing = await _context.TaskSubmissions.FirstOrDefaultAsync(s =>
                s.TaskAssignmentId == taskAssignmentId &&
                s.WorkerId == workerId &&
                s.WeekStart == weekStart);

            if (existing == null)
            {
                existing = new TaskSubmission
                {
                    WorkerId = workerId,
                    TaskAssignmentId = taskAssignmentId,
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    CommentText = commentText,
                    IsDraft = true,
                    SubmittedAt = null
                };
                _context.TaskSubmissions.Add(existing);
            }
            else
            {
                existing.CommentText = commentText;
                existing.IsDraft = true;
                existing.SubmittedAt = existing.SubmittedAt; // keep as-is if already submitted
                existing.WeekEnd = weekEnd;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard), new { weekStart = weekStart.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int taskAssignmentId, DateTime weekStart, string commentText)
        {
            var workerId = GetCurrentWorkerId();

            var assignment = await _context.TaskAssignments
                .FirstOrDefaultAsync(t => t.Id == taskAssignmentId && t.WorkerId == workerId);

            if (assignment == null)
                return Unauthorized();

            var computedWeekStart = GetWeekStart(assignment.StartDate);
            if (computedWeekStart.Date != weekStart.Date)
                weekStart = computedWeekStart;

            weekStart = weekStart.Date;
            var weekEnd = GetWeekEnd(weekStart);

            commentText ??= string.Empty;

            var existing = await _context.TaskSubmissions.FirstOrDefaultAsync(s =>
                s.TaskAssignmentId == taskAssignmentId &&
                s.WorkerId == workerId &&
                s.WeekStart == weekStart);

            if (existing == null)
            {
                existing = new TaskSubmission
                {
                    WorkerId = workerId,
                    TaskAssignmentId = taskAssignmentId,
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    CommentText = commentText,
                    IsDraft = false,
                    SubmittedAt = DateTime.UtcNow
                };
                _context.TaskSubmissions.Add(existing);
            }
            else
            {
                existing.CommentText = commentText;
                existing.IsDraft = false;
                existing.SubmittedAt = DateTime.UtcNow;
                existing.WeekEnd = weekEnd;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard), new { weekStart = weekStart.ToString("yyyy-MM-dd") });
        }

        private sealed class WeekOption
        {
            public DateTime WeekStart { get; set; }
            public DateTime WeekEnd { get; set; }
            public string DisplayText { get; set; } = string.Empty;
        }
    }
}
