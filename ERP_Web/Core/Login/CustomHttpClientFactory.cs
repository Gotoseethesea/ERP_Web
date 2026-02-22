//using ERP_Web.Controllers;
using System.Collections.Concurrent;
using System.Net;

namespace ERP_Web.Core.Login
{
    // CustomHttpClientFactory.cs
    public class CookieEnabledClientFactory : IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ConcurrentDictionary<string, (HttpClient Client, CookieContainer Handler)> _clients = new();

        public CookieEnabledClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        //public (HttpClient Client, HttpClientHandler Handler) CreateCookieEnabledClient(string name)
        //{
        //    return _clients.GetOrAdd(name, _ =>
        //    {
        //        var client = _httpClientFactory.CreateClient(name);

        //        // 创建实际使用的基础 Handler
        //        var baseHandler = new HttpClientHandler
        //        {
        //            UseCookies = true,
        //            AllowAutoRedirect = false,
        //            CookieContainer = new CookieContainer(),
        //            UseDefaultCredentials = true,
        //            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        //        };

        //        // 创建委托处理器链
        //        var handler = new CookieTrackingHandler(baseHandler);

        //        // 创建实际使用的 HttpClient
        //        var actualClient = new HttpClient(handler)
        //        {
        //            BaseAddress = client.BaseAddress,
        //            Timeout = client.Timeout
        //        };

        //        // 复制默认请求头
        //        foreach (var header in client.DefaultRequestHeaders)
        //        {
        //            actualClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        //        }

        //        return (actualClient, baseHandler);
        //    });
        //}
        public (HttpClient Client, CookieContainer CookieContainer) CreateCookieEnabledClient(string name)
        {
            //name = cookiesClientname:CookieEnabledClient
            return _clients.GetOrAdd(name, _ =>
            {
                var client = _httpClientFactory.CreateClient(name);

                // 创建CookieContainer
                var cookieContainer = new CookieContainer();

                // 创建基础Handler（实际发送请求的Handler）
                var baseHandler = new HttpClientHandler
                {
                    // 注意：这里我们不使用它的CookieContainer，因为我们自己管理
                    UseCookies = false, // 禁用内置的Cookie处理，因为我们手动处理
                    AllowAutoRedirect = false,
                    UseDefaultCredentials = true,
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                // 创建自定义的CookieTrackingHandler，并传入cookieContainer和baseHandler
                var cookieHandler = new CookieTrackingHandler(cookieContainer, baseHandler);

                // 创建实际使用的HttpClient
                var actualClient = new HttpClient(cookieHandler)
                {
                    BaseAddress = client.BaseAddress,
                    Timeout = client.Timeout
                };

                // 复制默认请求头
                foreach (var header in client.DefaultRequestHeaders)
                {
                    actualClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                return (actualClient, cookieContainer);
            });
        }
        public void Dispose()
        {
            foreach (var (client, _) in _clients.Values)
            {
                client.Dispose();
            }
            _clients.Clear();
        }
    }

}
