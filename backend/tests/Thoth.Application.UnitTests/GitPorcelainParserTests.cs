using FluentAssertions;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Git;

namespace Thoth.Application.UnitTests;

public sealed class GitPorcelainParserTests
{
    [Fact]
    public void Parse_maps_common_status_codes()
    {
        var result = GitPorcelainParser.Parse(" M src/app.ts\0A  src/new.ts\0 D src/deleted.ts\0?? docs/readme.md\0");

        result.Should().BeEquivalentTo(
        [
            new GitFileStatusDto("src/app.ts", GitFileChangeStatus.Modified),
            new GitFileStatusDto("src/new.ts", GitFileChangeStatus.Added),
            new GitFileStatusDto("src/deleted.ts", GitFileChangeStatus.Deleted),
            new GitFileStatusDto("docs/readme.md", GitFileChangeStatus.Untracked)
        ]);
    }

    [Fact]
    public void Parse_handles_rename_z_order_and_keeps_following_entries_aligned()
    {
        var result = GitPorcelainParser.Parse("R  src/new-name.ts\0src/old-name.ts\0 M src/next.ts\0");

        result.Should().BeEquivalentTo(
        [
            new GitFileStatusDto("src/new-name.ts", GitFileChangeStatus.Renamed, "src/old-name.ts"),
            new GitFileStatusDto("src/next.ts", GitFileChangeStatus.Modified)
        ]);
    }

    [Fact]
    public void Parse_maps_conflicts_to_modified_and_skips_ignored_or_malformed_entries()
    {
        var result = GitPorcelainParser.Parse("UU src/conflito-acao.ts\0!! ignored.txt\0bad\0 M src/servico-é.ts\0");

        result.Should().BeEquivalentTo(
        [
            new GitFileStatusDto("src/conflito-acao.ts", GitFileChangeStatus.Modified),
            new GitFileStatusDto("src/servico-é.ts", GitFileChangeStatus.Modified)
        ]);
    }

    [Fact]
    public void Parse_preserves_leading_and_trailing_spaces_in_z_paths()
    {
        var result = GitPorcelainParser.Parse(" M  leading.txt\0 M trailing.txt \0");

        result.Should().BeEquivalentTo(
        [
            new GitFileStatusDto(" leading.txt", GitFileChangeStatus.Modified),
            new GitFileStatusDto("trailing.txt ", GitFileChangeStatus.Modified)
        ]);
    }

    [Fact]
    public void Parse_empty_output_returns_empty_list()
    {
        GitPorcelainParser.Parse(string.Empty).Should().BeEmpty();
    }
}
