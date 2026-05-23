using System.Collections.Generic;
using BeyondNet.Bootstrapper.Interface;

namespace BeyondNet.Bootstrapper.Impl
{
    public class CompositeBootstrapper : IBootstrapper
    {
        private readonly List<IBootstrapper> _bootstrappers;

        public CompositeBootstrapper(IEnumerable<IBootstrapper> bootstrappers)
        {
            _bootstrappers = new List<IBootstrapper>(bootstrappers);
        }

        public CompositeBootstrapper()
        {
            _bootstrappers = new List<IBootstrapper>();
        }

        public CompositeBootstrapper Add(IBootstrapper bootstrapper)
        {
            _bootstrappers.Add(bootstrapper);

            return this;
        }


        public void Run()
        {
            foreach (var bootstrapper in _bootstrappers)
            {
                bootstrapper.Run();
            }
        }
    }
}