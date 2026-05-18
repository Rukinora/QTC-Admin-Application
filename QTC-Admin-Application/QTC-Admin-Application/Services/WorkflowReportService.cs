using Microsoft.EntityFrameworkCore;
using QTC_Admin_Application.Models;

namespace QTC_Admin_Application.Services
{
    public class WorkflowReportService
    {
        private readonly WorkflowContext _context;

        public WorkflowReportService(WorkflowContext context)
        {
            _context = context;
        }

        public async Task<WorkflowExportReport?> BuildWorkflowExportReportAsync(
            int workflowId,
            DateTime? fromDate,
            DateTime? toDate,
            string generatedBy)
        {
            var workflow = await _context.Workflows
                .FirstOrDefaultAsync(w => w.WorkflowId == workflowId);

            if (workflow == null)
                return null;

            // End date inclusive (through end of selected day)
            DateTime? toDateInclusive = toDate?.Date.AddDays(1).AddTicks(-1);

            var query = _context.Tasks
                .AsNoTracking()
                .Include(t => t.Status)
                .Include(t => t.SubStatus)
                .Include(t => t.WorkflowStep)
                .Where(t => t.WorkflowStep.WorkflowId == workflowId);

            // Date filtering: if either date is provided, include tasks where ANY key timestamp is in range
            if (fromDate.HasValue || toDateInclusive.HasValue)
            {
                var from = fromDate?.Date ?? DateTime.MinValue;
                var to = toDateInclusive ?? DateTime.MaxValue;

                query = query.Where(t =>
                    (t.CreatedDate >= from && t.CreatedDate <= to) ||
                    (t.UpdatedDate >= from && t.UpdatedDate <= to) ||
                    (t.AssignedDate.HasValue && t.AssignedDate.Value >= from && t.AssignedDate.Value <= to) ||
                    (t.CompletedDate.HasValue && t.CompletedDate.Value >= from && t.CompletedDate.Value <= to));
            }

            var rows = await query
                .OrderBy(t => t.CreatedDate)
                .Select(t => new WorkflowExportRow
                {
                    TaskId = t.TaskId,
                    WorkflowStepId = t.WorkflowStepId,
                    WorkflowStepName = t.WorkflowStep.StepName ?? "",
                    StatusName = t.Status.StatusName ?? "",
                    SubStatusName = t.SubStatus != null ? (t.SubStatus.SubStatusName ?? "") : "",
                    AssignedTo = t.AssignedTo ?? "",
                    AssignedBy = t.AssignedBy ?? "",
                    CompletedBy = t.CompletedBy ?? "",
                    CreatedDate = t.CreatedDate,
                    AssignedDate = t.AssignedDate,
                    CompletedDate = t.CompletedDate,
                    UpdatedDate = t.UpdatedDate
                })
                .ToListAsync();

            return new WorkflowExportReport
            {
                Workflow = workflow,
                FromDate = fromDate?.Date,
                ToDate = toDate?.Date,
                Rows = rows,
                GeneratedAt = DateTime.Now,
                GeneratedBy = generatedBy
            };
        }

        public async Task<MonthlyPerformanceReport> BuildMonthlyPerformanceReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            DateTime? from = fromDate?.Date;
            DateTime? toInclusive = toDate?.Date.AddDays(1).AddTicks(-1);

            var workflowQuery = _context.Workflows.AsNoTracking().AsQueryable();

            if (from.HasValue)
            {
                workflowQuery = workflowQuery.Where(w => w.CreatedDate >= from.Value);
            }

            if (toInclusive.HasValue)
            {
                workflowQuery = workflowQuery.Where(w => w.CreatedDate <= toInclusive.Value);
            }

            var workflows = await workflowQuery
                .Select(w => new
                {
                    w.WorkflowId,
                    w.CreatedDate
                })
                .ToListAsync();

            var workflowIds = workflows.Select(w => w.WorkflowId).ToList();

            var taskData = await _context.Tasks
                .AsNoTracking()
                .Where(t => workflowIds.Contains(t.WorkflowStep.WorkflowId))
                .Select(t => new
                {
                    WorkflowId = t.WorkflowStep.WorkflowId,
                    t.StatusId,
                    t.CompletedDate
                })
                .ToListAsync();

            var tasksByWorkflow = taskData
                .GroupBy(t => t.WorkflowId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var totalCreated = workflows.Count;
            var totalCompleted = 0;
            var totalPending = 0;
            var completionDays = new List<double>();

            foreach (var workflow in workflows)
            {
                if (!tasksByWorkflow.TryGetValue(workflow.WorkflowId, out var workflowTasks) || workflowTasks.Count == 0)
                {
                    totalPending++;
                    continue;
                }

                var isComplete = workflowTasks.All(t => t.StatusId == 5);

                if (isComplete)
                {
                    totalCompleted++;

                    var lastCompleted = workflowTasks
                        .Where(t => t.CompletedDate.HasValue)
                        .Select(t => t.CompletedDate!.Value)
                        .DefaultIfEmpty(workflow.CreatedDate)
                        .Max();

                    completionDays.Add((lastCompleted - workflow.CreatedDate).TotalDays);
                }
                else
                {
                    totalPending++;
                }
            }

            return new MonthlyPerformanceReport
            {
                FromDate = fromDate?.Date,
                ToDate = toDate?.Date,
                TotalWorkflowsCreated = totalCreated,
                TotalWorkflowsCompleted = totalCompleted,
                TotalWorkflowsPending = totalPending,
                AverageCompletionDays = completionDays.Count == 0 ? 0 : Math.Round(completionDays.Average(), 2)
            };
        }
    }
}