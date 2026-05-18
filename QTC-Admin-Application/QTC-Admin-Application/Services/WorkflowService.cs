using Microsoft.EntityFrameworkCore;
using QTC_Admin_Application.Models;

namespace QTC_Admin_Application.Services;

/// <summary>
/// Workflow data access service (EF Core).
/// NOTE: This project previously had an ADO.NET implementation merged in.
/// This file is the cleaned EF Core version to restore build + runtime stability.
/// </summary>
public class WorkflowService
{
    private readonly WorkflowContext _context;

    public WorkflowService(WorkflowContext context)
    {
        _context = context;
    }

    public async Task<List<Workflow>> GetAllAsync()
    {
        return await _context.Workflows
            .OrderBy(w => w.WorkflowName)
            .ToListAsync();
    }

    public async Task<Workflow?> GetByIdAsync(int workflowId)
    {
        return await _context.Workflows.FindAsync(workflowId);
    }

    public async Task<Workflow?> GetByIdWithStepsAsync(int workflowId)
    {
        // Ensure steps are ordered by Sequence when included.
        // EF Core doesn't guarantee ordering on Include by default, so we apply ordering in the projection.
        return await _context.Workflows
            .Include(w => w.WorkflowSteps)
            .Where(w => w.WorkflowId == workflowId)
            .Select(w => new Workflow
            {
                WorkflowId = w.WorkflowId,
                WorkflowName = w.WorkflowName,
                WorkflowType = w.WorkflowType,
                RecordLimit = w.RecordLimit,
                SubStatusId = w.SubStatusId,
                CreatedBy = w.CreatedBy,
                CreatedDate = w.CreatedDate,
                UpdatedBy = w.UpdatedBy,
                UpdatedDate = w.UpdatedDate,
                WorkflowSteps = w.WorkflowSteps
                    .OrderBy(s => s.Sequence)
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CreateAsync(Workflow workflow)
    {
        _context.Workflows.Add(workflow);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> UpdateAsync(Workflow workflow)
    {
        _context.Workflows.Update(workflow);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> DeleteAsync(int workflowId)
    {
        var workflow = await GetByIdAsync(workflowId);
        if (workflow == null) return false;

        _context.Workflows.Remove(workflow);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> WorkflowNameExistsAsync(string workflowName, int? excludeWorkflowId = null)
    {
        var query = _context.Workflows.Where(w => w.WorkflowName == workflowName);

        if (excludeWorkflowId.HasValue)
        {
            query = query.Where(w => w.WorkflowId != excludeWorkflowId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<List<QTC_Admin_Application.Models.Task>> GetTasksByWorkflowIdAsync(int workflowId)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Include(t => t.Status)
            .Include(t => t.WorkflowStep)
            .Include(t => t.Item) // Add Item inclusion to show ItemKey
            .Where(t => t.WorkflowStep.WorkflowId == workflowId)
            // Order by: Pending (1), In Progress (2), then others, then most recently updated first
            .OrderBy(t => t.StatusId == 1 ? 0 : t.StatusId == 2 ? 1 : 2)
            .ThenByDescending(t => t.UpdatedDate)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetActivityLogsByWorkflowIdAsync(int workflowId)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .Include(a => a.WorkflowStep)
            .Where(a => a.WorkflowId == workflowId)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> ReassignTaskAsync(int taskId, int workflowId, string assignedTo, string updatedBy)
    {
        var task = await _context.Tasks
            .Include(t => t.WorkflowStep)
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.WorkflowStep.WorkflowId == workflowId);

        if (task == null)
        {
            return (false, "Task not found.");
        }

        if (string.IsNullOrWhiteSpace(assignedTo))
        {
            return (false, "Assigned To is required.");
        }

        task.AssignedTo = assignedTo.Trim();
        task.AssignedBy = updatedBy;
        task.AssignedDate = DateTime.Now;
        task.UpdatedBy = updatedBy;
        task.UpdatedDate = DateTime.Now;
        task.StatusId = 2; // ASSIGNED

        AddActivityLog(task, task.WorkflowStep.WorkflowId, updatedBy, "Assign", "AdminReassign");

        try
        {
            var result = await _context.SaveChangesAsync();
            return result > 0
                ? (true, "Task reassigned successfully.")
                : (false, "No task changes were saved.");
        }
        catch (DbUpdateException)
        {
            return (false, "Unable to reassign task due to data constraints.");
        }
    }

    public async Task<(bool Success, string Message)> UnassignTaskAsync(int taskId, int workflowId, string updatedBy)
    {
        var task = await _context.Tasks
            .Include(t => t.WorkflowStep)
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.WorkflowStep.WorkflowId == workflowId);

        if (task == null)
        {
            return (false, "Task not found.");
        }

        task.AssignedTo = null;
        task.AssignedBy = null;
        task.AssignedDate = null;
        task.UpdatedBy = updatedBy;
        task.UpdatedDate = DateTime.Now;
        task.StatusId = 1; // NEW

        AddActivityLog(task, task.WorkflowStep.WorkflowId, updatedBy, "Unassign", "AdminUnassign");

        try
        {
            var result = await _context.SaveChangesAsync();
            return result > 0
                ? (true, "Task unassigned successfully.")
                : (false, "No task changes were saved.");
        }
        catch (DbUpdateException)
        {
            return (false, "Unable to unassign task due to data constraints.");
        }
    }

    public async Task<(bool Success, string Message)> CompleteTaskAsync(int taskId, int workflowId, string updatedBy)
    {
        var task = await _context.Tasks
            .Include(t => t.WorkflowStep)
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.WorkflowStep.WorkflowId == workflowId);

        if (task == null)
        {
            return (false, "Task not found.");
        }

        if (task.StatusId == 3)
        {
            return (true, "Task is already completed.");
        }

        var currentStep = task.WorkflowStep;
        if (currentStep == null)
        {
            return (false, "Unable to resolve current workflow step for this task.");
        }

        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            var now = DateTime.Now;
            task.StatusId = 3; // COMPLETED
            task.CompletedBy = updatedBy;
            task.CompletedDate = now;
            task.UpdatedBy = updatedBy;
            task.UpdatedDate = now;

            AddActivityLog(task, currentStep.WorkflowId, updatedBy, "Complete", "AdminCompleteTask");

            // Improvement #3: Only progression to NEXT *ENABLED* step
            var nextStep = await _context.WorkflowSteps
                .Where(s => s.WorkflowId == currentStep.WorkflowId 
                         && s.Sequence > currentStep.Sequence 
                         && s.Enabled == "Y")
                .OrderBy(s => s.Sequence)
                .FirstOrDefaultAsync();

            var shouldCreateNextTask = nextStep != null && string.IsNullOrWhiteSpace(currentStep.ActionUrl);

            if (shouldCreateNextTask && nextStep != null)
            {
                var newTask = new QTC_Admin_Application.Models.Task
                {
                    ItemId = task.ItemId,
                    StatusId = 1, // NEW
                    SubStatusId = null,
                    WorkflowStepId = nextStep.WorkflowStepId,
                    IsPaused = task.IsPaused,
                    CreatedBy = updatedBy,
                    CreatedDate = now,
                    UpdatedBy = updatedBy,
                    UpdatedDate = now
                };

                _context.Tasks.Add(newTask);

                var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemId == task.ItemId);
                if (item != null)
                {
                    item.CurrentWorkflowStepId = nextStep.WorkflowStepId;
                    item.UpdatedBy = updatedBy;
                    item.UpdatedDate = now;
                }
            }
            else if (nextStep == null)
            {
                // Improvement #4: Final step completed -> Mark Item as Completed
                var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemId == task.ItemId);
                if (item != null)
                {
                    // ItemStatusId = 3 usually maps to Completed in schema
                    item.ItemStatusId = 3; 
                    item.UpdatedBy = updatedBy;
                    item.UpdatedDate = now;
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return shouldCreateNextTask
                ? (true, "Task completed and advanced to the next workflow step.")
                : (true, "Task completed successfully.");
        }
        catch (DbUpdateException)
        {
            return (false, "Unable to complete task due to database constraints.");
        }
    }

    private void AddActivityLog(QTC_Admin_Application.Models.Task task, int workflowId, string user, string action, string operation)
    {
        var now = DateTime.Now;

        _context.ActivityLogs.Add(new ActivityLog
        {
            TaskId = task.TaskId,
            WorkflowId = workflowId,
            WorkflowStepId = task.WorkflowStepId,
            StatusId = task.StatusId,
            ActivityUser = user,
            Action = action,
            Operation = operation,
            CreatedBy = user,
            CreatedDate = now,
            UpdatedBy = user,
            UpdatedDate = now
        });
    }

    public async Task<Dictionary<int, string>> GetWorkflowStatusesAsync(List<int> workflowIds)
    {
        if (workflowIds == null || workflowIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        // This translates to a fast SQL GROUP BY with MIN() and MAX() aggregations
        // Only one row per WorkflowId is returned from the database to your web server.
        var workflowAggregates = await _context.Tasks
            .Where(t => workflowIds.Contains(t.WorkflowStep.WorkflowId))
            .GroupBy(t => t.WorkflowStep.WorkflowId)
            .Select(g => new
            {
                WorkflowId = g.Key,
                MinStatus = g.Min(t => t.StatusId),
                MaxStatus = g.Max(t => t.StatusId)
            })
            .ToDictionaryAsync(x => x.WorkflowId, x => x);

        var result = new Dictionary<int, string>();

        foreach (var workflowId in workflowIds)
        {
            // If the workflow isn't in our grouped results, it has no tasks yet
            if (!workflowAggregates.TryGetValue(workflowId, out var aggregate))
            {
                result[workflowId] = "New";
                continue;
            }

            // Evaluate the SQL boundaries we calculated
            if (aggregate.MinStatus == 1 && aggregate.MaxStatus == 1)
            {
                result[workflowId] = "New";
            }
            else if (aggregate.MinStatus == 3 && aggregate.MaxStatus == 3)
            {
                result[workflowId] = "Completed";
            }
            else
            {
                result[workflowId] = "In Progress";
            }
        }

        return result;
    }

    public async Task<(List<Workflow> Items, int TotalCount)> SearchPagedAsync(
        string? searchTerm = null,
        string? sortBy = null,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var query = BuildSearchQuery(searchTerm, sortBy);
        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Search workflows by name/type and sort results.
    /// sortBy options used by UI:
    /// - "name" / "name_asc" (default)
    /// - "name_desc"
    /// - "date" (CreatedDate ascending)
    /// - "date_desc"
    /// </summary>
    public async Task<List<Workflow>> SearchAsync(string? searchTerm = null, string? sortBy = null)
    {
        return await BuildSearchQuery(searchTerm, sortBy).ToListAsync();
    }

    private IQueryable<Workflow> BuildSearchQuery(string? searchTerm, string? sortBy)
    {
        var query = _context.Workflows.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            // Requirement #2: search/filter by Name
            query = query.Where(w => (w.WorkflowName ?? "").Contains(term));
        }

        query = (sortBy ?? "").ToLowerInvariant() switch
        {
            "name_desc" or "name_z-a" => query.OrderByDescending(w => w.WorkflowName),
            "date" => query.OrderBy(w => w.CreatedDate),
            "date_desc" => query.OrderByDescending(w => w.CreatedDate),
            _ => query.OrderBy(w => w.WorkflowName)
        };

        return query;
    }

    public async Task<Dictionary<string, int>> GetActiveTaskCountsPerUserAsync()
    {
        return await _context.Tasks
            // Only count active tasks (Pending = 1, In Progress = 2)
            .Where(t => !string.IsNullOrEmpty(t.AssignedTo) && (t.StatusId == 1 || t.StatusId == 2))
            .GroupBy(t => t.AssignedTo)
            .Select(g => new
            {
                User = g.Key!,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.User, x => x.Count);
    }

    public async Task<List<QTC_Admin_Application.Models.Task>> GetActiveTasksAssignedToUserAsync(string username)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Include(t => t.Status)
            .Include(t => t.Item)
            .Include(t => t.WorkflowStep)
                .ThenInclude(ws => ws.Workflow) // Necessary to display the parent Workflow name
            .Where(t => t.AssignedTo == username && (t.StatusId == 1 || t.StatusId == 2))
            .OrderBy(t => t.StatusId)
            .ThenByDescending(t => t.UpdatedDate)
            .ToListAsync();
    }
}
