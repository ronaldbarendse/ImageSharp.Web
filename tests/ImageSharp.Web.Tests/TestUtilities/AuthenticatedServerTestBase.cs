// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace SixLabors.ImageSharp.Web.Tests.TestUtilities
{
    public abstract class AuthenticatedServerTestBase<TFixture> : ServerTestBase<TFixture>
         where TFixture : AuthenticatedTestServerFixture
    {
        private readonly RequestAuthorizationUtilities requestAuthorizationUtilities;
        private readonly string relativeImageSouce;

        protected AuthenticatedServerTestBase(TFixture fixture, ITestOutputHelper outputHelper, string imageSource)
            : base(fixture, outputHelper, imageSource)
        {
            this.requestAuthorizationUtilities =
                       this.Fixture.Services.GetRequiredService<RequestAuthorizationUtilities>();

            this.relativeImageSouce = this.ImageSource.Replace("http://localhost", string.Empty);
        }

        [Fact]
        public async Task CanRejectUnauthorizedRequestAsync()
        {
            string url = this.ImageSource;

            // Send an unaugmented request without a token.
            HttpResponseMessage response = await this.HttpClient.GetAsync(url + this.Fixture.Commands[0]);
            Assert.NotNull(response);
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // Now send an invalid token
            response = await this.HttpClient.GetAsync(url + this.Fixture.Commands[0] + "&" + RequestAuthorizationUtilities.TokenCommand + "=INVALID");
            Assert.NotNull(response);
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        protected override async Task<string> AugmentCommandAsync(string command)
        {
            string uri = this.relativeImageSouce + command;
            string token = await this.GetTokenAsync(uri);
            return command + "&" + RequestAuthorizationUtilities.TokenCommand + "=" + token;
        }

        private async Task<string> GetTokenAsync(string uri)
        {
            string tokenSync = this.requestAuthorizationUtilities.ComputeHMAC(uri, CommandHandling.Sanitize);
            string tokenAsync = await this.requestAuthorizationUtilities.ComputeHMACAsync(uri, CommandHandling.Sanitize);

            Assert.Equal(tokenSync, tokenAsync);
            return tokenSync;
        }
    }
}
