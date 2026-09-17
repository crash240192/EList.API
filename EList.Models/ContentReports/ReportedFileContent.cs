namespace EList.Models.ContentReports
{
    /// <summary>
    /// Binary payload for staff proxy download of a reported (possibly Blocked) file.
    /// </summary>
    public class ReportedFileContent
    {
        public byte[] Content { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}
