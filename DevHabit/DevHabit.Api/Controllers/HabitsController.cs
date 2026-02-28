using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("Habits")]
public class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HabitsCollectionsDto>> GetHabits()
    {
        List<HabitDto> habitDto = await dbContext
            .Habits
            .Select(HabitQueries.ProjectToDto())
            .ToListAsync();


        HabitsCollectionsDto habitsCollectionsDto = new()
        {
            Data = habitDto
        };
        
        return Ok(habitsCollectionsDto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetHabit(string id)
    {
        HabitDto habitDto = await dbContext
            .Habits
            .Select(HabitQueries.ProjectToDto())
            .FirstOrDefaultAsync(h => h.Id == id);

        if (habitDto == null)
        {
            return NotFound();
        }
        
        return Ok(habitDto);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(CreateHabitDto createHabitDto)
    {
        Habit habit = createHabitDto.ToEntity();
        
        dbContext.Habits.Add(habit);

        await dbContext.SaveChangesAsync();
        
        HabitDto habitDto = habit.ToDto();
        
        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);   
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(string id, UpdateHabitDto updateHabitDto)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit == null)
        {
            return NotFound();
        }
        
        habit.UpdateFromDto(updateHabitDto);
        
        await dbContext.SaveChangesAsync();
        
        return NoContent();  
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);
        if (habit == null)
        {
            return NotFound();
        }

        HabitDto habitDto = habit.ToDto();
        
        // we can do more my manipulate the nested object
        patchDocument.ApplyTo(habitDto, ModelState);

        if (!TryValidateModel(habitDto))
        {
            return ValidationProblem(ModelState);
        }

        habit.Name = habitDto.Name;
        habit.Description = habitDto.Description;
        habit.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        
        return NoContent(); 
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteHabit(string id)
    {
        Habit habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit == null)
        {
            return NotFound();
        }
        
        dbContext.Habits.Remove(habit);
        
        await dbContext.SaveChangesAsync();
        
        return NoContent();
    }
}
