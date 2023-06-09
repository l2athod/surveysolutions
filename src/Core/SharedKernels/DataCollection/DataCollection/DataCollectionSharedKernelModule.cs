using System.Threading.Tasks;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.Infrastructure.Modularity;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Repositories;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Scenarios;

namespace WB.Core.SharedKernels.DataCollection
{
    public class DataCollectionSharedKernelModule : IModule
    {
        public void Load(IIocRegistry registry)
        {
            registry.Bind<IStatefulInterviewRepository, StatefulInterviewRepository>();
            registry.Bind<IScenarioService, ScenarioService>();
            registry.Bind<StatefulInterview>(true);
        }
    }
}
