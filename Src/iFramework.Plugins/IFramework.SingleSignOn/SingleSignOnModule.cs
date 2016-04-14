using System;
using System.Collections.Generic;
using System.IdentityModel;
using System.IdentityModel.Services;
using System.IdentityModel.Services.Configuration;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace IFramework.SingleSignOn
{
    /// <summary>
    /// 单点登录模块
    /// this module fixed bug ID3206: A signin response may only redirect within the current web application: (url) is not allowed
    /// For more information, please visit
    /// http://social.msdn.microsoft.com/Forums/vstudio/en-US/adcdd533-d5e3-4af9-b3f5-b9a6d06b5c44/id3206-a-signin-response-may-only-redirect-within-the-current-web-application-url-is-not-allowed
    /// </summary>
    public class SingleSignOnModule : WSFederationAuthenticationModule
    {
        static SingleSignOnModule()
        {
        }

        /// <summary>
        /// 使用ServiceCertificate加密Cookie
        /// </summary>
        public static void UseServiceCertificateEncryptCookie()
        {
            FederatedAuthentication.FederationConfigurationCreated += OnServiceConfigurationCreated;
        }

        static void OnServiceConfigurationCreated(object sender, FederationConfigurationCreatedEventArgs e)
        {
            var cookieTransforms = new CookieTransform[]
            {
                new DeflateCookieTransform(),
                new RsaEncryptionCookieTransform(e.FederationConfiguration.ServiceCertificate),
                new RsaSignatureCookieTransform(e.FederationConfiguration.ServiceCertificate)
            };
            var sessionTransforms = new List<CookieTransform>(cookieTransforms);
            var sessionHandler = new SessionSecurityTokenHandler(sessionTransforms.AsReadOnly());
            e.FederationConfiguration.IdentityConfiguration.SecurityTokenHandlers.AddOrReplace(sessionHandler);
        }

        /// <summary>
        /// 重定向到身份提供商（事件）
        /// </summary>
        /// <param name="uniqueId">唯一标识</param>
        /// <param name="returnUrl">返回地址</param>
        /// <param name="persist">是否持久</param>
        public override void RedirectToIdentityProvider(string uniqueId, string returnUrl, bool persist)
        {
            //This corrects WIF error ID3206 "A SignInResponse message may only redirect within the current web application:"
            //First Check if the request url doesn't end with a "/"
            if (!returnUrl.EndsWith("/"))
            {
                //Compare if Request Url +"/" is equal to the Realm, so only root access is corrected
                //https://localhost/AppName plus "/" is equal to https://localhost/AppName/
                //This is to avoid MVC urls
                if (String.Compare(System.Web.HttpContext.Current.Request.Url.AbsoluteUri + "/", base.Realm, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    //Add the trailing slash
                    returnUrl += "/";
                }
            }

            var currentAudienceUri = GetCurrentAudienceUri();
            //Realm = currentAudienceUri.AbsoluteUri;
            Reply = currentAudienceUri.AbsoluteUri;

            var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            base.RedirectToIdentityProvider(uniqueId, returnUrl, persist);
        }

        /// <summary>
        /// 获取当前AudienceUri
        /// </summary>
        /// <returns>当前AudienceUri</returns>
        public static Uri GetCurrentAudienceUri()
        {
            var audienceUris = FederatedAuthentication.WSFederationAuthenticationModule.FederationConfiguration.IdentityConfiguration.AudienceRestriction.AllowedAudienceUris;
            var currentUrl = HttpContext.Current.Request.Url.AbsoluteUri.ToLower();
            var compareUrl = currentUrl.EndsWith("/") ? currentUrl : currentUrl + "/";
            var currentAudienceUri = audienceUris.FirstOrDefault(ent => compareUrl.ToLower().Contains(ent.AbsoluteUri.ToLower()));
            if (currentAudienceUri == null)
            {
                throw new Exception("未找到到与访问地址匹配的audienceUri,请检查配置文件中的audienceUris");
            }
            return currentAudienceUri;
        }

        /// <summary>
        /// 从WEF中注销
        /// </summary>
        /// <example>
        /// <para>以下示例创建了一个用于注销的Action，一般置于AccountController中</para> 
        /// <para>其中SingleSignOnModule.SignOut()用于注销当前应用且生成一个注销请求</para> 
        /// <para>重定向到注销请求，以便让认证中心注销其他已登录的应用</para> 
        /// <code>
        /// public ActionResult LogOff()
        /// {
        ///     var signOutRequest = SingleSignOnModule.SignOut();
        ///     return Redirect(signOutRequest.WriteQueryString());
        /// }
        /// </code>
        /// </example>
        /// <returns>注销请求</returns>
        public new static SignOutRequestMessage SignOut()
        {
            var fam = FederatedAuthentication.WSFederationAuthenticationModule;
            try
            {
                FormsAuthentication.SignOut();
            }
            finally
            {
                fam.SignOut(true);
            }
            var currentAudienceUri = GetCurrentAudienceUri();
            return new SignOutRequestMessage(new Uri(fam.Issuer), currentAudienceUri.AbsoluteUri);
        }

        private bool ValidateSignoutCleanupRequest(HttpRequest request)
        {
            var wa = request.Params["wa"];
            return "wsignoutcleanup1.0".Equals(wa);
        }

        protected override void InitializeModule(HttpApplication context)
        {
            base.InitializeModule(context);
            context.PostAcquireRequestState += OnPostAcquireRequestState;
            context.PostMapRequestHandler += OnPostMapRequestHandler;

        }

        public override bool CanReadSignInResponse(HttpRequestBase request, bool onPage)
        {
            //var wa = request.Params["wa"];
            //return !"wsignoutcleanup1.0".Equals(wa) && base.CanReadSignInResponse(request, onPage);
            return base.CanReadSignInResponse(request, onPage);
        }


        protected void OnPostAcquireRequestState(object sender, EventArgs e)
        {
            var request = HttpContext.Current.Request;
            if (!ValidateSignoutCleanupRequest(request)) return;

            MyHttpHandler resourceHttpHandler = HttpContext.Current.Handler as MyHttpHandler;

            if (resourceHttpHandler != null)
            {
                // set the original handler back
                HttpContext.Current.Handler = resourceHttpHandler.OriginalHandler;
            }


            //Kill the session
            var session = HttpContext.Current.Session;
            session.Abandon();

            //Remove the session authentication cookies
            base.CanReadSignInResponse(new HttpRequestWrapper(request), false);
        }

        protected void OnPostMapRequestHandler(object source, EventArgs e)
        {
            var request = HttpContext.Current.Request;
            if (!ValidateSignoutCleanupRequest(request)) return;

            HttpApplication app = (HttpApplication)source;

            if (app.Context.Handler is IReadOnlySessionState || app.Context.Handler is IRequiresSessionState)
            {
                // no need to replace the current handler
                return;
            }

            // swap the current handler
            app.Context.Handler = new MyHttpHandler(app.Context.Handler);
        }
    }
    internal class MyHttpHandler : IHttpHandler, IRequiresSessionState
    {
        internal readonly IHttpHandler OriginalHandler;

        public MyHttpHandler(IHttpHandler originalHandler)
        {
            OriginalHandler = originalHandler;
        }

        public void ProcessRequest(HttpContext context)
        {
            // do not worry, ProcessRequest() will not be called, but let's be safe
            throw new InvalidOperationException("MyHttpHandler cannot process requests.");
        }

        public bool IsReusable
        {
            // IsReusable must be set to false since class has a member!
            get { return false; }
        }
    }
}
