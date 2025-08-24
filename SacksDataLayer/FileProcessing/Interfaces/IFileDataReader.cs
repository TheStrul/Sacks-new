namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing
{
    public interface IFileDataReader
    {
        Task<FileData> ReadFileAsync(string fullPath);
    }
}