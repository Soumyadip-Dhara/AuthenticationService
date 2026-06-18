using System.Text;
using UserManagement.Helper;

namespace UserManagement.Helper
{
    public class CommonHelper
    {
        public static void SaveErrorLocally(string filePath, string data, string error)
        {
            string folderPath = Path.GetDirectoryName(filePath);
            FileHelper.CreateFolderIfNotExists(folderPath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string logFileName = $"{fileName}_{DateTime.Now:yyyyMMdd_HHmm}.text";
            string logPath = Path.Combine(folderPath, logFileName);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[DATA]------------------------------------");
            sb.AppendLine(data);
            sb.AppendLine("[ERROR]------------------------------------");
            sb.AppendLine(error);

            FileHelper.AppendToFile(logPath, sb.ToString());
        }
    }
}
