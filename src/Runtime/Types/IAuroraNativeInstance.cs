namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Marks a generated native-type instance so ordinary CLR reflection fallback
    /// does not expose members that lack <c>AuroraExport</c>.
    /// </summary>
    public interface IAuroraNativeInstance
    {
    }
}
