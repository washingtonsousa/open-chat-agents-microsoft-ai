using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Options;

namespace OpenChatAgents.Infrastructure.BuiltInTools;

/// <summary>
/// Built-in "filesystem" tool group — read/write access sandboxed to a single configured root
/// directory (AppOptions.Filesystem.RootPath). Every path is resolved and canonicalized against
/// that root and rejected if it would escape it (no `..`/absolute-path traversal).
/// </summary>
public class FilesystemToolProvider(IOptions<AppOptions> options) : IBuiltInToolProvider
{
    private readonly string _root = Path.GetFullPath(options.Value.Filesystem.RootPath);

    public string Key => "filesystem";

    public IReadOnlyList<AITool> GetTools(BuiltInToolContext context) =>
    [
        AIFunctionFactory.Create(ListFilesAsync, new AIFunctionFactoryOptions
        {
            Name = "list_files",
            Description = "Lista os arquivos e pastas dentro do caminho informado (relativo à raiz permitida).",
        }),
        AIFunctionFactory.Create(ReadFileAsync, new AIFunctionFactoryOptions
        {
            Name = "read_file",
            Description = "Lê o conteúdo de texto de um arquivo dentro da pasta permitida.",
        }),
        AIFunctionFactory.Create(WriteFileAsync, new AIFunctionFactoryOptions
        {
            Name = "write_file",
            Description = "Cria ou sobrescreve um arquivo de texto dentro da pasta permitida.",
        }),
        AIFunctionFactory.Create(DeleteFileAsync, new AIFunctionFactoryOptions
        {
            Name = "delete_file",
            Description = "Exclui um arquivo dentro da pasta permitida.",
        }),
    ];

    private Task<string> ListFilesAsync([Description("Caminho relativo à raiz permitida; vazio para a raiz")] string path = "")
    {
        var fullPath = ResolveWithinRoot(path);
        if (!Directory.Exists(fullPath))
            return Task.FromResult($"A pasta '{path}' não existe.");

        var entries = Directory.EnumerateFileSystemEntries(fullPath)
            .Select(e => Path.GetRelativePath(_root, e) + (Directory.Exists(e) ? "/" : ""));
        return Task.FromResult(string.Join("\n", entries) is { Length: > 0 } list ? list : "(pasta vazia)");
    }

    private async Task<string> ReadFileAsync([Description("Caminho do arquivo, relativo à raiz permitida")] string path)
    {
        var fullPath = ResolveWithinRoot(path);
        if (!File.Exists(fullPath))
            return $"O arquivo '{path}' não existe.";
        return await File.ReadAllTextAsync(fullPath);
    }

    private async Task<string> WriteFileAsync(
        [Description("Caminho do arquivo, relativo à raiz permitida")] string path,
        [Description("Conteúdo de texto a gravar (sobrescreve o arquivo inteiro)")] string content)
    {
        var fullPath = ResolveWithinRoot(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content);
        return $"Arquivo '{path}' gravado ({content.Length} caracteres).";
    }

    private Task<string> DeleteFileAsync([Description("Caminho do arquivo, relativo à raiz permitida")] string path)
    {
        var fullPath = ResolveWithinRoot(path);
        if (!File.Exists(fullPath))
            return Task.FromResult($"O arquivo '{path}' não existe.");
        File.Delete(fullPath);
        return Task.FromResult($"Arquivo '{path}' excluído.");
    }

    private string ResolveWithinRoot(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath.TrimStart('/', '\\')));
        if (fullPath != _root && !fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Caminho fora da pasta permitida.");
        return fullPath;
    }
}
