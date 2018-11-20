using Newtonsoft.Json;
using Ninject;
using WhatSearch.Service;
using System;
using WhatSearch.Services.Interfaces;
using WhatSearch.Services;
using WhatSearch.Jobs;
using WhatSearch.DataProviders;
using WhatSearch.DataProviders.Interfaces;

namespace WhatSearch.Core
{
    public class Ioc
    {
        private static IKernel _kernel;
        public static void Register()
        {
            _kernel = new StandardKernel();
            _kernel.Bind<SystemConfig>().ToConstant(SystemConfig.Reload());
            _kernel.Bind<ICommonLog>().To<CommonLogger>().InSingletonScope();
            
            _kernel.Bind<IDocumentService>().To<SimpleDocumentService>().InSingletonScope();
            _kernel.Bind<ISearchSercice>().To<SimpleSearchService>().InSingletonScope();
            _kernel.Bind<IFileWatcherService>().To<FileWatcherService>().InSingletonScope();
            _kernel.Bind<IChineseConverter>().To<ChineseConverter>().InSingletonScope();
            _kernel.Bind<IFileSystemInfoIdAssigner>().To<FolderIdAssigner>().InSingletonScope();
            _kernel.Bind<IMainService>().To<MainService>().InSingletonScope();
            _kernel.Bind<IRssService>().To<RssService>().InSingletonScope();
            _kernel.Bind<IMemberProvider>().To<MemberProvider>().InSingletonScope();
            _kernel.Bind<IUserService>().To<UserService>().InSingletonScope();

            _kernel.Bind<IReSeekFolderJob>().To<ResSeekFolderJob>();
            //_kernel.Bind<IFolderChecker>().To<FolderModifyChecker>().InSingletonScope();

        }

        public static T Get<T>() where T : class
        {
            if (typeof(T).IsInterface==false)
            {
                throw new Exception("T must be interface");
            }
            return _kernel.Get<T>();
        }

        public static SystemConfig GetConfig()
        {
            return _kernel.Get<SystemConfig>();
        }

        public static void ReBind<T>(T t)
        {
            _kernel.Rebind<T>().ToConstant(t);
        }
    }
}
