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

        [HttpPost]
        public async Task<IActionResult> AddWorker(string name, string email, string password, string department, string designation, string status)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Name, email, and password are required.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Check for duplicate email
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "A user with this email already exists.";
                return RedirectToAction(nameof(Dashboard));
            }

            var worker = new AppUser
            {
                Name = name,
                Email = email,
                PasswordHash = password, // In a real app, hash this!
                Role = "Worker",
                Department = department ?? string.Empty,
                Designation = designation ?? string.Empty,
                Status = status ?? "Active"
            };

            _context.Users.Add(worker);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Worker \"{name}\" added successfully.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> EditWorker(int id, string name, string email, string department, string designation, string status)
        {
            var worker = await _context.Users.FindAsync(id);
            if (worker == null || worker.Role != "Worker")
            {
                TempData["ErrorMessage"] = "Worker not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Name and email are required.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Check for duplicate email (excluding current worker)
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Id != id);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Another user with this email already exists.";
                return RedirectToAction(nameof(Dashboard));
            }

            worker.Name = name;
            worker.Email = email;
            worker.Department = department ?? string.Empty;
            worker.Designation = designation ?? string.Empty;
            worker.Status = status ?? "Active";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Worker \"{name}\" updated successfully.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteWorker(int id)
        {
            var worker = await _context.Users.FindAsync(id);
            if (worker == null || worker.Role != "Worker")
            {
                TempData["ErrorMessage"] = "Worker not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            _context.Users.Remove(worker);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Worker \"{worker.Name}\" deleted successfully.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
