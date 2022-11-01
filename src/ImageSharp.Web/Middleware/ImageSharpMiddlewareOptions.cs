// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using SixLabors.ImageSharp.Web.Commands;
using SixLabors.ImageSharp.Web.Providers;

namespace SixLabors.ImageSharp.Web.Middleware
{
    /// <summary>
    /// Configuration options for the <see cref="ImageSharpMiddleware"/> middleware.
    /// </summary>
    public class ImageSharpMiddlewareOptions
    {
        private Func<ImageCommandContext, byte[], Task<string>> onComputeHMACAsync = (context, secret) =>
        {
            if (context.Commands.Count == 0)
            {
                // Skip HMAC when no commands are supplied in the request
                return Task.FromResult<string>(null);
            }

            // Default to SHA256 hash of relative, lowercased URL
            string uri = CaseHandlingUriBuilder.BuildRelative(
                 CaseHandlingUriBuilder.CaseHandling.LowerInvariant,
                 context.Context.Request.PathBase,
                 context.Context.Request.Path,
                 QueryString.Create(context.Commands));

            return Task.FromResult(HMACUtilities.ComputeHMACSHA256(uri, secret));
        };

        private Func<ImageCommandContext, Task> onParseCommandsAsync = _ => Task.CompletedTask;
        private Func<FormattedImage, Task> onBeforeSaveAsync = _ => Task.CompletedTask;
        private Func<ImageProcessingContext, Task> onProcessedAsync = _ => Task.CompletedTask;
        private Func<HttpContext, Task> onPrepareResponseAsync = _ => Task.CompletedTask;

        /// <summary>
        /// Gets or sets the base library configuration.
        /// </summary>
        public Configuration Configuration { get; set; } = Configuration.Default;

        /// <summary>
        /// Gets or sets the recyclable memorystream manager used for managing pooled stream
        /// buffers independently from image buffer pooling.
        /// </summary>
        public RecyclableMemoryStreamManager MemoryStreamManager { get; set; } = new RecyclableMemoryStreamManager();

        /// <summary>
        /// Gets or sets a value indicating whether to use culture-independent (invariant)
        /// conversion when converting commands.
        /// If set to <see langword="false"/> the <see cref="CommandParser"/> will use
        /// the <see cref="CultureInfo.CurrentCulture"/>.
        /// </summary>
        public bool UseInvariantParsingCulture { get; set; } = true;

        /// <summary>
        /// Gets or sets the duration to store images in the browser cache.
        /// If an image provider provides a Max-Age for a source image then that will override
        /// this value.
        /// <para>
        /// Defaults to 7 days.
        /// </para>
        /// </summary>
        public TimeSpan BrowserMaxAge { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Gets or sets the duration to store images in the image cache.
        /// <para>
        /// Defaults to 365 days.
        /// </para>
        /// </summary>
        public TimeSpan CacheMaxAge { get; set; } = TimeSpan.FromDays(365);

        /// <summary>
        /// Gets or sets the length of the filename to use (minus the extension) when storing
        /// images in the image cache. Defaults to 12 characters.
        /// </summary>
        public uint CacheHashLength { get; set; } = 12;

        /// <summary>
        /// Gets or sets the secret key for Hash-based Message Authentication Code (HMAC) encryption.
        /// </summary>
        /// <remarks>
        /// The key can be any length. However, the recommended size is at least 64 bytes. If the length is zero then no authentication is performed.
        /// </remarks>
        public byte[] HMACSecretKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the method used to compute a Hash-based Message Authentication Code (HMAC) for request authentication.
        /// Defaults to <see cref="HMACUtilities.ComputeHMACSHA256(string, byte[])"/> using an invariant lowercase relative Uri
        /// generated using <see cref="CaseHandlingUriBuilder.BuildRelative(CaseHandlingUriBuilder.CaseHandling, PathString, PathString, QueryString)"/>.
        /// </summary>
        public Func<ImageCommandContext, byte[], Task<string>> OnComputeHMACAsync
        {
            get => this.onComputeHMACAsync;

            set
            {
                Guard.NotNull(value, nameof(this.onComputeHMACAsync));
                this.onComputeHMACAsync = value;
            }
        }

        /// <summary>
        /// Gets or sets the additional command parsing method that can be used to used to augment commands.
        /// This is called once the commands have been gathered and before an <see cref="IImageProvider"/> has been assigned.
        /// </summary>
        public Func<ImageCommandContext, Task> OnParseCommandsAsync
        {
            get => this.onParseCommandsAsync;

            set
            {
                Guard.NotNull(value, nameof(this.OnParseCommandsAsync));
                this.onParseCommandsAsync = value;
            }
        }

        /// <summary>
        /// Gets or sets the additional method that can be used for final manipulation before the image is saved.
        /// This is called after image has been processed, but before the image has been saved to the output stream for caching.
        /// This can be used to alter the metadata of the resultant image.
        /// </summary>
        public Func<FormattedImage, Task> OnBeforeSaveAsync
        {
            get => this.onBeforeSaveAsync;

            set
            {
                Guard.NotNull(value, nameof(this.OnBeforeSaveAsync));
                this.onBeforeSaveAsync = value;
            }
        }

        /// <summary>
        /// Gets or sets the additional processing method.
        /// This is called after image has been processed, but before the result has been cached.
        /// This can be used to further optimize the resultant image.
        /// </summary>
        public Func<ImageProcessingContext, Task> OnProcessedAsync
        {
            get => this.onProcessedAsync;

            set
            {
                Guard.NotNull(value, nameof(this.OnProcessedAsync));
                this.onProcessedAsync = value;
            }
        }

        /// <summary>
        /// Gets or sets the additional response method.
        /// This is called after the status code and headers have been set, but before the body has been written.
        /// This can be used to add or change the response headers.
        /// </summary>
        public Func<HttpContext, Task> OnPrepareResponseAsync
        {
            get => this.onPrepareResponseAsync;

            set
            {
                Guard.NotNull(value, nameof(this.OnPrepareResponseAsync));
                this.onPrepareResponseAsync = value;
            }
        }
    }
}
