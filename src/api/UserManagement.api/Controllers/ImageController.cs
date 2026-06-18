using Microsoft.AspNetCore.Mvc;
using UserManagement.Filters;
using UserManagement.Helper;

namespace UserManagement.Controllers
{
    [Authorize("")]
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : Controller
    {

        public ImageController() { }
        //[HttpGet("get-image")]
        //public async Task<APIResponseClass<string>> GetImageFileStream(string fileName)
        //{

        //    APIResponseClass<string> response = new();
        //    try
        //    {
        //        var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

        //        var byteImageData = Base64(fileStream);

        //        Stream stream = new MemoryStream(byteImageData);
        //        var img = JsonConvert.SerializeObject(stream, Formatting.Indented, new MemoryStreamJsonConverter());
        //        //var img = JsonConvert.SerializeObject(File(imgText, "Image/Png"), Formatting.Indented, new MemoryStreamJsonConverter());
        //        response.apiResponseStatus = Enum.APIResponseStatus.Success;
        //        response.Message = "Captcha generate successfully.";
        //        response.result = img;
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //        response.Message = "Failed to retrieve image. Please try again.";
        //        return response;
        //    }
        //}
        [HttpGet("get-image-file-stream")]
        public async Task<APIResponseClass<string>> GetImageFileStream(string fileName)
        {
            APIResponseClass<string> response = new();
            try
            {
                // Get the file path
                //var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                //var filePath = Path.Combine(uploadsDir, fileName);

                // Check if the file exists
                if (!System.IO.File.Exists(fileName))
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Image not found.";
                    return response;
                }

                // Read the file as a byte array
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(fileName);

                // Convert to Base64
                string base64Image = Convert.ToBase64String(fileBytes);

                // Prepare the response
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Image retrieved successfully.";
                response.result = base64Image;
                return response;
            }
            catch (Exception ex)
            {
                // Log the error if necessary

                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Failed to retrieve image. Please try again. Error: {ex.Message}";
                return response;
            }
        }

        //[HttpGet("get-image")]
        //public async APIResponseClass<File> GetImage(string fileName)
        //{

        //    APIResponseClass<File> response = new();
        //    try
        //    {
        //        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
        //        var filePath = Path.Combine(uploadsDir, fileName);

        //        if (!System.IO.File.Exists(filePath))
        //        {
        //            response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //            response.Message = "Image Not Found";
        //            return response;
        //        }

        //        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        //        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

        //        var contentType = fileExtension switch
        //        {
        //            ".jpg" or ".jpeg" => "image/jpeg",
        //            ".png" => "image/png",
        //            ".gif" => "image/gif",
        //            ".bmp" => "image/bmp",
        //            _ => "application/octet-stream"
        //        };

        //        response.result = File(fileStream, contentType);
        //        response.Message = "Image Found";
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //        response.Message = "Failed, please try again.." + ex.Message;
        //        return response;
        //    }
        //}

        [HttpGet("get-image")]
        public IActionResult GetImage(string fileName)
        {
            try
            {
                //var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                //var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

                // Check if file exists
                if (!System.IO.File.Exists(fileName))
                {
                    return NotFound(new
                    {
                        Status = "Error",
                        Message = "Image Not Found"
                    });
                }

                // Determine content type
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                var contentType = fileExtension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".bmp" => "image/bmp",
                    _ => "application/octet-stream"
                };

                // Serve the file
                var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                return File(fileStream, contentType, "logo." + fileExtension); // Sends file directly as response
            }
            catch (Exception ex)
            {
                // Log error and return error response
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "Failed to retrieve image. Please try again.",
                    Details = ex.Message
                });
            }
        }


    }
}

