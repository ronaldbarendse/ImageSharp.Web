// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web.Middleware;

namespace SixLabors.ImageSharp.Web.TagHelpers
{
    /// <summary>
    /// A tag helper implementation targeting &lt;img&gt; and &lt;source&gt; elements that allows the automatic generation of HMAC image processing protection tokens.
    /// </summary>
    [HtmlTargetElement("img", Attributes = SrcAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("img", Attributes = SrcSetAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("source", Attributes = SrcSetAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class HmacTokenTagHelper : UrlResolutionTagHelper
    {
        private const string SrcAttributeName = "src";
        private const string SrcSetAttributeName = "srcset";

        private readonly ImageSharpMiddlewareOptions options;
        private readonly RequestAuthorizationUtilities authorizationUtilities;

        /// <summary>
        /// Initializes a new instance of the <see cref="HmacTokenTagHelper" /> class.
        /// </summary>
        /// <param name="options">The middleware configuration options.</param>
        /// <param name="authorizationUtilities">Contains helpers that allow authorization of image requests.</param>
        /// <param name="urlHelperFactory">The URL helper factory.</param>
        /// <param name="htmlEncoder">The HTML encorder.</param>
        public HmacTokenTagHelper(
            IOptions<ImageSharpMiddlewareOptions> options,
            RequestAuthorizationUtilities authorizationUtilities,
            IUrlHelperFactory urlHelperFactory,
            HtmlEncoder htmlEncoder)
            : base(urlHelperFactory, htmlEncoder)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(authorizationUtilities, nameof(authorizationUtilities));

            this.options = options.Value;
            this.authorizationUtilities = authorizationUtilities;
        }

        /// <inheritdoc/>
        public override int Order => 1000;

        /// <summary>
        /// Gets or sets the source of the image.
        /// </summary>
        /// <remarks>
        /// Passed through to the generated HTML in all cases.
        /// </remarks>
        [HtmlAttributeName(SrcAttributeName)]
        public string Src { get; set; }

        /// <summary>
        /// Gets or sets the source set of the image.
        /// </summary>
        /// <remarks>
        /// Passed through to the generated HTML in all cases.
        /// </remarks>
        [HtmlAttributeName(SrcSetAttributeName)]
        public string SrcSet { get; set; }

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(output, nameof(output));

            if (context.AllAttributes.ContainsName(SrcAttributeName))
            {
                output.CopyHtmlAttribute(SrcAttributeName, context);
                this.ProcessUrlAttribute(SrcAttributeName, output);
            }

            if (context.AllAttributes.ContainsName(SrcSetAttributeName))
            {
                output.CopyHtmlAttribute(SrcSetAttributeName, context);
                this.ProcessUrlAttribute(SrcSetAttributeName, output);
            }

            byte[] secret = this.options.HMACSecretKey;
            if (secret is null || secret.Length == 0)
            {
                return;
            }

            // Retrieve the TagHelperOutput variation of the "src"/"srcset" attribute in case other TagHelpers in the
            // pipeline have touched the value. If the value is already encoded this HmacTokenTagHelper may
            // not function properly.
            string src = output.Attributes[SrcAttributeName]?.Value as string;
            if (!string.IsNullOrWhiteSpace(src))
            {
                string hmac = this.authorizationUtilities.ComputeHMAC(src, CommandHandling.Sanitize);
                if (hmac is not null)
                {
                    this.Src = AddQueryString(src, hmac);
                    output.Attributes.SetAttribute(SrcAttributeName, this.Src);
                }
            }

            string srcset = output.Attributes[SrcSetAttributeName]?.Value as string;
            if (!string.IsNullOrWhiteSpace(srcset))
            {
                string hmac = this.authorizationUtilities.ComputeHMAC(srcset, CommandHandling.Sanitize);
                if (hmac is not null)
                {
                    this.SrcSet = AddQueryString(srcset, hmac);
                    output.Attributes.SetAttribute(SrcSetAttributeName, this.SrcSet);
                }
            }
        }

        private static string AddQueryString(
            ReadOnlySpan<char> uri,
            string hmac)
        {
            ReadOnlySpan<char> uriToBeAppended = uri;
            ReadOnlySpan<char> anchorText = default;

            // If there is an anchor, then the query string must be inserted before its first occurrence.
            int anchorIndex = uri.IndexOf('#');
            if (anchorIndex != -1)
            {
                anchorText = uri.Slice(anchorIndex);
                uriToBeAppended = uri.Slice(0, anchorIndex);
            }

            int queryIndex = uriToBeAppended.IndexOf('?');
            bool hasQuery = queryIndex != -1;

            StringBuilder sb = new();
            sb.Append(uriToBeAppended);

            sb.Append(hasQuery ? '&' : '?');
            sb.Append(UrlEncoder.Default.Encode(RequestAuthorizationUtilities.TokenCommand));
            sb.Append('=');
            sb.Append(UrlEncoder.Default.Encode(hmac));

            sb.Append(anchorText);

            return sb.ToString();
        }
    }
}
