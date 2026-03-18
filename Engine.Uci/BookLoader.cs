using Engine.Core.Book.bins;

namespace Engine.Uci;

using System.Reflection;
using System.IO;

public static class BookLoader
{
    public static void LoadFromEmbedded()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = "Yris.Titans.bin"; // full namespace + filename

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException("Embedded book not found: " + resourceName);

        using var br = new BinaryReader(stream);
        OpeningBook.Load(br);
    }
}