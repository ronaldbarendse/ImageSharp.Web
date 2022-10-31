// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.Processors;
using SixLabors.ImageSharp.Web.TagHelpers;
using SixLabors.ImageSharp.Web.Tests.TestUtilities;
using Xunit;

namespace SixLabors.ImageSharp.Web.Tests.TagHelpers
{
    public sealed class ImageTagHelperTests : TagHelperTestBase
    {
        public ImageTagHelperTests()
            : base(_ => { })
        {
        }

        [Theory]
        [InlineData(null, "test.jpg", "test.jpg")]
        [InlineData("abcd.jpg", "test.jpg", "test.jpg")]
        [InlineData(null, "~/test.jpg", "virtualRoot/test.jpg")]
        [InlineData("abcd.jpg", "~/test.jpg", "virtualRoot/test.jpg")]
        public void Process_SrcDefaultsToTagHelperOutputSrcAttributeAddedByOtherTagHelper(
            string src,
            string srcOutput,
            string expectedSrcPrefix)
        {
            // Arrange
            var allAttributes = new TagHelperAttributeList(
                new TagHelperAttributeList
                {
                    { "alt", new HtmlString("Testing") },
                    { "width", 100 },
                });
            TagHelperContext context = MakeTagHelperContext(allAttributes);
            var outputAttributes = new TagHelperAttributeList
                {
                    { "alt", new HtmlString("Testing") },
                    { "src", srcOutput },
                };
            var output = new TagHelperOutput(
                "img",
                outputAttributes,
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                    new DefaultTagHelperContent()));

            ImageTagHelper helper = this.GetHelper();
            helper.Src = src;
            helper.Width = 100;

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(
                expectedSrcPrefix + "?width=100",
                (string)output.Attributes["src"].Value,
                StringComparer.Ordinal);
        }

        [Fact]
        public void PreservesOrderOfSourceAttributesWhenRun()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "alt", new HtmlString("alt text") },
                    { "data-extra", new HtmlString("something") },
                    { "title", new HtmlString("Image title") },
                    { "src", "testimage.png" },
                    { "width", 50 },
                    { "height", 60 }
                });

            TagHelperOutput output = MakeImageTagHelperOutput(
                attributes: new TagHelperAttributeList
                {
                    { "alt", new HtmlString("alt text") },
                    { "data-extra", new HtmlString("something") },
                    { "title", new HtmlString("Image title") },
                    { "width", 50 },
                    { "height", 60 }
                });

            TagHelperOutput expectedOutput = MakeImageTagHelperOutput(
                attributes: new TagHelperAttributeList
                {
                    { "alt", new HtmlString("alt text") },
                    { "data-extra", new HtmlString("something") },
                    { "title", new HtmlString("Image title") },
                    { "src", "testimage.png?width=100&height=120" },
                    { "width", 50 },
                    { "height", 60 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.Width = 100;
            helper.Height = 120;

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(expectedOutput.TagName, output.TagName);
            Assert.Equal(6, output.Attributes.Count);

            for (int i = 0; i < expectedOutput.Attributes.Count; i++)
            {
                TagHelperAttribute expectedAttribute = expectedOutput.Attributes[i];
                TagHelperAttribute actualAttribute = output.Attributes[i];
                Assert.Equal(expectedAttribute.Name, actualAttribute.Name);
                Assert.Equal(expectedAttribute.Value.ToString(), actualAttribute.Value.ToString());
            }
        }

        [Fact]
        public void RendersImageTag_AddsAttributes_WithRequestPathBase()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "alt", new HtmlString("alt text") },
                    { "src", "/bar/images/image.jpg" },
                    { "width", 100 },
                    { "height", 200 },
                });
            TagHelperOutput output = MakeImageTagHelperOutput(attributes: new TagHelperAttributeList
            {
                { "alt", new HtmlString("alt text") },
            });

            ViewContext viewContext = MakeViewContext("/bar");

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "/bar/images/image.jpg";
            helper.Width = 100;
            helper.Height = 200;

            // Act
            helper.Process(context, output);

            // Assert
            Assert.True(output.Content.GetContent().Length == 0);
            Assert.Equal("img", output.TagName);
            Assert.Equal(4, output.Attributes.Count);
            TagHelperAttribute srcAttribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("src"));
            Assert.Equal("/bar/images/image.jpg?width=100&height=200", srcAttribute.Value);
        }

        [Fact]
        public void RendersImageTag_SrcIncludes_ResizeMode()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?width=100&{ResizeWebProcessor.Mode}={nameof(ResizeMode.Stretch)}" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.Width = 100;
            helper.ResizeMode = ResizeMode.Stretch;

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

        [Fact]
        public void RendersImageTag_SrcIncludes_ResizePosition()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?width=100&{ResizeWebProcessor.Xy}=20,50" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.Width = 100;
            helper.Center = new(20, 50);

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

        [Fact]
        public void RendersImageTag_SrcIncludes_ResizeAnchor()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?width=100&{ResizeWebProcessor.Anchor}={nameof(AnchorPositionMode.Top)}" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.Width = 100;
            helper.AnchorPosition = AnchorPositionMode.Top;

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

        [Fact]
        public void RendersImageTag_SrcIncludes_ResizePadColor()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?width=100&{ResizeWebProcessor.Color}={Color.LimeGreen.ToHex()}" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.Width = 100;
            helper.PadColor = Color.LimeGreen;

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

        [Fact]
        public void RendersImageTag_SrcIncludes_ResizeCompand()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?width=100&{ResizeWebProcessor.Compand}={bool.TrueString}" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.Width = 100;
            helper.Compand = true;

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

        [Fact]
        public void RendersImageTag_SrcIncludes_ResizeOrient()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?width=100&{ResizeWebProcessor.Orient}={bool.TrueString}" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.Width = 100;
            helper.Orient = true;

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

        public static TheoryData<ResamplerCommand> ResamplerCommands { get; } = new()
        {
            { Resampler.Bicubic },
            { Resampler.Box },
            { Resampler.CatmullRom },
            { Resampler.Hermite },
            { Resampler.Lanczos2 },
            { Resampler.Lanczos3 },
            { Resampler.Lanczos5 },
            { Resampler.Lanczos8 },
            { Resampler.MitchellNetravali },
            { Resampler.NearestNeighbor },
            { Resampler.Robidoux },
            { Resampler.RobidouxSharp },
            { Resampler.Spline },
            { Resampler.Triangle },
            { Resampler.Welch }
        };

        [Theory]
        [MemberData(nameof(ResamplerCommands))]
        public void RendersImageTag_SrcIncludes_Resampler(ResamplerCommand resampler)
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?width=100&{ResizeWebProcessor.Sampler}={resampler.Name}" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.Width = 100;
            helper.Sampler = resampler;

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

        [Fact]
        public void RendersImageTag_SrcIncludes_AutoOrient()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?{AutoOrientWebProcessor.AutoOrient}={bool.TrueString}" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.AutoOrient = true;

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

        public static TheoryData<FormatCommand> FormatCommands { get; } = new()
        {
            { Format.Bmp },
            { Format.Gif },
            { Format.Jpg },
            { Format.Png },
            { Format.Tga },
            { Format.WebP }
        };

        [Theory]
        [MemberData(nameof(FormatCommands))]
        public void RendersImageTag_SrcIncludes_Format(FormatCommand format)
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?{FormatWebProcessor.Format}={format.Name}" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.Format = format;

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

        [Fact]
        public void RendersImageTag_SrcIncludes_BackgroundColor()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?{BackgroundColorWebProcessor.Color}={Color.Red.ToHex()}" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.BackgroundColor = Color.Red;

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

        [Fact]
        public void RendersImageTag_SrcIncludes_Quality()
        {
            // Arrange
            TagHelperContext context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "src", "testimage.png" },
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
                    { "src", $"testimage.png?{QualityWebProcessor.Quality}={42}" },
                    { "width", 50 }
                });

            ImageTagHelper helper = this.GetHelper();
            helper.Src = "testimage.png";
            helper.Quality = 42;

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

        private ImageTagHelper GetHelper(
            IUrlHelperFactory urlHelperFactory = null,
            ViewContext viewContext = null)
        {
            urlHelperFactory ??= new FakeUrlHelperFactory();
            viewContext ??= MakeViewContext();

            return new ImageTagHelper(
                this.Provider.GetRequiredService<IOptions<ImageSharpMiddlewareOptions>>(),
                urlHelperFactory,
                new HtmlTestEncoder())
            {
                ViewContext = viewContext,
            };
        }
    }
}
