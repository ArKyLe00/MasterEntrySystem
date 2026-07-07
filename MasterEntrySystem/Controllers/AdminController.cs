using MasterEntrySystem.Data;
using MasterEntrySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MasterEntrySystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            // Get all workers
            var workers = await _context.Users.Where(u => u.Role == "Worker").ToListAsync();
            
            // Get all task assignments
            var taskAssignments = await _context.TaskAssignments
                                                .Include(t => t.Worker)
                                                .OrderByDescending(t => t.StartDate)
                                                .ToListAsync();

            ViewBag.Workers = workers;
            ViewBag.TaskAssignments = taskAssignments;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignTask(int workerId, string taskName, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(taskName))
            {
                TempData["ErrorMessage"] = "Task name cannot be empty.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (startDate > endDate)
            {
                TempData["ErrorMessage"] = "Start date must be before or equal to the end date.";
                return RedirectToAction(nameof(Dashboard));
            }

            var worker = await _context.Users.FindAsync(workerId);
            if (worker == null || worker.Role != "Worker")
            {
                TempData["ErrorMessage"] = "Invalid worker selected.";
                return RedirectToAction(nameof(Dashboard));
            }

            var assignment = new TaskAssignment
            {
                WorkerId = workerId,
                TaskName = taskName,
                StartDate = startDate,
                EndDate = endDate
            };

            _context.TaskAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task assigned successfully.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
