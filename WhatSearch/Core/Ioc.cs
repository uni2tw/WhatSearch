using Newtonsoft.Json;
using Ninject;
using WhatSearch.Service;
using System;

namespace WhatSearch.Core
{
    public class Ioc
    {
        private static IKernel _kernel;
        public static void Init()
        {
            _kernel = new StandardKernel();
            _kernel.Bind<SystemConfig>().ToConstant(SystemConfig.Reload());
            _kernel.Bind<ICommonLog>().To<CommonLogger>().InSingletonScope();
            
            _kernel.Bind<ISeekService>().To<SimpleSeekService>().InSingletonScope();
            _kernel.Bind<ISearchManager>().To<LuceneSearchInRam>().InSingletonScope();
            _kernel.Bind<ILoginProvider>().To<FileZillaLoginProvider>().InSingletonScope();
            _kernel.Bind<IMemberService>().To<MemberService>().InSingletonScope();
            _kernel.Bind<IFileWatcherService>().To<FileWatcherService>().InSingletonScope();

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
