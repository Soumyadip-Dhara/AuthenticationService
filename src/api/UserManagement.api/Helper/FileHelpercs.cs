namespace UserManagement.Helper
{
    public class FileHelper
    {
        public static void CreateFolderIfNotExists(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"Folder does not exist: {folderPath}");
                Console.WriteLine($"Creating folder: {folderPath}");
                Directory.CreateDirectory(folderPath);
                Console.WriteLine($"Folder created: {folderPath}");
            }
        }
        public static void AppendToFile(string filePath, string content)
        {
            if (File.Exists(filePath))
            {
                File.AppendAllText(filePath, content);
            }
            else
            {
                CreateFolderIfNotExists(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, content);
            }
        }
    }
}
