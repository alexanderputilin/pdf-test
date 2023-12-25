using System.Text.Json.Serialization;

namespace Pdf.Models;

public class FileTask
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EFileState
    {
        Queued,
        Done,
        Error
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public EFileState State { get; set; }
    public string DoneSet { get; set; } = null!;
    
}