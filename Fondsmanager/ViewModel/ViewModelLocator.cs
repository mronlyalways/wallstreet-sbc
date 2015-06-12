using Ninject;
using Fondsmanager.Model;
using XcoSpaces;
using System.Collections.Generic;
using System;
using System.Collections;

namespace Fondsmanager.ViewModel
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

        public LoginViewModel Login
        {
            get
            {
                return kernel.Get<LoginViewModel>();
            }
        }

        public SetupViewModel Setup
        {
            get
            {
                return kernel.Get<SetupViewModel>();
            }
        }

        public static void BindXcoDataService(IList<Uri> spaceServers)
        {

            kernel.Bind<IDataService>().To<XcoDataService>().InSingletonScope().WithConstructorArgument(spaceServers);
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