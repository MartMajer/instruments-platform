using Platform.Domain.Responses;

namespace Platform.UnitTests.Domain;

public sealed class ResponseEntitiesTests
{
    [Fact]
    public void Response_session_starts_for_assignment_and_locale()
    {
        var session = new ResponseSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " hr ");

        Assert.Equal("hr", session.Locale);
        Assert.NotNull(session.StartedAt);
        Assert.Null(session.SubmittedAt);
    }

    [Fact]
    public void Response_session_rejects_answer_mutation_after_submit()
    {
        var session = new ResponseSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en");

        session.Submit(DateTimeOffset.Parse("2026-05-07T12:00:00+00:00"), timeTakenMs: 1200);

        Assert.Throws<InvalidOperationException>(() => session.EnsureCanAcceptAnswers());
        Assert.Equal(1200, session.TimeTakenMs);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    public void Answer_rejects_invalid_json_values(string value)
    {
        Assert.Throws<ArgumentException>(() => new Answer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            value));
    }

    [Fact]
    public void Answer_allows_json_scalar_or_object_values()
    {
        var scalar = new Answer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "4");
        var structured = new Answer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            """{"value":4,"label":"Agree"}""",
            comment: " Optional comment ");

        Assert.Equal("4", scalar.Value);
        Assert.Equal("""{"value":4,"label":"Agree"}""", structured.Value);
        Assert.Equal("Optional comment", structured.Comment);
    }

    [Fact]
    public void Answer_can_be_updated_while_session_is_open()
    {
        var session = new ResponseSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en");
        var answer = new Answer(
            Guid.NewGuid(),
            session.TenantId,
            session.Id,
            Guid.NewGuid(),
            "3");

        answer.UpdateValue(session, "5", comment: "Changed", isSkipped: false, isNa: false);

        Assert.Equal("5", answer.Value);
        Assert.Equal("Changed", answer.Comment);
    }
}
