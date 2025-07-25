namespace Reshebnik.Domain.Extensions;

public static class TaskExtensions
{
    public static async Task OneByOne(this IEnumerable<Func<Task>> tasks)
    {
        foreach (var taskFactory in tasks)
        {
            await taskFactory();
        }
    }

    public static async Task OneByOne<T>(this IEnumerable<Func<T, Task>> tasks, T input)
    {
        foreach (var taskFactory in tasks)
        {
            await taskFactory(input);
        }
    }
}