using System.Reflection;

namespace SignalBooster.AppServices.ResourceLocator;

public static class Resource
{
    public static ResourceLocator FromNamespaceOf(Type type) =>
        new ResourceLocator(type.Assembly, type.Namespace ?? string.Empty);

    public sealed class ResourceLocator
    {
        private readonly Assembly _assembly;
        private readonly string _baseNamespace;
        private string? _fileName;

        internal ResourceLocator(Assembly assembly, string baseNamespace)
        {
            _assembly = assembly;
            _baseNamespace = baseNamespace;
        }

        public ResourceLocator WithName(string fileName)
        {
            _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            return this;
        }

        public async Task<string> ReadAsync()
        {
            if (_fileName == null)
            {
                throw new InvalidOperationException("You must call WithName before ReadAsync.");
            }

            // Resource names are like "<Namespace>.<Folder>.<FileName>"
            var resourceName = $"{_baseNamespace}.{_fileName.Replace(Path.DirectorySeparatorChar, '.')}";

            await using var stream = _assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }
}
