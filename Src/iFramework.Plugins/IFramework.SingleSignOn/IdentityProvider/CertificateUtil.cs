using System;
using System.Security.Cryptography.X509Certificates;

namespace IFramework.SingleSignOn.IdentityProvider
{
    public class CertificateUtil
    {
        public static X509Certificate2 GetCertificate(StoreName name, StoreLocation location, string subjectName)
        {
            var store = new X509Store(name, location);
            X509Certificate2Collection certificates = null;
            store.Open(OpenFlags.ReadOnly);

            try
            {
                X509Certificate2 result = null;
                certificates = store.Certificates;

                foreach (var cert in certificates)
                {
                    if (cert.SubjectName.Name == null || !String.Equals(cert.SubjectName.Name, subjectName, StringComparison.CurrentCultureIgnoreCase))
                        continue;
                    if (result != null)
                        throw new ApplicationException(string.Format("subject Name {0}存在多个证书", subjectName));
                    result = new X509Certificate2(cert);
                }

                if (result == null)
                {
                    throw new ApplicationException(string.Format("没有找到用于 subject Name {0} 的证书", subjectName));
                }

                return result;
            }
            finally
            {
                if (certificates != null)
                {
                    foreach (var cert in certificates)
                    {
                        cert.Reset();
                    }
                }
                store.Close();
            }
        }
    }
}