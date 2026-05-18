using Microsoft.AspNetCore.Mvc;
using QTC_Admin_Application.Services;

namespace QTC_Admin_Application.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly WorkflowService _workflowService;

        public AnalyticsController(WorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var username = HttpContext.Session.GetString("Username") ?? "";

            if (!username.Equals("Ruben", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "You do not have permission to view the Investor Dashboard.";
                return RedirectToAction("Index", "Home");
            }

            var workflows = await _workflowService.GetAllAsync();

            var totalWorkflows = workflows.Count;
            var completedWorkflows = 0;

            var completionRate = totalWorkflows == 0 ? 0 :
                (double)completedWorkflows / totalWorkflows * 100;

            var workflowsByMonth = workflows
                .Where(w => w.CreatedDate != default)
                .GroupBy(w => new { w.CreatedDate.Year, w.CreatedDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Count = g.Count()
                })
                .ToList();

            ViewBag.TotalWorkflows = totalWorkflows;
            ViewBag.CompletionRate = Math.Round(completionRate, 2);
            ViewBag.WorkflowsByMonth = System.Text.Json.JsonSerializer.Serialize(workflowsByMonth);

            return View();
        }
    }
}