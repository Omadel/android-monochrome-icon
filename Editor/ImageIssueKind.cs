namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Problems the monochrome layer can have, from the settings page to the build.
    /// </summary>
    internal enum ImageIssueKind
    {
        MissingImage,
        NotSingleChannelAlpha,
        NotPng,
        NotSquare,
        NotGrey,
        EmptyLayer,
        UnexpectedSize,
        MissingAdaptiveIcon
    }
}