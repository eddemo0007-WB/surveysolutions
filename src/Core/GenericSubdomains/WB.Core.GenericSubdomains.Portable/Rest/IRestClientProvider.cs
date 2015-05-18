using System.Threading;

using WB.Core.GenericSubdomains.Utils.Implementation;
using WB.Core.GenericSubdomains.Utils.Services;

namespace WB.Core.GenericSubdomains.Portable.Rest
{
    public interface IRestClientProvider
    {
        IRestClient GetRestClient(string url, object queryString, RestCredentials credentials, IRestServiceSettings restServiceSettings, CancellationToken token);
    }
}