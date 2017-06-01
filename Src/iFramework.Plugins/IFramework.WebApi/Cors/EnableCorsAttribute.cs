using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Http.Cors;
using IFramework.Config;

namespace IFramework.AspNet
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class EnableCorsAttribute : Attribute, ICorsPolicyProvider
    {
        private readonly CorsPolicy _corsPolicy;
        private bool _originsValidated;

        public EnableCorsAttribute(string origins, string headers, string methods) : this(origins, headers, methods,
                                                                                          false, null) { }

        public EnableCorsAttribute(string origins,
                                   string headers,
                                   string methods,
                                   bool supportsCredentials = true,
                                   string exposedHeaders = null)
        {
            if (string.IsNullOrEmpty(origins))
            {
                origins = Configuration.GetAppConfig("AllowOrigins");
                if (string.IsNullOrEmpty(origins))
                {
                    throw new ArgumentException("ArgumentCannotBeNullOrEmpty", "origins");
                }
            }
            _corsPolicy = new CorsPolicy();
            _corsPolicy.SupportsCredentials = supportsCredentials;
            if (origins == "*")
            {
                _corsPolicy.AllowAnyOrigin = true;
            }
            else
            {
                AddCommaSeparatedValuesToCollection(origins, _corsPolicy.Origins);
            }
            if (!string.IsNullOrEmpty(headers))
            {
                if (headers == "*")
                {
                    _corsPolicy.AllowAnyHeader = true;
                }
                else
                {
                    AddCommaSeparatedValuesToCollection(headers, _corsPolicy.Headers);
                }
            }
            if (!string.IsNullOrEmpty(methods))
            {
                if (methods == "*")
                {
                    _corsPolicy.AllowAnyMethod = true;
                }
                else
                {
                    AddCommaSeparatedValuesToCollection(methods, _corsPolicy.Methods);
                }
            }
            if (!string.IsNullOrEmpty(exposedHeaders))
            {
                AddCommaSeparatedValuesToCollection(exposedHeaders, _corsPolicy.ExposedHeaders);
            }
        }

        public IList<string> ExposedHeaders => _corsPolicy.ExposedHeaders;

        public IList<string> Headers => _corsPolicy.Headers;

        public IList<string> Methods => _corsPolicy.Methods;

        public IList<string> Origins => _corsPolicy.Origins;

        public long PreflightMaxAge
        {
            get
            {
                var preflightMaxAge = _corsPolicy.PreflightMaxAge;
                if (!preflightMaxAge.HasValue)
                {
                    return -1L;
                }
                return preflightMaxAge.GetValueOrDefault();
            }
            set => _corsPolicy.PreflightMaxAge = value;
        }

        public bool SupportsCredentials
        {
            get => _corsPolicy.SupportsCredentials;
            set => _corsPolicy.SupportsCredentials = value;
        }

        public Task<CorsPolicy> GetCorsPolicyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_originsValidated)
            {
                ValidateOrigins(_corsPolicy.Origins);
                _originsValidated = true;
            }
            return Task.FromResult(_corsPolicy);
        }

        private static void AddCommaSeparatedValuesToCollection(string commaSeparatedValues, IList<string> collection)
        {
            var strArray = commaSeparatedValues.Split(',');
            for (var i = 0; i < strArray.Length; i++)
            {
                var str = strArray[i].Trim();
                if (!string.IsNullOrEmpty(str))
                {
                    collection.Add(str);
                }
            }
        }

        private static void ValidateOrigins(IList<string> origins)
        {
            foreach (var str in origins)
            {
                if (string.IsNullOrEmpty(str))
                {
                    throw new InvalidOperationException("OriginCannotBeNullOrEmpty");
                }
                if (str.EndsWith("/", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                                                        string.Format(CultureInfo.CurrentCulture, "OriginCannotEndWithSlash", str));
                }
                if (!Uri.IsWellFormedUriString(str, UriKind.Absolute))
                {
                    throw new InvalidOperationException(
                                                        string.Format(CultureInfo.CurrentCulture, "OriginNotWellFormed", str));
                }
                var uri = new Uri(str);
                if (!string.IsNullOrEmpty(uri.AbsolutePath) &&
                    !string.Equals(uri.AbsolutePath, "/", StringComparison.Ordinal) ||
                    !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                                                      "OriginMustNotContainPathQueryOrFragment", str));
                }
            }
        }
    }
}