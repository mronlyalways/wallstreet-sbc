using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Investor.ViewModel;
using Investor.Model;

namespace Investor
{
    class Program
    {
        static private MainViewModel viewModel;
        static void Main(string[] args)
        {
            IDataService ds = new XcoDataService();
            viewModel = new MainViewModel(ds);

            while (true)
            {
                string line = Console.ReadLine();
                if (line == "help") {
                    printHelp();
                } 
                else if (line == "register") 
                {
                    register();
                }
                else
                {
                    printHelp();
                }
            }
        }

        static void printHelp() {
            Console.WriteLine("Usage:");
            Console.WriteLine("- help: print this message");
            Console.WriteLine("- register: register a depot");
        }

        static void register()
        {
            Console.Write("Please enter your Email address: ");
            String email = Console.ReadLine();
            Console.Write("Please enter your budget (optional): ");
            String budget = Console.ReadLine();

            viewModel.Register(email, budget);
        }
    }
}
