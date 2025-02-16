// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.Bff.Tests.TestFramework;
using Duende.Bff.Tests.TestHosts;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Duende.Bff.Tests.Endpoints
{
    public class LocalEndpointTests(ITestOutputHelper output) : BffIntegrationTestBase(output)
    {
        [Fact]
        public async Task calls_to_authorized_local_endpoint_should_succeed()
        {
            await BffHost.BffLoginAsync("alice");

            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/local_authz"));
            req.Headers.Add("x-csrf", "1");
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.IsSuccessStatusCode.ShouldBeTrue();
            response.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
            var json = await response.Content.ReadAsStringAsync();
            var apiResult = JsonSerializer.Deserialize<ApiResponse>(json);
            apiResult.Method.ShouldBe("GET");
            apiResult.Path.ShouldBe("/local_authz");
            apiResult.Sub.ShouldBe("alice");
        }
        
        [Fact]
        public async Task calls_to_authorized_local_endpoint_without_csrf_should_succeed_without_antiforgery_header()
        {
            await BffHost.BffLoginAsync("alice");

            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/local_authz_no_csrf"));
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.IsSuccessStatusCode.ShouldBeTrue();
            response.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
            var json = await response.Content.ReadAsStringAsync();
            var apiResult = JsonSerializer.Deserialize<ApiResponse>(json);
            apiResult.Method.ShouldBe("GET");
            apiResult.Path.ShouldBe("/local_authz_no_csrf");
            apiResult.Sub.ShouldBe("alice");
        }
        
        [Fact]
        public async Task unauthenticated_calls_to_authorized_local_endpoint_should_fail()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/local_authz"));
            req.Headers.Add("x-csrf", "1");
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task calls_to_local_endpoint_should_require_antiforgery_header()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/local_anon"));
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
        
        [Fact]
        public async Task calls_to_local_endpoint_without_csrf_should_not_require_antiforgery_header()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/local_anon_no_csrf"));
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task calls_to_anon_endpoint_should_allow_anonymous()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/local_anon"));
            req.Headers.Add("x-csrf", "1");
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.IsSuccessStatusCode.ShouldBeTrue();
            response.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
            var json = await response.Content.ReadAsStringAsync();
            var apiResult = JsonSerializer.Deserialize<ApiResponse>(json);
            apiResult.Method.ShouldBe("GET");
            apiResult.Path.ShouldBe("/local_anon");
            apiResult.Sub.ShouldBeNull();
        }

        [Fact]
        public async Task put_to_local_endpoint_should_succeed()
        {
            await BffHost.BffLoginAsync("alice");

            var req = new HttpRequestMessage(HttpMethod.Put, BffHost.Url("/local_authz"));
            req.Headers.Add("x-csrf", "1");
            req.Content = new StringContent(JsonSerializer.Serialize(new TestPayload("hello test api")), Encoding.UTF8, "application/json");
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.IsSuccessStatusCode.ShouldBeTrue();
            response.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
            var json = await response.Content.ReadAsStringAsync();
            var apiResult = JsonSerializer.Deserialize<ApiResponse>(json);
            apiResult.Method.ShouldBe("PUT");
            apiResult.Path.ShouldBe("/local_authz");
            apiResult.Sub.ShouldBe("alice");
            var body = JsonSerializer.Deserialize<TestPayload>(apiResult.Body);
            body.Message.ShouldBe("hello test api");
        }

        [Fact]
        public async Task unauthenticated_non_bff_endpoint_should_return_302_for_login()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/always_fail_authz_non_bff_endpoint"));
            req.Headers.Add("x-csrf", "1");
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            response.Headers.Location.ToString().ToLowerInvariant().ShouldStartWith(IdentityServerHost.Url("/connect/authorize"));
        }

        [Fact]
        public async Task unauthenticated_api_call_should_return_401()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/always_fail_authz"));
            req.Headers.Add("x-csrf", "1");
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
        
        [Fact]
        public async Task forbidden_api_call_should_return_403()
        {
            await BffHost.BffLoginAsync("alice");

            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/always_fail_authz"));
            req.Headers.Add("x-csrf", "1");
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task challenge_response_should_return_401()
        {
            await BffHost.BffLoginAsync("alice");
            BffHost.LocalApiResponseStatus = BffHost.ResponseStatus.Challenge;

            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/local_authz"));
            req.Headers.Add("x-csrf", "1");
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task forbid_response_should_return_403()
        {
            await BffHost.BffLoginAsync("alice");
            BffHost.LocalApiResponseStatus = BffHost.ResponseStatus.Forbid;

            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/local_authz"));
            req.Headers.Add("x-csrf", "1");
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task challenge_response_when_response_handling_skipped_should_trigger_redirect_for_login()
        {
            await BffHost.BffLoginAsync("alice");
            BffHost.LocalApiResponseStatus = BffHost.ResponseStatus.Challenge;

            var req = new HttpRequestMessage(HttpMethod.Get, BffHost.Url("/local_anon_no_csrf_no_response_handling"));
            var response = await BffHost.BrowserClient.SendAsync(req);

            response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        }


        [Fact]
        public async Task fallback_policy_should_not_fail()
        {
            BffHost.OnConfigureServices += svcs =>
            {
                svcs.AddAuthorization(opts =>
                { 
                    opts.FallbackPolicy = 
                        new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                });
            };
            await BffHost.InitializeAsync();

            var response = await BffHost.HttpClient.GetAsync(BffHost.Url("/not-found"));
            response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        }
    }
}
