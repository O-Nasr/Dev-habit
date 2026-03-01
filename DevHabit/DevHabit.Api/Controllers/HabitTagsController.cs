using DevHabit.Api.Database;
using DevHabit.Api.DTOs.HabitTags;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("habits/{habitId}/tags")]
public class HabitTagsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPut]
    public async Task<ActionResult> UpsertHabitTag(string habitId, UpsertHabitTagDto upsertHabitTagDto)
    {
        Habit? habit = await dbContext
            .Habits
            .Include(h => h.HabitTags)
            .FirstOrDefaultAsync(h => h.Id == habitId);

        if (habit == null)
        {
            return NotFound();
        }
        // assume the current tags are [1,2,3,4] and upsert habit tag is [2,3,4,5]
        var currentTags = habit.HabitTags.Select(t => t.TagId).ToHashSet();
        if (currentTags.SetEquals(upsertHabitTagDto.TagIds))
        {
            return NoContent();
        }
        
        List<string> existingTagIds = await dbContext
            .Tags
            .Where(t => upsertHabitTagDto.TagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        if (existingTagIds.Count != upsertHabitTagDto.TagIds.Count)
        {
            return BadRequest("One or more tag IDs are invalid.");
        }
        // here we remove all the tags that are not in the upsert
        // so the current tag will be [2,3,4]
        habit.HabitTags.RemoveAll(t => !upsertHabitTagDto.TagIds.Contains(t.TagId));
        
        // here we add all the tags that are not in the current tags
        // and this tag is [5]
        string[] tagIdsToAdd = upsertHabitTagDto.TagIds.Except(currentTags).ToArray();
        habit.HabitTags.AddRange(tagIdsToAdd.Select(tagId => new HabitTag
        {
            HabitId = habitId, 
            TagId = tagId,
            CreatedAtUtc = DateTime.UtcNow
        }));
        
        await dbContext.SaveChangesAsync();
        
        return Ok();
    }

    [HttpDelete("{tagIds}")]
    public async Task<ActionResult> DeleteHabitTag(string habitId, string tagIds)
    {
        HabitTag? habitTag = await dbContext.HabitTags
            .FirstOrDefaultAsync(t => t.HabitId == habitId && t.TagId == tagIds);

        if (habitTag == null)
        {
            return NotFound();
        }
        
        dbContext.HabitTags.Remove(habitTag);
        await dbContext.SaveChangesAsync();
        
        
        return NoContent();
    }
}
