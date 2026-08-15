namespace ChurchWebsite.Core;

public static class SpeakerFormatter
{
    /// <summary>
    /// Combines the (optional) <paramref name="title"/> and <paramref name="name"/>
    /// into three display variants used by the AI summary and the admin UI:
    ///
    /// <list type="bullet">
    ///   <item><description><c>FullFormal</c>  - <c>{Title}. {First} {Last}</c>  (e.g., "Elder. John Smith")</description></item>
    ///   <item><description><c>ShortFormal</c> - <c>{ShortTitle}. {Last}</c>   (e.g., "Eld. Smith")</description></item>
    ///   <item><description><c>ShortCasual</c> - <c>{ShortTitle} {First}</c>   (e.g., "Eld John")</description></item>
    /// </list>
    ///
    /// When no title is supplied, only <see cref="SpeakerVariants.FullFormal"/>
    /// is populated and equals the plain "First Last" name. Short forms come
    /// back as empty strings in that case. Abbreviations are looked up in
    /// <paramref name="titleAbbreviations"/>; unknown titles fall back to the
    /// typed value (stripped of any trailing period) so the short forms still
    /// read sensibly.
    /// </summary>
    public static SpeakerVariants Format(
        string? title,
        string name,
        IReadOnlyDictionary<string, string> titleAbbreviations)
    {
        var nameClean = (name ?? string.Empty).Trim();
        var titleClean = string.IsNullOrWhiteSpace(title) ? null : title.Trim();

        var tokens = nameClean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var first = tokens.Length > 0 ? tokens[0] : string.Empty;
        var last = tokens.Length > 1
            ? string.Join(' ', tokens, 1, tokens.Length - 1)
            : string.Empty;

        if (titleClean is null)
        {
            var plain = string.Join(' ', new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return new SpeakerVariants(plain, string.Empty, string.Empty);
        }

        var shortTitle = ResolveAbbreviation(titleClean, titleAbbreviations);
        var fullFormal = $"{(titleClean.TrimEnd(' ', '.'))}. {first} {last}".Trim();
        var shortFormal = string.IsNullOrEmpty(last)
            ? string.Empty
            : $"{shortTitle.TrimEnd(' ', '.')}. {last}";
        var shortCasual = $"{shortTitle.TrimEnd(' ', '.')} {first}".Trim();

        return new SpeakerVariants(fullFormal, shortFormal, shortCasual);
    }

    /// <summary>
    /// Returns an instruction block suitable for prepending to the summary
    /// prompt's user message. Empty when no relevant name/title is supplied.
    /// The LLM is told it MAY mention the speaker using any of the available
    /// forms but is never required to.
    /// </summary>
    public static string BuildSummaryHint(
        string? title,
        string name,
        IReadOnlyDictionary<string, string> titleAbbreviations)
    {
        var variants = Format(title, name, titleAbbreviations);
        if (string.IsNullOrWhiteSpace(variants.FullFormal))
        {
            return string.Empty;
        }

        var forms = new List<string> { $"'{variants.FullFormal}'" };
        if (!string.IsNullOrWhiteSpace(variants.ShortFormal))
        {
            forms.Add($"'{variants.ShortFormal}'");
        }
        if (!string.IsNullOrWhiteSpace(variants.ShortCasual))
        {
            forms.Add($"'{variants.ShortCasual}'");
        }

        return "When referring to the speaker above, you may use only one of these forms: "
            + string.Join(", ", forms)
            + ". Pick whichever fits the prose naturally; do not invent other forms and do not refer to the speaker by first name or last name alone.\n\n";
    }

    private static string ResolveAbbreviation(string title, IReadOnlyDictionary<string, string> abbreviations)
    {
        var trimmed = title.Trim();
        if (abbreviations.TryGetValue(trimmed, out var mapped))
        {
            return mapped;
        }

        var stripped = trimmed.TrimEnd('.', ' ');
        if (!string.IsNullOrEmpty(stripped) && abbreviations.TryGetValue(stripped, out mapped))
        {
            return mapped;
        }

        return string.IsNullOrEmpty(stripped) ? trimmed : stripped;
    }
}

public record SpeakerVariants(string FullFormal, string ShortFormal, string ShortCasual);
