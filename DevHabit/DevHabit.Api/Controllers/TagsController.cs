using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Tags;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;
[ApiController]
[Route("tags")]
public class TagsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TagsCollectionDto>> GetTags()
    {
        List<TagDto> tagsDto = await dbContext
            .Tags
            .Select(TagQueries.ProjectToDto())
            .ToListAsync();

        TagsCollectionDto tagsCollectionDto = new()
        {
            Data = tagsDto
        };
        
        return Ok(tagsCollectionDto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag(string id)
    {
        TagDto? tag = await dbContext
            .Tags
            .Where(t => t.Id == id)
            .Select(TagQueries.ProjectToDto())
            .FirstOrDefaultAsync();
        
        if (tag == null)
        {
            return NotFound();
        }
        
        return Ok(tag);
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag(CreateTagDto createTagDto)
    {
        Tag tag = createTagDto.ToEntity();

        if (await dbContext.Tags.AnyAsync(t => t.Name == tag.Name))
        {
            return Conflict($"The tag '{tag.Name}' already exists.");
        }

        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync();
        
        TagDto tagDto = tag.ToDto();
        
        return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTag(string id, UpdateTagDto updateTagDto)
    {
        Tag tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id);
        if (tag == null)
        {
            return NotFound();
        }
        
        tag.UpdateFromDto(updateTagDto);
        await dbContext.SaveChangesAsync();
        
        return NoContent(); 
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag(string id)
    {
        Tag tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id);
        if (tag == null)
        {
            return NotFound();
        }
        
        dbContext.Tags.Remove(tag);
        await dbContext.SaveChangesAsync();
        
        return NoContent();
    }
}
