namespace EfCoreInterceptors.Demos;

public interface IDemo
{
    string Name { get; }

    string Title { get; }

    Task RunAsync();
}
