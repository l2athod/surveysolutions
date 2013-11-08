using System;
using System.Web;
using Main.Core;
using Main.Core.Commands;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Ncqrs;
using Ncqrs.Commanding.ServiceModel;
using Ncqrs.Eventing.ServiceModel.Bus;
using Ncqrs.Eventing.ServiceModel.Bus.ViewConstructorEventBus;
using Ncqrs.Eventing.Sourcing.Snapshotting;
using Ncqrs.Eventing.Storage;
using Ninject;
using Ninject.Web.Common;
using WB.Core.BoundedContexts.Designer;
using WB.Core.GenericSubdomains.Logging;
using WB.Core.GenericSubdomains.Logging.NLog;
using WB.Core.Infrastructure.Raven;
using WB.Core.SharedKernels.ExpressionProcessor;
using WB.Core.SharedKernels.QuestionnaireVerification;
using WB.UI.Designer.App_Start;
using WB.UI.Designer.Code;
using WB.UI.Designer.CommandDeserialization;
using WB.UI.Designer.Views.Questionnaire.Indexes;
using WebActivator;

[assembly: WebActivator.PreApplicationStartMethod(typeof (NinjectWebCommon), "Start")]
[assembly: ApplicationShutdownMethod(typeof (NinjectWebCommon), "Stop")]

namespace WB.UI.Designer.App_Start
{
    public static class NinjectWebCommon
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();
        private static ViewConstructorEventBus eventBus;

        /// <summary>
        ///     Starts the application
        /// </summary>
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof (OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof (NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }

        /// <summary>
        ///     Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }

        /// <summary>
        ///     Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            MvcApplication.Initialize(); // pinging global.asax to perform it's part of static initialization

            var ravenSettings = new RavenConnectionSettings(storagePath: AppSettings.Instance.RavenDocumentStore,
                username: AppSettings.Instance.RavenUserName, password: AppSettings.Instance.RavenUserPassword);

            var kernel = new StandardKernel(
                new ServiceLocationModule(),
                new NLogLoggingModule(),
                new RavenWriteSideInfrastructureModule(ravenSettings),
                new RavenReadSideInfrastructureModule(ravenSettings, typeof(DesignerReportQuestionnaireListViewItem).Assembly),
                new DesignerCommandDeserializationModule(),
                new DesignerBoundedContextModule(),
                new ExpressionProcessorModule(),
                new QuestionnaireVerificationModule(),
                new MembershipModule(),
                new MainModule(),
                new DesignerRegistry()
                );

            kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
            kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

            PrepareNcqrsInfrastucture(kernel);

            return kernel;
        }

        private static void PrepareNcqrsInfrastucture(StandardKernel kernel)
        {
            var commandService = new ConcurrencyResolveCommandService(ServiceLocator.Current.GetInstance<ILogger>());
            NcqrsEnvironment.SetDefault(commandService);
            NcqrsInit.InitializeCommandService(kernel.Get<ICommandListSupplier>(), commandService);
            kernel.Bind<ICommandService>().ToConstant(commandService);
            NcqrsEnvironment.SetDefault<ISnapshottingPolicy>(new SimpleSnapshottingPolicy(1));
            NcqrsEnvironment.SetDefault<ISnapshotStore>(new InMemoryEventStore());

            CreateAndRegisterEventBus(kernel);
        }

        private static void CreateAndRegisterEventBus(StandardKernel kernel)
        {
            NcqrsEnvironment.SetGetter<IEventBus>(() => GetEventBus(kernel));
            kernel.Bind<IEventBus>().ToMethod(_ => GetEventBus(kernel));
            kernel.Bind<IViewConstructorEventBus>().ToMethod(_ => GetEventBus(kernel));
        }

        private static ViewConstructorEventBus GetEventBus(StandardKernel kernel)
        {
            return eventBus ?? (eventBus = CreateEventBus(kernel));
        }

        private static ViewConstructorEventBus CreateEventBus(StandardKernel kernel)
        {
            var bus = new ViewConstructorEventBus();

            foreach (var handler in kernel.GetAll(typeof (IEventHandler)))
            {
                bus.AddHandler(handler as IEventHandler);
            }

            return bus;
        }
    }
}