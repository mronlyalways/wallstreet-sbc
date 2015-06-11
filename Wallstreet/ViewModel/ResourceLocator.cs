using Ninject;
using System;
using Wallstreet.Model;
using XcoSpaces;

namespace Wallstreet.ViewModel
{
    public class ResourceLocator
    {
        private static StandardKernel kernel;

        static ResourceLocator()
        {
            kernel = new StandardKernel();
            
        }

        public MainViewModel Main
        {
            get
            {
                return kernel.Get<MainViewModel>();
            }
        }

        public SetupViewModel Setup
        {
            get
            {
                return kernel.Get<SetupViewModel>();
            }
        }

        public static void BindXcoDataService(Uri spaceServer)
        {
            kernel.Bind<IDataService>().To<XcoDataService>().InSingletonScope().WithConstructorArgument(spaceServer);
        }

        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
            kernel.Dispose();
        }
    }
}