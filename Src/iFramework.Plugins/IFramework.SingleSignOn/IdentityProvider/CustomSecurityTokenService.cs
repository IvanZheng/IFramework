using System;
using System.IdentityModel;
using System.IdentityModel.Configuration;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Configuration;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace IFramework.SingleSignOn.IdentityProvider
{
    public abstract class CustomSecurityTokenService : SecurityTokenService
    {
        private readonly SigningCredentials _signingCreds;
        private readonly EncryptingCredentials _encryptingCreds;

        public CustomSecurityTokenService(SecurityTokenServiceConfiguration config)
            : base(config)
        {
            var certificate = CertificateUtil.GetCertificate(StoreName.My, StoreLocation.LocalMachine, WebConfigurationManager.AppSettings[Common.SigningCertificateName]);
            this._signingCreds = new X509SigningCredentials(certificate);
           

            if (!string.IsNullOrWhiteSpace(WebConfigurationManager.AppSettings[Common.EncryptingCertificateName]))
            {
                this._encryptingCreds = new X509EncryptingCredentials(
                    CertificateUtil.GetCertificate(StoreName.My, StoreLocation.LocalMachine, WebConfigurationManager.AppSettings[Common.EncryptingCertificateName]));
            }
        }

        protected abstract ICustomIdentityObject GetCustomIdentity(string identity);


        /// <summary>
        /// 此方法返回要发布的令牌内容。内容由一组ClaimsIdentity实例来表示，每一个实例对应了一个要发布的令牌。当前Windows Identity Foundation只支持单个令牌发布，因此返回的集合必须总是只包含单个实例。
        /// </summary>
        /// <param name="principal">调用方的principal</param>
        /// <param name="request">进入的 RST,我们这里不用它</param>
        /// <param name="scope">由之前通过GetScope方法返回的范围</param>
        /// <returns></returns>
        protected override ClaimsIdentity GetOutputClaimsIdentity(ClaimsPrincipal principal, RequestSecurityToken request, Scope scope)
        {
            //返回一个默认声明集，里面了包含自己想要的声明
            //这里你可以通过ClaimsPrincipal来验证用户，并通过它来返回正确的声明。
            var identityName = principal.Identity.Name;
            var identityObject = GetCustomIdentity(identityName);
            //var organizationService = IoCFactory.Resolve<IOrganizationService>();
            //var user = organizationService.GetUser(identityName);

            var outgoingIdentity = new ClaimsIdentity();
            outgoingIdentity.AddClaim(new Claim(ClaimTypes.Name, identityObject.Name));
            outgoingIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identityObject.ID));
            
            outgoingIdentity.AddClaim(new Claim("IFramework/Info", JsonConvert.SerializeObject(identityObject)));
            SingleSignOnManager.RegisterRP(scope.AppliesToAddress);
            return outgoingIdentity;
        }

        /// <summary>
        /// 此方法返回用于令牌发布请求的配置。配置由Scope类表示。在这里，我们只发布令牌到一个由encryptingCreds字段表示的RP标识        /// </summary>
        /// <param name="principal"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        protected override Scope GetScope(ClaimsPrincipal principal, RequestSecurityToken request)
        {
            // 使用request的AppliesTo属性和RP标识来创建Scope
            var scope = new Scope(request.AppliesTo.Uri.AbsoluteUri, this._signingCreds);
            
            if (Uri.IsWellFormedUriString(request.ReplyTo, UriKind.Absolute))
            {
                if (request.AppliesTo.Uri.Host != new Uri(request.ReplyTo).Host)
                    scope.ReplyToAddress = request.AppliesTo.Uri.AbsoluteUri;
                else
                {
                    scope.ReplyToAddress = request.ReplyTo;
                }
                
            }
            else
            {
                Uri resultUri = null;
                if (Uri.TryCreate(request.AppliesTo.Uri, request.ReplyTo, out resultUri))
                    scope.ReplyToAddress = resultUri.AbsoluteUri;
                else
                    scope.ReplyToAddress = request.AppliesTo.Uri.ToString();
            }
            if (this._encryptingCreds != null)
            {
                // 如果STS对应多个RP，要选择证书指定到请求令牌的RP，然后再用 encryptingCreds 
                scope.EncryptingCredentials = this._encryptingCreds;
            }
            else
                scope.TokenEncryptionRequired = false;
            return scope;
        }

        //protected override RequestSecurityTokenResponse GetResponse(RequestSecurityToken request, SecurityTokenDescriptor tokenDescriptor)
        //{
        //    var userId = tokenDescriptor.Subject.Claims.First(ent => ent.Type == ClaimTypes.NameIdentifier).Value;
        //    Uri returnUri = new Uri(request.ReplyTo);
        //    string applicationId = HttpUtility.ParseQueryString(returnUri.Query).Get("ApplicationId");
        //    if (applicationId == "E0B0CECC-1BE5-442E-9DB3-3C6DA1CD4359")
        //    {
        //        var formUriHost = new UriBuilder(returnUri.Scheme, returnUri.Host,
        //                    returnUri.Port);
        //        var formUriUrl = new UriBuilder(returnUri.Scheme, returnUri.Host,
        //            returnUri.Port, returnUri.Segments[1]);
        //        string builderreturnurl = formUriUrl.ToString() + "SSO.aspx";

        //        var returnurl = SetQueryString(builderreturnurl, "userid", userId);
        //        returnurl = SetQueryString(returnurl, "url",
        //            formUriHost + returnUri.LocalPath.Remove(0, 1));
        //        tokenDescriptor.ReplyToAddress = returnurl;
        //    }


        //    return base.GetResponse(request, tokenDescriptor);
        //}

        public static String ConstructQueryString(NameValueCollection parameters)
        {
            List<String> items = new List<String>();

            foreach (String name in parameters)
                items.Add(String.Concat(name, "=", System.Web.HttpUtility.UrlEncode(parameters[name])));

            return String.Join("&", items.ToArray());
        }

        public static string SetQueryString(string url, string key, string value)
        {
            if (url.IndexOf("?") > -1)
            {
                var host = url.Split('?')[0];
                var queryString = HttpUtility.ParseQueryString(url.Split('?')[1]);
                queryString[key] = value;
                return host + "?" + ConstructQueryString(queryString);
            }
            return url + "?" + key + "=" + value;
        }
    }

}