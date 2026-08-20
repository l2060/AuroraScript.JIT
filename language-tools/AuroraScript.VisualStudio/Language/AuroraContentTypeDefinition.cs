using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace AuroraScript.VisualStudio.Language;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "MEF composition fields are discovered by Visual Studio.")]
internal static class AuroraContentTypeDefinition
{
    public const string ContentTypeName = "aurorascript";
    public const string FileExtension = ".as";
    public const string TypedDocumentFileExtension = ".tdoc";

    [Export]
    [Name(ContentTypeName)]
    [BaseDefinition("code")]
    [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CS0649:Field is never assigned to", Justification = "MEF composition field.")]
    internal static ContentTypeDefinition? AuroraContentType;

    [Export]
    [FileExtension(FileExtension)]
    [ContentType(ContentTypeName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CS0649:Field is never assigned to", Justification = "MEF composition field.")]
    internal static FileExtensionToContentTypeDefinition? AuroraFileExtension;

    [Export]
    [FileExtension(TypedDocumentFileExtension)]
    [ContentType(ContentTypeName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CS0649:Field is never assigned to", Justification = "MEF composition field.")]
    internal static FileExtensionToContentTypeDefinition? AuroraTypedDocumentFileExtension;
}
