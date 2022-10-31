// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.TagHelpers;
using SixLabors.ImageSharp.Web.Tests.TestUtilities;
using Xunit;

namespace SixLabors.ImageSharp.Web.Tests.TagHelpers
{
    public sealed class HmacTokenTagHelperTests : TagHelperTestBase
    {
        public HmacTokenTagHelperTests()
            : base(options => options.HMACSecretKey = new byte[] { 1, 2, 3, 4, 5 })
        {
        }

        [Fact]
        public void RendersHmacTokenTag_SrcIncludes_HMAC()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png?width=50" },
                    { "width", 50 }
                });

            TagHelperOutput output = MakeImageTagHelperOutput(
                attributes: new TagHelperAttributeList
                {
                    { "width", 50 }
                });

            TagHelperOutput expectedOutput = MakeImageTagHelperOutput(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png?width=50&hmac=54edff059ad28d0f0ec2494de1dce0e6152e8d26e53e2efb249cdae93e30acbc" },
                    { "width", 50 }
                });

            HmacTokenTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png?width=50";

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(expectedOutput.TagName, output.TagName);
            Assert.Equal(2, output.Attributes.Count);

            for (int i = 0; i < expectedOutput.Attributes.Count; i++)
            {
                TagHelperAttribute expectedAttribute = expectedOutput.Attributes[i];
                TagHelperAttribute actualAttribute = output.Attributes[i];
                Assert.Equal(expectedAttribute.Name, actualAttribute.Name);
                Assert.Equal(expectedAttribute.Value.ToString(), actualAttribute.Value.ToString(), ignoreCase: true);
            }
        }

        private HmacTokenTagHelper GetHelper(
            IUrlHelperFactory urlHelperFactory = null,
            ViewContext viewContext = null)
        {
            urlHelperFactory ??= new FakeUrlHelperFactory();
            viewContext ??= MakeViewContext();

            return new HmacTokenTagHelper(
                this.Provider.GetRequiredService<IOptions<ImageSharpMiddlewareOptions>>(),
                this.Provider.GetRequiredService<RequestAuthorizationUtilities>(),
                urlHelperFactory,
                new HtmlTestEncoder())
            {
                ViewContext = viewContext,
            };
        }
    }
}
