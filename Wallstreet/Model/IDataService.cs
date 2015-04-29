using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wallstreet.Model
{
    public interface IDataService
    {
        IEnumerable<ShareInformation> LoadShareInformation();

        void OnNewShareInformationAvailable(Action<ShareInformation> callback);
    }
}
