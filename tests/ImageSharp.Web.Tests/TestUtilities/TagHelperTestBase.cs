// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Middleware;

namespace SixLabors.ImageSharp.Web.Tests.TestUtilities
{
    public abstract class TagHelperTestBase : IDisposable
    {
        protected TagHelperTestBase(Action<ImageSharpMiddlewareOptions> setupAction)
        {
            ServiceCollection services = new();
            services.AddSingleton<IWebHostEnvironment, FakeWebHostEnvironment>();
            services.AddImageSharp(setupAction);
            this.Provider = services.BuildServiceProvider();
        }

        protected ServiceProvider Provider { get; }

        protected static TagHelperContext MakeTagHelperContext(
            TagHelperAttributeList attributes)
            => new(
                tagName: "image",
                allAttributes: attributes,
                items: new Dictionary<object, object>(),
                uniqueId: Guid.NewGuid().ToString("N"));

        protected static TagHelperOutput MakeImageTagHelperOutput(TagHelperAttributeList attributes)
        {
            attributes ??= new TagHelperAttributeList();

            return new TagHelperOutput(
                "img",
                attributes,
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent(default);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
        }

        protected static ViewContext MakeViewContext(string requestPathBase = null)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            if (requestPathBase != null)
            {
                actionContext.HttpContext.Request.PathBase = new PathString(requestPathBase);
            }

            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());
            return new ViewContext(
                actionContext,
                new FakeView(),
                viewData,
                new FakeTempDataDictionary(),
                TextWriter.Null,
                new HtmlHelperOptions());
        }

        public void Dispose() => this.Provider.Dispose();

        protected class FakeWebHostEnvironment : IWebHostEnvironment
        {
            public string WebRootPath { get; set; }

            public IFileProvider WebRootFileProvider { get; set; } = new FakeFileProvider();

            public string ApplicationName { get; set; }

            public IFileProvider ContentRootFileProvider { get; set; }

            public string ContentRootPath { get; set; }

            public string EnvironmentName { get; set; }
        }

        protected class FakeView : IView
        {
            public string Path { get; }

            public Task RenderAsync(ViewContext context) => throw new NotSupportedException();
        }

        protected class FakeTempDataDictionary : Dictionary<string, object>, ITempDataDictionary
        {
            public void Keep() => throw new NotSupportedException();

            public void Keep(string key) => throw new NotSupportedException();

            public void Load() => throw new NotSupportedException();

            public object Peek(string key) => throw new NotSupportedException();

            public void Save() => throw new NotSupportedException();
        }

        protected class FakeFileProvider : IFileProvider
        {
            public IDirectoryContents GetDirectoryContents(string subpath) => new FakeDirectoryContents();

            public IFileInfo GetFileInfo(string subpath) => new FakeFileInfo();

            public IChangeToken Watch(string filter) => new FakeFileChangeToken();
        }

        protected class FakeFileChangeToken : IChangeToken
        {
            public FakeFileChangeToken(string filter = "") => this.Filter = filter;

            public bool ActiveChangeCallbacks => false;

            public bool HasChanged { get; set; }

            public string Filter { get; }

            public IDisposable RegisterChangeCallback(Action<object> callback, object state) => new NullDisposable();

            private sealed class NullDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }

            public override string ToString() => this.Filter;
        }

        protected class FakeDirectoryContents : IDirectoryContents
        {
            public bool Exists { get; }

            public IEnumerator<IFileInfo> GetEnumerator() => Enumerable.Empty<IFileInfo>().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }

        protected class FakeFileInfo : IFileInfo
        {
            public bool Exists { get; } = true;

            public bool IsDirectory { get; }

            public DateTimeOffset LastModified { get; }

            public long Length { get; }

            public string Name { get; }

            public string PhysicalPath { get; }

            public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes("Hello World!"));
        }

        protected class FakeUrlHelperFactory : IUrlHelperFactory
        {
            public IUrlHelper GetUrlHelper(ActionContext context) => new FakeUrlHelper() { ActionContext = context };
        }

        protected class FakeUrlHelper : IUrlHelper
        {
            public ActionContext ActionContext { get; set; }

            public string Action(UrlActionContext actionContext) => throw new NotSupportedException();

            // Ensure expanded path does not look like an absolute path on Linux, avoiding
            // https://github.com/aspnet/External/issues/21
            [return: NotNullIfNotNull("contentPath")]
            public string Content(string contentPath) => contentPath.Replace("~/", "virtualRoot/");

            public bool IsLocalUrl([NotNullWhen(true)] string url) => throw new NotSupportedException();

            public string Link(string routeName, object values) => throw new NotSupportedException();

            public string RouteUrl(UrlRouteContext routeContext) => throw new NotSupportedException();
        }
    }
}
