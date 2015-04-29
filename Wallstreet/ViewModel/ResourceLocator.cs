using Ninject;
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
            kernel.Bind<IDataService>().To<XcoDataService>().InSingletonScope();
        }

        public MainViewModel Main
        {
            get
            {
                return kernel.Get<MainViewModel>();
            }
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