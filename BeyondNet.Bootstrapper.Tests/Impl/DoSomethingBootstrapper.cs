using BeyondNet.Bootstrapper.Interface;

namespace BeyondNet.Bootstrapper.Tests.Impl
{
    public class DoSomethingBootstrapper : IBootstrapper<bool>
    {
        public void Run()
        {
            Result = true;
        }

        public bool Result { get; private set; }
    }
}
