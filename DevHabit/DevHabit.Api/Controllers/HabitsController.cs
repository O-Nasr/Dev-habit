using System.Dynamic;
using DevHabit.Api.Common;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("Habits")]
public sealed class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetHabits(
        [FromQuery] HabitsQueryParameters query,
            DataShapingService dataShapingService,
            SortMappingProvider sortMappingProvider)
    {
        if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort mappings aren't valid: '{query.Sort}'");
        }
        
        if (!dataShapingService.Validate<HabitDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields aren't valid: '{query.Fields}'");
        }
        
        query.Search = query.Search?.Trim().ToLower();
        
        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();
        
        IQueryable<HabitDto> habitQuery = dbContext
            .Habits
            .Where(h => query.Search == null ||
                        h.Name.ToLower().Contains(query.Search) ||
                        h.Description != null && h.Description.ToLower().Contains(query.Search))
            .Where(h => query.Type == null || h.Type == query.Type)
            .Where(h => query.Status == null || h.Status == query.Status)
            .ApplySort(query.Sort, sortMappings)
            .Select(HabitQueries.ProjectToDto());
        
        int totalCount = await habitQuery.CountAsync();

        List<HabitDto> habitDtos = await habitQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(habitDtos, query.Fields),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
        
        // var paginationResult = await PaginationResult<HabitDto>.CreateAsync(habitQuery, query.Page, query.PageSize);
        
        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetHabit(string id, string? fields, DataShapingService dataShapingService)
    {
        if (!dataShapingService.Validate<HabitDto>(fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields aren't valid: '{fields}'");
        }
        
        HabitWithTagDto habitDto = await dbContext
            .Habits
            .Select(HabitQueries.ProjectToDtoWithTags())
            .FirstOrDefaultAsync(h => h.Id == id);

        if (habitDto == null)
        {
            return NotFound();
        }

        ExpandoObject shapingHabitDto = dataShapingService.ShapeData(habitDto, fields);
        
        return Ok(shapingHabitDto);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(CreateHabitDto createHabitDto, IValidator<CreateHabitDto> validator)
    {
        await validator.ValidateAndThrowAsync(createHabitDto);

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
