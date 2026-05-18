using Microsoft.EntityFrameworkCore;
using QTC_Admin_Application.Models;

namespace QTC_Admin_Application.Services;

public class WorkflowStepService
{
    private readonly WorkflowContext _context;

    public WorkflowStepService(WorkflowContext context)
    {
        _context = context;
    }

    public async Task<List<WorkflowStep>> GetStepsByWorkflowIdAsync(int workflowId)
    {
        return await _context.WorkflowSteps
            .Where(s => s.WorkflowId == workflowId)
            .OrderBy(s => s.Sequence)
            .ToListAsync();
    }

    public async Task<WorkflowStep?> GetByIdAsync(int stepId)
    {
        return await _context.WorkflowSteps.FindAsync(stepId);
    }

    public async Task<bool> CreateAsync(WorkflowStep step)
    {
        _context.WorkflowSteps.Add(step);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> UpdateAsync(WorkflowStep step)
    {
        _context.WorkflowSteps.Update(step);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> DeleteAsync(int stepId)
    {
        var step = await GetByIdAsync(stepId);
        if (step == null) return false;

        _context.WorkflowSteps.Remove(step);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> StepNameExistsAsync(int workflowId, string stepName, int? excludeStepId = null)
    {
        var query = _context.WorkflowSteps
            .Where(s => s.WorkflowId == workflowId && s.StepName == stepName);

        if (excludeStepId.HasValue)
        {
            query = query.Where(s => s.WorkflowStepId != excludeStepId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> SequenceExistsAsync(int workflowId, int sequence, int? excludeStepId = null)
    {
        var query = _context.WorkflowSteps
            .Where(s => s.WorkflowId == workflowId && s.Sequence == sequence);

        if (excludeStepId.HasValue)
        {
            query = query.Where(s => s.WorkflowStepId != excludeStepId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<int> GetNextSequenceNumberAsync(int workflowId)
    {
        var maxSequence = await _context.WorkflowSteps
            .Where(s => s.WorkflowId == workflowId)
            .MaxAsync(s => (int?)s.Sequence);

        return (maxSequence ?? 0) + 1;
    }

    public async Task<(bool Success, string Message)> UpdateWithReorderAsync(WorkflowStep updatedStep, string username)
    {
        var existingStep = await _context.WorkflowSteps
            .FirstOrDefaultAsync(s => s.WorkflowStepId == updatedStep.WorkflowStepId);

        if (existingStep == null)
        {
            return (false, "Workflow step not found.");
        }

        if (existingStep.WorkflowId != updatedStep.WorkflowId)
        {
            return (false, "Invalid workflow step update request.");
        }

        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            var maxOtherSequence = await _context.WorkflowSteps
                .Where(s => s.WorkflowId == existingStep.WorkflowId && s.WorkflowStepId != existingStep.WorkflowStepId)
                .MaxAsync(s => (int?)s.Sequence) ?? 0;

            var maxAllowedSequence = maxOtherSequence + 1;
            var targetSequence = updatedStep.Sequence <= 0 ? existingStep.Sequence : updatedStep.Sequence;
            targetSequence = Math.Max(1, Math.Min(targetSequence, maxAllowedSequence));

            var currentSequence = existingStep.Sequence;

            if (targetSequence < currentSequence)
            {
                var stepsToShift = await _context.WorkflowSteps
                    .Where(s => s.WorkflowId == existingStep.WorkflowId
                                && s.WorkflowStepId != existingStep.WorkflowStepId
                                && s.Sequence >= targetSequence
                                && s.Sequence < currentSequence)
                    .ToListAsync();

                foreach (var step in stepsToShift)
                {
                    step.Sequence += 1;
                    step.UpdatedBy = username;
                    step.UpdatedDate = DateTime.Now;
                }
            }
            else if (targetSequence > currentSequence)
            {
                var stepsToShift = await _context.WorkflowSteps
                    .Where(s => s.WorkflowId == existingStep.WorkflowId
                                && s.WorkflowStepId != existingStep.WorkflowStepId
                                && s.Sequence > currentSequence
                                && s.Sequence <= targetSequence)
                    .ToListAsync();

                foreach (var step in stepsToShift)
                {
                    step.Sequence -= 1;
                    step.UpdatedBy = username;
                    step.UpdatedDate = DateTime.Now;
                }
            }

            existingStep.StepName = updatedStep.StepName?.Trim();
            existingStep.StepDesc = updatedStep.StepDesc;
            existingStep.Sequence = targetSequence;
            existingStep.StepType = string.IsNullOrWhiteSpace(updatedStep.StepType) ? existingStep.StepType : updatedStep.StepType.Trim();
            existingStep.Enabled = string.IsNullOrWhiteSpace(updatedStep.Enabled) ? existingStep.Enabled : updatedStep.Enabled;
            existingStep.UpdatedBy = username;
            existingStep.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, "Workflow step updated successfully.");
        }
        catch (DbUpdateException)
        {
            return (false, "Unable to update workflow step due to related data constraints.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteWithDependenciesAsync(int stepId, string username)
    {
        var step = await _context.WorkflowSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkflowStepId == stepId);

        if (step == null)
        {
            return (false, "Workflow step not found.");
        }

        var dependentTasks = await _context.Tasks
            .Where(t => t.WorkflowStepId == stepId)
            .ToListAsync();

        WorkflowStep? nextStep = null;
        if (dependentTasks.Count > 0)
        {
            nextStep = await _context.WorkflowSteps
                .AsNoTracking()
                .Where(s => s.WorkflowId == step.WorkflowId && s.Sequence > step.Sequence)
                .OrderBy(s => s.Sequence)
                .FirstOrDefaultAsync();

            if (nextStep == null)
            {
                return (false, "Cannot delete this step because it has dependent tasks and no next step exists.");
            }
        }

        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            if (dependentTasks.Count > 0 && nextStep != null)
            {
                var now = DateTime.Now;

                var newTasks = dependentTasks.Select(t => new Models.Task
                {
                    ItemId = t.ItemId,
                    StatusId = 1,
                    SubStatusId = null,
                    WorkflowStepId = nextStep.WorkflowStepId,
                    IsPaused = "N",
                    AssignedTo = null,
                    AssignedBy = null,
                    AssignedDate = null,
                    CompletedBy = null,
                    CompletedDate = null,
                    CreatedBy = username,
                    CreatedDate = now,
                    UpdatedBy = username,
                    UpdatedDate = now,
                    BasePriority = t.BasePriority,
                    AgingFactor = t.AgingFactor,
                    AgingPeriod = t.AgingPeriod,
                    TaskRedirectUrl = t.TaskRedirectUrl,
                    DelayUntil = t.DelayUntil,
                    DelayHours = t.DelayHours,
                    ShowAsNew = true
                }).ToList();

                _context.Tasks.AddRange(newTasks);

                var itemIds = dependentTasks
                    .Select(t => t.ItemId)
                    .Distinct()
                    .ToList();

                var itemsToUpdate = await _context.Items
                    .Where(i => itemIds.Contains(i.ItemId) && i.CurrentWorkflowStepId == stepId)
                    .ToListAsync();

                foreach (var item in itemsToUpdate)
                {
                    item.CurrentWorkflowStepId = nextStep.WorkflowStepId;
                    item.UpdatedBy = username;
                    item.UpdatedDate = now;
                }

                _context.Tasks.RemoveRange(dependentTasks);
            }

            var trackedStep = await _context.WorkflowSteps.FindAsync(stepId);
            if (trackedStep == null)
            {
                await tx.RollbackAsync();
                return (false, "Workflow step not found.");
            }

            _context.WorkflowSteps.Remove(trackedStep);

            var stepsToReorder = await _context.WorkflowSteps
                .Where(s => s.WorkflowId == step.WorkflowId && s.Sequence > step.Sequence)
                .ToListAsync();

            foreach (var workflowStep in stepsToReorder)
            {
                workflowStep.Sequence -= 1;
                workflowStep.UpdatedBy = username;
                workflowStep.UpdatedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, "Workflow step deleted successfully.");
        }
        catch (DbUpdateException)
        {
            return (false, "Unable to delete step due to related records. Please review task/history dependencies.");
        }
    }

    
}
