using System;
using System.Text;

namespace eQuantic.Linq.SourceGenerator;

/// <summary>
/// Represents the type of indentation to use in code generation.
/// </summary>
public enum IndentationType
{
    /// <summary>
    /// Use tab characters for indentation.
    /// </summary>
    Tabs,
    
    /// <summary>
    /// Use 4 spaces for each indentation level.
    /// </summary>
    Spaces
}

/// <summary>
/// Utility class for writing formatted code with proper indentation.
/// Based on eQuantic.Mapper.Generator.CodeWriter
/// </summary>
internal class CodeWriter
{
    /// <summary>
    /// Gets the content being written.
    /// </summary>
    private StringBuilder Content { get; } = new();
    
    /// <summary>
    /// Initializes a new instance of the CodeWriter class.
    /// </summary>
    /// <param name="indentLevel">The initial indentation level.</param>
    /// <param name="indentationType">The type of indentation to use (tabs or spaces).</param>
    public CodeWriter(int indentLevel = 0, IndentationType indentationType = IndentationType.Spaces)
    {
        IndentLevel = indentLevel;
        IndentationType = indentationType;
        InternalScopeTracker = new ScopeTracker(this); //We only need one. It can be reused.
    }

    /// <summary>
    /// Gets or sets the current indentation level.
    /// </summary>
    public int IndentLevel { get; set; }
    
    /// <summary>
    /// Gets the type of indentation being used.
    /// </summary>
    public IndentationType IndentationType { get; private set; }
    
    /// <summary>
    /// Gets the internal scope tracker for managing code blocks.
    /// </summary>
    private ScopeTracker InternalScopeTracker { get; } //We only need one. It can be reused.
    
    /// <summary>
    /// Gets the indentation string for the current level.
    /// </summary>
    private string GetIndentation()
    {
        var level = Math.Max(0, IndentLevel); // Protect against negative values
        return IndentationType == IndentationType.Spaces 
            ? new string(' ', level * 4) 
            : new string('\t', level);
    }
    
    /// <summary>
    /// Appends text to the current line without adding a new line.
    /// </summary>
    /// <param name="line">The text to append.</param>
    /// <param name="args">Optional format arguments.</param>
    public void Append(string line, params object[] args) => 
        Content.Append(args?.Length > 0 ? string.Format(line, args) : line);

    /// <summary>
    /// Appends a line of text with proper indentation.
    /// </summary>
    /// <param name="line">The line of text to append.</param>
    /// <param name="args">Optional format arguments.</param>
    public void AppendLine(string line, params object[] args) => 
        Content.Append(GetIndentation()).AppendLine(args?.Length > 0 ? string.Format(line, args) : line);
    
    /// <summary>
    /// Appends an empty line.
    /// </summary>
    public void AppendLine() => Content.AppendLine();

    /// <summary>
    /// Appends XML documentation summary with automatic line wrapping.
    /// </summary>
    /// <param name="summary">The summary text.</param>
    /// <param name="maxLineLength">Maximum characters per line before wrapping (default: 80).</param>
    public void AppendXmlDocSummary(string summary, int maxLineLength = 80)
    {
        AppendLine("/// <summary>");
        
        // Calculate available space after "/// " prefix and current indentation
        var indentLength = GetIndentation().Length;
        const int prefixLength = 4; // "/// ".Length
        var availableLength = maxLineLength - indentLength - prefixLength;
        
        // If summary fits in one line, use it as is
        if (summary.Length <= availableLength)
        {
            AppendLine($"/// {summary}");
        }
        else
        {
            // Split summary into words and wrap lines
            var words = summary.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = new StringBuilder();
            
            foreach (var word in words)
            {
                // Check if adding this word would exceed the line limit
                var testLine = currentLine.Length == 0 ? word : $"{currentLine} {word}";
                
                if (testLine.Length <= availableLength)
                {
                    currentLine = new StringBuilder(testLine);
                }
                else
                {
                    // Output current line and start a new one
                    if (currentLine.Length > 0)
                    {
                        AppendLine($"/// {currentLine}");
                        currentLine = new StringBuilder(word);
                    }
                    else
                    {
                        // Single word is too long, output it anyway to avoid infinite loop
                        AppendLine($"/// {word}");
                    }
                }
            }
            
            // Output the last line if it has content
            if (currentLine.Length > 0)
            {
                AppendLine($"/// {currentLine}");
            }
        }
        
        AppendLine("/// </summary>");
    }

    /// <summary>
    /// Appends XML documentation parameter.
    /// </summary>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="description">The parameter description.</param>
    public void AppendXmlDocParam(string paramName, string description)
    {
        AppendLine($"/// <param name=\"{paramName}\">{description}</param>");
    }

    /// <summary>
    /// Appends XML documentation returns.
    /// </summary>
    /// <param name="description">The return description.</param>
    public void AppendXmlDocReturns(string description)
    {
        AppendLine($"/// <returns>{description}</returns>");
    }
    
    /// <summary>
    /// Begins a new scope with the specified line and returns a disposable scope tracker.
    /// </summary>
    /// <param name="line">The line to write before opening the scope.</param>
    /// <param name="args">Optional format arguments.</param>
    /// <returns>A disposable scope tracker.</returns>
    public IDisposable BeginScope(string line, params object[] args)
    {
        AppendLine(line, args);
        return BeginScope();
    }
    
    /// <summary>
    /// Begins a new scope and returns a disposable scope tracker.
    /// </summary>
    /// <returns>A disposable scope tracker.</returns>
    public IDisposable BeginScope()
    {
        Content.Append(GetIndentation()).AppendLine("{");
        IndentLevel += 1;
        return InternalScopeTracker;
    }

    /// <summary>
    /// Ends the current line.
    /// </summary>
    public void EndLine() => Content.AppendLine();

    /// <summary>
    /// Ends the current scope by decreasing indentation and adding a closing brace.
    /// </summary>
    public void EndScope()
    {
        IndentLevel = Math.Max(0, IndentLevel - 1); // Protect against negative values
        Content.Append(GetIndentation()).AppendLine("}");
    }

    /// <summary>
    /// Changes the indentation type.
    /// </summary>
    /// <param name="indentationType">The new indentation type.</param>
    public void SetIndentationType(IndentationType indentationType)
    {
        IndentationType = indentationType;
    }

    /// <summary>
    /// Starts a new line with proper indentation.
    /// </summary>
    public void StartLine() => Content.Append(GetIndentation());
    
    /// <summary>
    /// Returns the generated code as a string.
    /// </summary>
    /// <returns>The generated code.</returns>
    public override string ToString() => Content.ToString();

    /// <summary>
    /// Internal class for tracking code scopes and automatically closing them when disposed.
    /// </summary>
    private class ScopeTracker : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the ScopeTracker class.
        /// </summary>
        /// <param name="parent">The parent CodeWriter instance.</param>
        public ScopeTracker(CodeWriter parent)
        {
            Parent = parent;
        }
        
        /// <summary>
        /// Gets the parent CodeWriter instance.
        /// </summary>
        public CodeWriter Parent { get; }
        
        /// <summary>
        /// Releases resources and ends the scope.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Parent.EndScope();
            }
        }

        /// <summary>
        /// Releases all resources used by the ScopeTracker.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}