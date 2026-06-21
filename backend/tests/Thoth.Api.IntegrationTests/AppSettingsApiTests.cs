using System.Net.Http.Json;
using FluentAssertions;
using Thoth.Application.Common.Models;

namespace Thoth.Api.IntegrationTests;

public sealed class AppSettingsApiTests(ThothApiFactory factory) : IClassFixture<ThothApiFactory>
{
    [Fact]
    public async Task App_settings_can_be_read_and_updated()
    {
        var client = factory.CreateClient();

        var defaults = await client.GetFromJsonAsync<AppSettingsDto>("/api/app-settings");
        defaults.Should().NotBeNull();
        defaults!.ShowAgentTerminalOfferAfterChildPrompt.Should().BeTrue();

        var response = await client.PutAsJsonAsync(
            "/api/app-settings",
            new { ShowAgentTerminalOfferAfterChildPrompt = false });
        response.EnsureSuccessStatusCode();

        var updated = await client.GetFromJsonAsync<AppSettingsDto>("/api/app-settings");
        updated.Should().NotBeNull();
        updated!.ShowAgentTerminalOfferAfterChildPrompt.Should().BeFalse();
    }
}
