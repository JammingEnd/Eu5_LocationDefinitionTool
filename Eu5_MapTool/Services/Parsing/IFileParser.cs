using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eu5_MapTool.Services.Parsing;

/// <summary>
/// Base interface for all file format parsers.
/// Provides abstraction for reading and parsing EU5 game files.
/// </summary>
public interface IFileParser<TResult>
{
    /// <summary>
    /// Parse a file and return the structured result.
    /// </summary>
    /// <param name="filePath">Path to the file to parse</param>
    /// <returns>Parsed data structure</returns>
    Task<TResult> ParseFileAsync(string filePath);

    /// <summary>
    /// Parse multiple files and return combined results.
    /// </summary>
    /// <param name="filePaths">Collection of file paths to parse</param>
    /// <returns>Combined parsed data</returns>
    Task<TResult> ParseFilesAsync(IEnumerable<string> filePaths);

    /// <summary>
    /// Parse file content from string array (lines).
    /// </summary>
    /// <param name="lines">File lines to parse</param>
    /// <returns>Parsed data structure</returns>
    TResult ParseLines(string[] lines);
}

/// <summary>
/// Base interface for file writers.
/// Provides abstraction for writing EU5 game files.
/// </summary>
public interface IFileWriter<TData>
{
    /// <summary>
    /// Write data to file in the appropriate format.
    /// </summary>
    /// <param name="filePath">Path to write to</param>
    /// <param name="data">Data to write</param>
    Task WriteFileAsync(string filePath, TData data);

    /// <summary>
    /// Serialize data to string array (lines).
    /// </summary>
    /// <param name="data">Data to serialize</param>
    /// <returns>File lines</returns>
    string[] SerializeToLines(TData data);
}
