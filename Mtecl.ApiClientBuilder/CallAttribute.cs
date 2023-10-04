using Mtecl.ApiClientBuilder.Abstract;
using System;
using System.Net.Http;

namespace Mtecl.ApiClientBuilder
{
    public abstract class CallAttribute : Attribute, IActionUrlProvider, IHttpMethodProvider
    {
        protected CallAttribute(HttpMethod httpMethod, string url)
        {
            HttpMethod = httpMethod;
            Url = url;
        }

        public HttpMethod HttpMethod { get; }

        public string Url { get; }
    }

    public class GetAttribute : CallAttribute
    {
        public GetAttribute(string url) : base(HttpMethod.Get, url) { }
    }

    public class PostAttribute : CallAttribute
    {
        public PostAttribute(string url) : base(HttpMethod.Post, url) { }
    }

    public class PutAttribute : CallAttribute
    {
        public PutAttribute(string url) : base(HttpMethod.Put, url) { }
    }

    public class DeleteAttribute : CallAttribute
    {
        public DeleteAttribute(string url) : base(HttpMethod.Delete, url) { }
    }
}