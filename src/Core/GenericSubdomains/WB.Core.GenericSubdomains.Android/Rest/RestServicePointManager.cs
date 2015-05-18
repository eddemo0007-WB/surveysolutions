using System.Net;

using WB.Core.GenericSubdomains.Portable.Rest;

namespace WB.Core.GenericSubdomains.Android.Rest
{
    public class RestServicePointManager : IRestServicePointManager
    {
        public void AcceptUnsignedSslCertificate()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }
    }
}