using System;
using System.Collections;
using System.Web;

namespace IFramework.Infrastructure
{
    public class DisposeObjectHttpModule : IHttpModule
    {
        #region IHttpModule 成员

        public void Dispose() { }

        public void Init(HttpApplication context)
        {
            context.EndRequest += context_EndRequest;
        }

        private void context_EndRequest(object sender, EventArgs e)
        {
            if (HttpContext.Current != null)
            {
                foreach (DictionaryEntry resource in HttpContext.Current.Items)
                {
                    if (resource.Value != null && resource.Value is IDisposable)
                    {
                        (resource.Value as IDisposable).Dispose();
                    }
                }
            }
        }

        #endregion
    }
}