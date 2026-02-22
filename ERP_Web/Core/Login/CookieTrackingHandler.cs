using System.Net;
using System.Text.RegularExpressions;

namespace ERP_Web.Core.Login
{
    // CookieTrackingHandler.cs
    public class CookieTrackingHandler : DelegatingHandler
    {
        // 直接持有CookieContainer的引用
        public CookieContainer CookieContainer { get; }

        public CookieTrackingHandler(CookieContainer cookieContainer, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            CookieContainer = cookieContainer;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 在发送请求前，将CookieContainer中的cookie添加到请求头
            string cookieHeader = CookieContainer.GetCookieHeader(request.RequestUri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // 处理响应中的Set-Cookie头（容错：忽略或清理格式不合法的 cookie）
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var cookie in setCookies)
                {
                    try
                    {
                        CookieContainer.SetCookies(response.RequestMessage.RequestUri, cookie);
                    }
                    catch (CookieException)
                    {
                        // 尝试移除空的 Domain= 片段后重试
                        var sanitized = Regex.Replace(cookie, "(?i)(?:;\\s*)?Domain=(?:\\s*;|\\s*$)", string.Empty);
                        try
                        {
                            CookieContainer.SetCookies(response.RequestMessage.RequestUri, sanitized);
                        }
                        catch (CookieException)
                        {
                            // 忽略不可解析的 cookie，防止请求流程中断
                        }
                    }
                }
            }

            return response;
        }
    }
}
