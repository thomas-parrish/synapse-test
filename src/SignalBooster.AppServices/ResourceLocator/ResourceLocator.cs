using System.Reflection;

namespace SignalBooster.AppServices.ResourceLocator;

/// <summary>
/// Provides a fluent API for locating and reading embedded resources from an assembly.
/// </summary>
public static class Resource
{
    /// <summary>
    /// Creates a <see cref="ResourceLocator"/> rooted at the namespace of the given <see cref="Type"/>.
    /// </summary>
    /// <param name="type">
    /// A type in the target namespace. Its <see cref="Assembly"/> and <see cref="Type.Namespace"/> 
    /// are used to resolve resource names.
    /// </param>
    /// <returns>
    /// A <see cref="ResourceLocator"/> instance for locating resources relative to the type’s namespace.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <c>null</c>.</exception>
    public static ResourceLocator FromNamespaceOf(Type type) => type == null
        ? throw new ArgumentNullException(nameof(type))
        : new ResourceLocator(type.Assembly, type.Namespace ?? string.Empty);

    /// <summary>
    /// Encapsulates logic for resolving and reading embedded resources from an assembly.
    /// </summary>
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

        /// <summary>
        /// Specifies the resource file name to locate.  
        /// The provided file name will be combined with the base namespace to form the resource name.
        /// </summary>
        /// <param name="fileName">The resource file name (e.g., <c>"Samples.sample.txt"</c>).</param>
        /// <returns>The current <see cref="ResourceLocator"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="fileName"/> is null, empty, or whitespace.</exception>
        public ResourceLocator WithName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(nameof(fileName));
            }
            _fileName = fileName;
            return this;
        }

        /// <summary>
        /// Reads the specified embedded resource into a string asynchronously.
        /// </summary>
        /// <returns>
        /// A task resolving to the resource contents as a string.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="WithName"/> has not been called before <see cref="ReadAsync"/>.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown if the specified resource cannot be found in the assembly manifest.
        /// </exception>
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
