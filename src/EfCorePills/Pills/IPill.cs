using EfCorePills.Diagnostics;

namespace EfCorePills.Pills;

public interface IPill
{
    /// <summary>Name used on the command line to run only this pill.</summary>
    string Name { get; }

    Task RunAsync(Measurer measurer);
}
