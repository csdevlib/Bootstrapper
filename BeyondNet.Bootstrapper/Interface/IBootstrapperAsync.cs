using System.Threading;
using System.Threading.Tasks;

namespace BeyondNet.Bootstrapper.Interface
{
    public interface IBootstrapperAsync<out T> : IBootstrapperAsync
    {
        T? Result { get; }
    }

    public interface IBootstrapperAsync
    {
        Task RunAsync(CancellationToken cancellationToken = default);
    }
}
