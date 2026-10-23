namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     A problem of the monochrome layer and its message. Errors keep the layer out of the build, or fail it if the
    ///     settings ask for it; warnings never stop it.
    /// </summary>
    internal readonly struct ImageIssue
    {
        public ImageIssue(ImageIssueKind kind, string message)
        {
            Kind = kind;
            Message = message;
        }

        public ImageIssueKind Kind { get; }
        public string Message { get; }
        public bool IsError => Kind != ImageIssueKind.UnexpectedSize && Kind != ImageIssueKind.MissingAdaptiveIcon;
    }
}