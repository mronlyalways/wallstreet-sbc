using Ninject;

namespace Wallstreet.ViewModel
{
    public class ViewModelLocator
    {
        private static StandardKernel kernel;

        static ViewModelLocator()
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

        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
        }
    }
}