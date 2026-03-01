namespace DevHabit.Api.DTOs.Habits;

public sealed record HabitsCollectionsDto
{
    public List<HabitDto> Data { get; init; }
}
