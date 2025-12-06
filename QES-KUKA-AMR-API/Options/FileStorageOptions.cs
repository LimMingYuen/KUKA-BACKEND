namespace QES_KUKA_AMR_API.Options
{
    public class FileStorageOptions
    {
        public const string SectionName = "FileStorage";

        /// <summary>
        /// Base path for file uploads (e.g., "C:\var\kuka\uploads")
        /// </summary>
        public string UploadsPath { get; set; } = string.Empty;

        /// <summary>
        /// Subfolder for map background images (e.g., "maps")
        /// </summary>
        public string MapsFolder { get; set; } = "maps";
    }
}
