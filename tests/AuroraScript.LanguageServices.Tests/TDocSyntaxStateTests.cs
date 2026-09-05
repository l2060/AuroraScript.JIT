using AuroraScript.VisualStudio.Language;

namespace AuroraScript.LanguageServices.Tests;

public sealed class TDocSyntaxStateTests
{
    [Fact]
    public void ConfigMembersAndNativeTypesHaveDistinctRoles()
    {
        var state = new TDocSyntaxState();
        Assert.Equal(TDocTokenRole.Type, state.Identifier("Object", false));
        state.Punctuation('{');
        Assert.Equal(TDocTokenRole.Type, state.Identifier("Vec2", true));
        Assert.Equal(TDocTokenRole.MapKey, state.Identifier("vec", false));
        state.Punctuation('{');
        Assert.Equal(TDocTokenRole.MapKey, state.Identifier("x", false));
        state.Punctuation('}');
        state.Punctuation(',');
        // Comments are skipped by the tagger, without changing token context.
        Assert.Equal(TDocTokenRole.MapKey, state.Identifier("numbers", false));
        state.Punctuation('{');
        Assert.Equal(TDocTokenRole.Type, state.Identifier("Int64", true));
        Assert.Equal(TDocTokenRole.MapKey, state.Identifier("i64", false));
        state.Punctuation(',');
        Assert.Equal(TDocTokenRole.Type, state.Identifier("UInt64", true));
        Assert.Equal(TDocTokenRole.MapKey, state.Identifier("ui64", false));
    }

    [Fact]
    public void ArrayTypePrefixesAreNotMistakenForObjectKeys()
    {
        var state = new TDocSyntaxState();
        state.Punctuation('[');
        Assert.Equal(TDocTokenRole.Type, state.Identifier("Int64", false));
        state.Punctuation(',');
        Assert.Equal(TDocTokenRole.Type, state.Identifier("Vec2", false));
        state.Punctuation('{');
        Assert.Equal(TDocTokenRole.Keyword, state.Identifier("readonly", true));
        Assert.Equal(TDocTokenRole.Type, state.Identifier("String", true));
        Assert.True(state.TakeKey()); // A quoted property name.
        Assert.False(state.TakeKey()); // Its string value.
        state.Punctuation(',');
        Assert.Equal(TDocTokenRole.MapKey, state.Identifier("enabled", false));
        Assert.Equal(TDocTokenRole.None, state.Identifier("true", false));
        state.Punctuation('}');
        state.Punctuation(',');
        Assert.Equal(TDocTokenRole.Type, state.Identifier("UInt64", false));
        state.Punctuation(']');
    }

    [Fact]
    public void TypeNamesCanAlsoBePropertyNames()
    {
        var state = new TDocSyntaxState();
        state.Punctuation('{');
        Assert.Equal(TDocTokenRole.MapKey, state.Identifier("Int64", false));
        state.Punctuation(',');
        Assert.Equal(TDocTokenRole.MapKey, state.Identifier("String", false));
        Assert.False(state.TakeKey());
    }
}
