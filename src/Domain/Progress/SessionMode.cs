namespace Spine.Domain.Progress;

/// <summary>How a session picks its questions: regular (due, then new) or weak (last answer wrong).</summary>
public enum SessionMode
{
    Regular,
    Weak,
}
