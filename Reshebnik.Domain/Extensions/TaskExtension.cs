namespace Reshebnik.Domain.Extensions;

public static class TaskExtensions
{
    public static async Task OneByOne(params Func<Task>[] tasks)
    {
        foreach (var taskFactory in tasks)
        {
            await taskFactory();
        }
    }

    public static async Task OneByOne<T>(T input, params Func<T, Task>[] tasks)
    {
        foreach (var taskFactory in tasks)
        {
            await taskFactory(input);
        }
    }
}