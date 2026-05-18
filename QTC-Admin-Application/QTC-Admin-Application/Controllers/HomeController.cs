using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QTC_Admin_Application.Filters;
using QTC_Admin_Application.Models;
using QTC_Admin_Application.Services;
using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace QTC_Admin_Application.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly WorkflowService _workflowService;
        private readonly WorkflowStepService _workflowStepService;
        private readonly UserService _userService;
        private readonly WorkflowReportService _workflowReportService;

        private const int WorkflowNameMaxLength = 200;
        private const int WorkflowTypeMaxLength = 200;
        private const int StepNameMaxLength = 200;
        private const int StepDescMaxLength = 500;
        private const int StepTypeMaxLength = 200;

        public HomeController(
            ILogger<HomeController> logger,
            WorkflowService workflowService,
            WorkflowStepService workflowStepService,
            UserService userService,
            WorkflowReportService workflowReportService)
        {
            _logger = logger;
            _workflowService = workflowService;
            _workflowStepService = workflowStepService;
            _userService = userService;
            _workflowReportService = workflowReportService;
        }

        // Helper method to check if current user is Admin
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> Index(string? search, string? sort, int page = 1)
        {
            const int pageSize = 10;

            var pagedResult = await _workflowService.SearchPagedAsync(search, sort, page, pageSize);
            var totalPages = pagedResult.TotalCount == 0
                ? 1
                : (int)Math.Ceiling(pagedResult.TotalCount / (double)pageSize);

            var workflowIds = pagedResult.Items.Select(w => w.WorkflowId).ToList();
            var workflowStatuses = await _workflowService.GetWorkflowStatusesAsync(workflowIds);

            ViewData["CurrentSearch"] = search;
            ViewData["CurrentSort"] = sort;
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalCount"] = pagedResult.TotalCount;
            ViewData["TotalPages"] = totalPages;
            ViewData["WorkflowStatuses"] = workflowStatuses;

            return View(pagedResult.Items);
        }

        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> Create(Workflow workflow)
        {
            // Only Admin can create workflows
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to create workflows.";
                return RedirectToAction(nameof(Index));
            }

            workflow.WorkflowName = workflow.WorkflowName?.Trim();
            workflow.WorkflowType = workflow.WorkflowType?.Trim();

            if (string.IsNullOrWhiteSpace(workflow.WorkflowName))
            {
                ModelState.AddModelError("WorkflowName", "Workflow Name is required.");
            }
            else if (workflow.WorkflowName.Length > WorkflowNameMaxLength)
            {
                ModelState.AddModelError("WorkflowName", $"Workflow Name cannot exceed {WorkflowNameMaxLength} characters.");
            }
            else if (await _workflowService.WorkflowNameExistsAsync(workflow.WorkflowName))
            {
                ModelState.AddModelError("WorkflowName", "A workflow with this name already exists.");
            }

            if (string.IsNullOrWhiteSpace(workflow.WorkflowType))
            {
                ModelState.AddModelError("WorkflowType", "Description is required.");
            }
            else if (workflow.WorkflowType.Length > WorkflowTypeMaxLength)
            {
                ModelState.AddModelError("WorkflowType", $"Description cannot exceed {WorkflowTypeMaxLength} characters.");
            }

            if (workflow.RecordLimit <= 0)
            {
                ModelState.AddModelError("RecordLimit", "Record Limit must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                return View(workflow);
            }

            workflow.CreatedBy = "SYSTEM";
            workflow.CreatedDate = DateTime.Now;
            workflow.UpdatedBy = "SYSTEM";
            workflow.UpdatedDate = DateTime.Now;

            try
            {
                var success = await _workflowService.CreateAsync(workflow);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Workflow '{workflow.WorkflowName}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Database update error while creating workflow {WorkflowName}", workflow.WorkflowName);
                ModelState.AddModelError(string.Empty, "Could not save workflow due to a database constraint. Please verify values and try again.");
                return View(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating workflow {WorkflowName}", workflow.WorkflowName);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the workflow.");
                return View(workflow);
            }

            TempData["ErrorMessage"] = "Failed to create workflow. Please try again.";
            return View(workflow);
        }

        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to edit workflows.";
                return RedirectToAction(nameof(Index));
            }

            var workflow = await _workflowService.GetByIdAsync(id);
            if (workflow == null)
            {
                TempData["ErrorMessage"] = "Workflow not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(workflow);
        }

        [HttpPost]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> Edit(int id, Workflow workflow)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to edit workflows.";
                return RedirectToAction(nameof(Index));
            }

            if (id != workflow.WorkflowId)
            {
                TempData["ErrorMessage"] = "Invalid workflow request.";
                return RedirectToAction(nameof(Index));
            }

            var existingWorkflow = await _workflowService.GetByIdAsync(id);
            if (existingWorkflow == null)
            {
                TempData["ErrorMessage"] = "Workflow not found.";
                return RedirectToAction(nameof(Index));
            }

            workflow.WorkflowName = workflow.WorkflowName?.Trim();
            workflow.WorkflowType = workflow.WorkflowType?.Trim();

            if (string.IsNullOrWhiteSpace(workflow.WorkflowName))
            {
                ModelState.AddModelError("WorkflowName", "Workflow Name is required.");
            }
            else if (workflow.WorkflowName.Length > WorkflowNameMaxLength)
            {
                ModelState.AddModelError("WorkflowName", $"Workflow Name cannot exceed {WorkflowNameMaxLength} characters.");
            }
            else if (await _workflowService.WorkflowNameExistsAsync(workflow.WorkflowName, id))
            {
                ModelState.AddModelError("WorkflowName", "A workflow with this name already exists.");
            }

            if (string.IsNullOrWhiteSpace(workflow.WorkflowType))
            {
                ModelState.AddModelError("WorkflowType", "Description is required.");
            }
            else if (workflow.WorkflowType.Length > WorkflowTypeMaxLength)
            {
                ModelState.AddModelError("WorkflowType", $"Description cannot exceed {WorkflowTypeMaxLength} characters.");
            }

            if (workflow.RecordLimit <= 0)
            {
                ModelState.AddModelError("RecordLimit", "Record Limit must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                return View(workflow);
            }

            existingWorkflow.WorkflowName = workflow.WorkflowName?.Trim();
            existingWorkflow.WorkflowType = workflow.WorkflowType?.Trim();
            existingWorkflow.RecordLimit = workflow.RecordLimit;
            existingWorkflow.SubStatusId = workflow.SubStatusId;
            existingWorkflow.UpdatedBy = HttpContext.Session.GetString("Username") ?? "SYSTEM";
            existingWorkflow.UpdatedDate = DateTime.Now;

            try
            {
                var success = await _workflowService.UpdateAsync(existingWorkflow);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Workflow '{existingWorkflow.WorkflowName}' updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = existingWorkflow.WorkflowId });
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency error while editing workflow {WorkflowId}", existingWorkflow.WorkflowId);
                ModelState.AddModelError(string.Empty, "This workflow was changed by another operation. Please refresh and try again.");
                return View(workflow);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Database update error while editing workflow {WorkflowId}", existingWorkflow.WorkflowId);
                ModelState.AddModelError(string.Empty, "Could not update workflow due to a database constraint. Please verify values and try again.");
                return View(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while editing workflow {WorkflowId}", existingWorkflow.WorkflowId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the workflow.");
                return View(workflow);
            }

            TempData["ErrorMessage"] = "Failed to update workflow. Please try again.";
            return View(workflow);
        }

        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public IActionResult ViewWorkFlow(int id)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> Details(int id)
        {
            // Only Admin can view workflow details
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to view workflow details.";
                return RedirectToAction(nameof(Index));
            }

            var workflow = await _workflowService.GetByIdWithStepsAsync(id);

            if (workflow == null)
            {
                TempData["ErrorMessage"] = "Workflow not found.";
                return RedirectToAction(nameof(Index));
            }

            var tasks = await _workflowService.GetTasksByWorkflowIdAsync(id);
            ViewData["WorkflowTasks"] = tasks;

            var activityLogs = await _workflowService.GetActivityLogsByWorkflowIdAsync(id);
            ViewData["ActivityLogs"] = activityLogs;

            return View(workflow);
        }

        [HttpGet]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> ExportWorkflowReport(int id, string format = "csv", DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to export workflow reports.";
                return RedirectToAction(nameof(Index));
            }

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            {
                TempData["ErrorMessage"] = "From Date cannot be later than To Date.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUsername = HttpContext.Session.GetString("Username") ?? "SYSTEM";
            var report = await _workflowReportService.BuildWorkflowExportReportAsync(id, fromDate, toDate, currentUsername);

            if (report == null)
            {
                TempData["ErrorMessage"] = "Workflow not found.";
                return RedirectToAction(nameof(Index));
            }

            var safeWorkflowName = MakeSafeFileName(report.Workflow.WorkflowName ?? $"Workflow_{id}");
            format = (format ?? "csv").Trim().ToLowerInvariant();

            // Clean filenames: "<WorkflowName>_ComplianceReport.(csv|pdf)" (no timestamp suffix)
            if (format == "pdf")
            {
                var pdfBytes = GenerateWorkflowReportPdf(report);
                var pdfFileName = $"{safeWorkflowName}_ComplianceReport.pdf";
                return File(pdfBytes, "application/pdf", pdfFileName);
            }

            // Default to CSV
            var csv = GenerateWorkflowReportCsv(report);
            var csvBytes = Encoding.UTF8.GetBytes(csv);
            var csvFileName = $"{safeWorkflowName}_ComplianceReport.csv";
            return File(csvBytes, "text/csv", csvFileName);
        }

        [HttpGet]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> MonthlyPerformanceReport(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to view performance reports.";
                return RedirectToAction(nameof(Index));
            }

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            {
                TempData["ErrorMessage"] = "From Date cannot be later than To Date.";
                return View(new MonthlyPerformanceReport
                {
                    FromDate = fromDate?.Date,
                    ToDate = toDate?.Date
                });
            }

            var report = await _workflowReportService.BuildMonthlyPerformanceReportAsync(fromDate, toDate);
            return View(report);
        }

        [HttpGet]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> ExportMonthlyPerformanceReport(string format = "csv", DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to export performance reports.";
                return RedirectToAction(nameof(Index));
            }

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            {
                TempData["ErrorMessage"] = "From Date cannot be later than To Date.";
                return RedirectToAction(nameof(MonthlyPerformanceReport), new { fromDate, toDate });
            }

            var report = await _workflowReportService.BuildMonthlyPerformanceReportAsync(fromDate, toDate);
            format = (format ?? "csv").Trim().ToLowerInvariant();

            if (format == "pdf")
            {
                var pdfBytes = GenerateMonthlyPerformanceReportPdf(report);
                return File(pdfBytes, "application/pdf", "Monthly_Performance_Report.pdf");
            }

            var csv = GenerateMonthlyPerformanceReportCsv(report);
            var csvBytes = Encoding.UTF8.GetBytes(csv);
            return File(csvBytes, "text/csv", "Monthly_Performance_Report.csv");
        }

        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> CreateStep(int workflowId)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to create workflow steps.";
                return RedirectToAction(nameof(Index));
            }

            var workflow = await _workflowService.GetByIdAsync(workflowId);
            if (workflow == null)
            {
                TempData["ErrorMessage"] = "Workflow not found.";
                return RedirectToAction(nameof(Index));
            }

            var nextSequence = await _workflowStepService.GetNextSequenceNumberAsync(workflowId);
            ViewData["WorkflowName"] = workflow.WorkflowName;

            return View(new WorkflowStep
            {
                WorkflowId = workflowId,
                Sequence = nextSequence,
                Enabled = "Y"
            });
        }

        [HttpPost]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> CreateStep(WorkflowStep step)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to create workflow steps.";
                return RedirectToAction(nameof(Index));
            }

            var workflow = await _workflowService.GetByIdAsync(step.WorkflowId);
            if (workflow == null)
            {
                TempData["ErrorMessage"] = "Workflow not found.";
                return RedirectToAction(nameof(Index));
            }

            step.StepName = step.StepName?.Trim();
            step.StepType = step.StepType?.Trim();

            if (string.IsNullOrWhiteSpace(step.StepName))
            {
                ModelState.AddModelError("StepName", "Step Name is required.");
            }
            else if (step.StepName.Length > StepNameMaxLength)
            {
                ModelState.AddModelError("StepName", $"Step Name cannot exceed {StepNameMaxLength} characters.");
            }
            else if (await _workflowStepService.StepNameExistsAsync(step.WorkflowId, step.StepName))
            {
                ModelState.AddModelError("StepName", "A step with this name already exists in this workflow.");
            }

            if (!string.IsNullOrWhiteSpace(step.StepDesc) && step.StepDesc.Length > StepDescMaxLength)
            {
                ModelState.AddModelError("StepDesc", $"Step Description cannot exceed {StepDescMaxLength} characters.");
            }

            if (!string.IsNullOrWhiteSpace(step.StepType) && step.StepType.Length > StepTypeMaxLength)
            {
                ModelState.AddModelError("StepType", $"Step Type cannot exceed {StepTypeMaxLength} characters.");
            }

            if (step.Sequence <= 0)
            {
                step.Sequence = await _workflowStepService.GetNextSequenceNumberAsync(step.WorkflowId);
            }

            if (await _workflowStepService.SequenceExistsAsync(step.WorkflowId, step.Sequence))
            {
                ModelState.AddModelError("Sequence", "This sequence number is already in use for this workflow.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["WorkflowName"] = workflow.WorkflowName;
                return View(step);
            }

            step.StepName = step.StepName?.Trim();
            step.StepType = string.IsNullOrWhiteSpace(step.StepType) ? "Manual" : step.StepType.Trim();
            step.Enabled = string.IsNullOrWhiteSpace(step.Enabled) ? "Y" : step.Enabled;
            step.CreatedBy = HttpContext.Session.GetString("Username") ?? "SYSTEM";
            step.CreatedDate = DateTime.Now;
            step.UpdatedBy = step.CreatedBy;
            step.UpdatedDate = DateTime.Now;

            try
            {
                var success = await _workflowStepService.CreateAsync(step);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Step '{step.StepName}' created successfully.";
                    return RedirectToAction(nameof(Details), new { id = step.WorkflowId });
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Database update error while creating workflow step for workflow {WorkflowId}", step.WorkflowId);
                ModelState.AddModelError(string.Empty, "Could not create step due to related data constraints. Please verify values and try again.");
                ViewData["WorkflowName"] = workflow.WorkflowName;
                return View(step);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating workflow step for workflow {WorkflowId}", step.WorkflowId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the workflow step.");
                ViewData["WorkflowName"] = workflow.WorkflowName;
                return View(step);
            }

            TempData["ErrorMessage"] = "Failed to create workflow step. Please try again.";
            ViewData["WorkflowName"] = workflow.WorkflowName;
            return View(step);
        }

        [HttpGet]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> EditStep(int workflowStepId)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to edit workflow steps.";
                return RedirectToAction(nameof(Index));
            }

            var step = await _workflowStepService.GetByIdAsync(workflowStepId);
            if (step == null)
            {
                TempData["ErrorMessage"] = "Workflow step not found.";
                return RedirectToAction(nameof(Index));
            }

            var workflow = await _workflowService.GetByIdAsync(step.WorkflowId);
            ViewData["WorkflowName"] = workflow?.WorkflowName ?? "Workflow";

            return View(step);
        }

        [HttpPost]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> EditStep(WorkflowStep step, string? returnUrl)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to edit workflow steps.";
                return RedirectToAction(nameof(Index));
            }

            var existingStep = await _workflowStepService.GetByIdAsync(step.WorkflowStepId);
            if (existingStep == null)
            {
                TempData["ErrorMessage"] = "Workflow step not found.";
                return RedirectToAction(nameof(Index));
            }

            if (step.WorkflowId <= 0)
            {
                step.WorkflowId = existingStep.WorkflowId;
            }

            step.StepName = step.StepName?.Trim();
            step.StepType = step.StepType?.Trim();

            if (string.IsNullOrWhiteSpace(step.StepName))
            {
                ModelState.AddModelError("StepName", "Step Name is required.");
            }
            else if (step.StepName.Length > StepNameMaxLength)
            {
                ModelState.AddModelError("StepName", $"Step Name cannot exceed {StepNameMaxLength} characters.");
            }
            else if (await _workflowStepService.StepNameExistsAsync(step.WorkflowId, step.StepName, step.WorkflowStepId))
            {
                ModelState.AddModelError("StepName", "A step with this name already exists in this workflow.");
            }

            if (!string.IsNullOrWhiteSpace(step.StepDesc) && step.StepDesc.Length > StepDescMaxLength)
            {
                ModelState.AddModelError("StepDesc", $"Step Description cannot exceed {StepDescMaxLength} characters.");
            }

            if (!string.IsNullOrWhiteSpace(step.StepType) && step.StepType.Length > StepTypeMaxLength)
            {
                ModelState.AddModelError("StepType", $"Step Type cannot exceed {StepTypeMaxLength} characters.");
            }

            if (step.Sequence < 0)
            {
                ModelState.AddModelError("Sequence", "Sequence cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    TempData["ErrorMessage"] = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                        ?? "Unable to update workflow step. Please review your inputs.";

                    return Redirect(returnUrl);
                }

                var workflowForView = await _workflowService.GetByIdAsync(step.WorkflowId);
                ViewData["WorkflowName"] = workflowForView?.WorkflowName ?? "Workflow";
                return View(step);
            }

            var currentUsername = HttpContext.Session.GetString("Username") ?? "SYSTEM";
            var result = await _workflowStepService.UpdateWithReorderAsync(step, currentUsername);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(nameof(Details), new { id = step.WorkflowId });
            }

            TempData["ErrorMessage"] = result.Message;
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var workflow = await _workflowService.GetByIdAsync(step.WorkflowId);
            ViewData["WorkflowName"] = workflow?.WorkflowName ?? "Workflow";
            return View(step);
        }

        [HttpPost]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> DeleteStep(int workflowStepId, int workflowId)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to delete workflow steps.";
                return RedirectToAction(nameof(Index));
            }

            var currentUsername = HttpContext.Session.GetString("Username") ?? "SYSTEM";
            var result = await _workflowStepService.DeleteWithDependenciesAsync(workflowStepId, currentUsername);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id = workflowId });
        }

        [HttpPost]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> ReassignTask(int taskId, int workflowId, string assignedTo)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to reassign tasks.";
                return RedirectToAction(nameof(Index));
            }

            var currentUsername = HttpContext.Session.GetString("Username") ?? "SYSTEM";
            var result = await _workflowService.ReassignTaskAsync(taskId, workflowId, assignedTo, currentUsername);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id = workflowId });
        }

        [HttpPost]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> UnassignTask(int taskId, int workflowId)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to unassign tasks.";
                return RedirectToAction(nameof(Index));
            }

            var currentUsername = HttpContext.Session.GetString("Username") ?? "SYSTEM";
            var result = await _workflowService.UnassignTaskAsync(taskId, workflowId, currentUsername);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id = workflowId });
        }

        [HttpPost]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> CompleteTask(int taskId, int workflowId)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to complete tasks.";
                return RedirectToAction(nameof(Index));
            }

            var currentUsername = HttpContext.Session.GetString("Username") ?? "SYSTEM";
            var result = await _workflowService.CompleteTaskAsync(taskId, workflowId, currentUsername);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id = workflowId });
        }

        // Login/Logout actions (team's manual authentication system)
        // Note: This coexists with ?admin=true via AdminAuthorizationFilter
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in via session, skip to Index
            if (HttpContext.Session.GetString("LoggedIn") == "true")
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _userService.ValidateUser(username, password);

            if (user != null)
            {
                HttpContext.Session.SetString("LoggedIn", "true");
                HttpContext.Session.SetString("Username", user.Username ?? "");
                HttpContext.Session.SetString("Role", user.UserRole ?? "User");

                return RedirectToAction("Index");
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private static string MakeSafeFileName(string input)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(input.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "Workflow" : cleaned;
        }

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
        }

        private static string FormatDate(DateTime? dt)
        {
            return dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
        }

        private string GenerateWorkflowReportCsv(WorkflowExportReport report)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Workflow Compliance Export Report");
            sb.AppendLine($"Workflow ID,{report.Workflow.WorkflowId}");
            sb.AppendLine($"Workflow Name,{CsvEscape(report.Workflow.WorkflowName)}");
            sb.AppendLine($"Workflow Type,{CsvEscape(report.Workflow.WorkflowType)}");
            sb.AppendLine($"Generated By,{CsvEscape(report.GeneratedBy)}");
            sb.AppendLine($"Generated At,{report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"From Date,{(report.FromDate.HasValue ? report.FromDate.Value.ToString("yyyy-MM-dd") : "")}");
            sb.AppendLine($"To Date,{(report.ToDate.HasValue ? report.ToDate.Value.ToString("yyyy-MM-dd") : "")}");
            sb.AppendLine($"Total Rows,{report.Rows.Count}");
            sb.AppendLine();

            sb.AppendLine("TaskId,WorkflowStepId,WorkflowStepName,Status,SubStatus,AssignedTo,AssignedBy,CompletedBy,CreatedDate,AssignedDate,CompletedDate,UpdatedDate");

            foreach (var row in report.Rows)
            {
                sb.AppendLine(string.Join(",",
                    row.TaskId.ToString(CultureInfo.InvariantCulture),
                    row.WorkflowStepId.ToString(CultureInfo.InvariantCulture),
                    CsvEscape(row.WorkflowStepName),
                    CsvEscape(row.StatusName),
                    CsvEscape(row.SubStatusName),
                    CsvEscape(row.AssignedTo),
                    CsvEscape(row.AssignedBy),
                    CsvEscape(row.CompletedBy),
                    CsvEscape(FormatDate(row.CreatedDate)),
                    CsvEscape(FormatDate(row.AssignedDate)),
                    CsvEscape(FormatDate(row.CompletedDate)),
                    CsvEscape(FormatDate(row.UpdatedDate))
                ));
            }

            return sb.ToString();
        }

        private byte[] GenerateWorkflowReportPdf(WorkflowExportReport report)
        {
            // Fix QuestPDF runtime license exception
            QuestPDF.Settings.License = LicenseType.Community;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(24);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Workflow Compliance Export Report").Bold().FontSize(16);
                        col.Item().Text($"Workflow: {report.Workflow.WorkflowName} (ID: {report.Workflow.WorkflowId})");
                        col.Item().Text($"Type: {report.Workflow.WorkflowType ?? "-"}");
                        col.Item().Text($"Generated By: {report.GeneratedBy}    Generated At: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
                        col.Item().Text($"Date Filter: {(report.FromDate.HasValue ? report.FromDate.Value.ToString("yyyy-MM-dd") : "Any")} to {(report.ToDate.HasValue ? report.ToDate.Value.ToString("yyyy-MM-dd") : "Any")}");
                        col.Item().Text($"Total Rows: {report.Rows.Count}");
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(42);
                            columns.ConstantColumn(55);
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.7f);
                            columns.RelativeColumn(1.7f);
                            columns.RelativeColumn(1.7f);
                            columns.RelativeColumn(1.7f);
                        });

                        table.Header(header =>
                        {
                            void HeaderCell(string text) =>
                                header.Cell().Element(CellStyleHeader).Text(text);

                            HeaderCell("TaskId");
                            HeaderCell("StepId");
                            HeaderCell("Step");
                            HeaderCell("Status");
                            HeaderCell("SubStatus");
                            HeaderCell("Assigned To");
                            HeaderCell("Assigned By");
                            HeaderCell("Completed By");
                            HeaderCell("Created");
                            HeaderCell("Assigned");
                            HeaderCell("Completed");
                            HeaderCell("Updated");
                        });

                        foreach (var row in report.Rows)
                        {
                            table.Cell().Element(CellStyle).Text(row.TaskId.ToString());
                            table.Cell().Element(CellStyle).Text(row.WorkflowStepId.ToString());
                            table.Cell().Element(CellStyle).Text(row.WorkflowStepName);
                            table.Cell().Element(CellStyle).Text(row.StatusName);
                            table.Cell().Element(CellStyle).Text(row.SubStatusName);
                            table.Cell().Element(CellStyle).Text(row.AssignedTo);
                            table.Cell().Element(CellStyle).Text(row.AssignedBy);
                            table.Cell().Element(CellStyle).Text(row.CompletedBy);
                            table.Cell().Element(CellStyle).Text(FormatDate(row.CreatedDate));
                            table.Cell().Element(CellStyle).Text(FormatDate(row.AssignedDate));
                            table.Cell().Element(CellStyle).Text(FormatDate(row.CompletedDate));
                            table.Cell().Element(CellStyle).Text(FormatDate(row.UpdatedDate));
                        }

                        static IContainer CellStyle(IContainer container)
                        {
                            return container
                                .BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(4)
                                .PaddingHorizontal(3);
                        }

                        static IContainer CellStyleHeader(IContainer container)
                        {
                            return container
                                .Background(Colors.Grey.Lighten3)
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Medium)
                                .PaddingVertical(5)
                                .PaddingHorizontal(3);
                        }
                    });

                    page.Footer()
                        .AlignRight()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return doc.GeneratePdf();
        }

        private string GenerateMonthlyPerformanceReportCsv(MonthlyPerformanceReport report)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Monthly Performance Report");
            sb.AppendLine($"From Date,{(report.FromDate.HasValue ? report.FromDate.Value.ToString("yyyy-MM-dd") : "")}");
            sb.AppendLine($"To Date,{(report.ToDate.HasValue ? report.ToDate.Value.ToString("yyyy-MM-dd") : "")}");
            sb.AppendLine($"Total Workflows Created,{report.TotalWorkflowsCreated}");
            sb.AppendLine($"Total Workflows Completed,{report.TotalWorkflowsCompleted}");
            sb.AppendLine($"Total Workflows Pending,{report.TotalWorkflowsPending}");
            sb.AppendLine($"Average Completion Days,{report.AverageCompletionDays.ToString(CultureInfo.InvariantCulture)}");

            return sb.ToString();
        }

        private byte[] GenerateMonthlyPerformanceReportPdf(MonthlyPerformanceReport report)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(24);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Monthly Performance Report").Bold().FontSize(18);
                        col.Item().Text($"Date Filter: {(report.FromDate.HasValue ? report.FromDate.Value.ToString("yyyy-MM-dd") : "Any")} to {(report.ToDate.HasValue ? report.ToDate.Value.ToString("yyyy-MM-dd") : "Any")}");
                        col.Item().Text($"Generated At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    });

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Cell().Element(CellHeader).Text("Metric");
                        table.Cell().Element(CellHeader).Text("Value");

                        table.Cell().Element(CellBody).Text("Total Workflows Created");
                        table.Cell().Element(CellBody).Text(report.TotalWorkflowsCreated.ToString(CultureInfo.InvariantCulture));

                        table.Cell().Element(CellBody).Text("Total Workflows Completed");
                        table.Cell().Element(CellBody).Text(report.TotalWorkflowsCompleted.ToString(CultureInfo.InvariantCulture));

                        table.Cell().Element(CellBody).Text("Total Workflows Pending");
                        table.Cell().Element(CellBody).Text(report.TotalWorkflowsPending.ToString(CultureInfo.InvariantCulture));

                        table.Cell().Element(CellBody).Text("Average Completion Days");
                        table.Cell().Element(CellBody).Text(report.AverageCompletionDays.ToString(CultureInfo.InvariantCulture));

                        static IContainer CellHeader(IContainer container)
                        {
                            return container
                                .Background(Colors.Grey.Lighten3)
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Medium)
                                .Padding(6)
                                .DefaultTextStyle(x => x.SemiBold());
                        }

                        static IContainer CellBody(IContainer container)
                        {
                            return container
                                .BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(6);
                        }
                    });
                });
            });

            return doc.GeneratePdf();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        [ServiceFilter(typeof(AdminAuthorizationFilter))]
        public async Task<IActionResult> WorkloadDashboard(string? selectedUserName)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to view the workload dashboard.";
                return RedirectToAction(nameof(Index));
            }

            var workloadData = await _workflowService.GetActiveTaskCountsPerUserAsync();
            var users = _userService.GetAllUsers();

            var model = new WorkloadDashboardViewModel
            {
                Users = users.Select(u => new WorkloadUserViewModel
                {
                    UserName = u.Username ?? "",
                    TaskCount = workloadData.ContainsKey(u.Username ?? "")
                        ? workloadData[u.Username ?? ""]
                        : 0
                }).ToList(),
                SelectedUserName = selectedUserName
            };

            if (!string.IsNullOrWhiteSpace(selectedUserName))
            {
                var activeTasks = await _workflowService.GetActiveTasksAssignedToUserAsync(selectedUserName);

                model.SelectedUserTasks = activeTasks.Select(t => new MyTaskRowViewModel
                {
                    TaskId = t.TaskId,
                    WorkflowId = t.WorkflowStep.WorkflowId,
                    WorkflowName = t.WorkflowStep.Workflow?.WorkflowName ?? "Unknown Workflow",
                    StepName = t.WorkflowStep.StepName ?? "Unknown Step",
                    StatusName = t.Status?.StatusName ?? "Unknown Status",
                    ItemKey = t.Item?.ItemKey ?? $"Item #{t.ItemId}",
                    UpdatedDate = t.UpdatedDate
                }).ToList();
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MyTasks()
        {
            if (HttpContext.Session.GetString("LoggedIn") != "true")
            {
                return RedirectToAction("Login");
            }

            var role = HttpContext.Session.GetString("Role");
            if (role == "Admin")
            {
                return RedirectToAction("Index");
            }

            var username = HttpContext.Session.GetString("Username") ?? "";

            var activeTasks = await _workflowService.GetActiveTasksAssignedToUserAsync(username);

            var model = new MyTasksViewModel
            {
                Tasks = activeTasks.Select(t => new MyTaskRowViewModel
                {
                    TaskId = t.TaskId,
                    WorkflowId = t.WorkflowStep.WorkflowId,
                    WorkflowName = t.WorkflowStep.Workflow?.WorkflowName ?? "Unknown Workflow",
                    StepName = t.WorkflowStep.StepName ?? "Unknown Step",
                    StatusName = t.Status?.StatusName ?? "Unknown Status",
                    ItemKey = t.Item?.ItemKey ?? $"Item #{t.ItemId}",
                    UpdatedDate = t.UpdatedDate
                }).ToList()
            };

            return View(model);
        }
    }
}
