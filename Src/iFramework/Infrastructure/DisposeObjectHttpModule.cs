using System.Web;
using System;
using System.Collections;

namespace IFramework.Infrastructure
{
    class DisposeObjectHttpModule : IHttpModule
    {
        #region IHttpModule 成员

        public void Dispose()
        {

        }

        public void Init(HttpApplication context)
        {
            context.EndRequest += new EventHandler(context_EndRequest);
        }

        void context_EndRequest(object sender, EventArgs e)
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
