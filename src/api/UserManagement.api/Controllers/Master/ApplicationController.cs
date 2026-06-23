using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;

using UserManagement.Helper;
using UserManagement.Models.DTO;
namespace UserManagement.Controllers.Master
{
    [Authorize("Super Admin,User Admin,Level Admin,Module Admin,IFMS USER")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ApplicationController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly IClaimService _claimService;
        public ApplicationController(IApplicationService applicationService, IClaimService claimService, IConfiguration config)
        {
            _applicationService = applicationService;
            _claimService = claimService;
        }

        [HttpGet("GetApplications")]
        public async Task<APIResponseClass<List<ApplicationGetDTO>>> GetApplications()
        {
            APIResponseClass<List<ApplicationGetDTO>> response = new();
            try
            {
                List<ApplicationGetDTO> applicationResult = new List<ApplicationGetDTO>();
                string[] userRoles = _claimService.GetRoles();
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    applicationResult = await _applicationService.GetAllApplications();
                }
                else
                {
                    userRoles[0] = userRoles[0].ToLower().Split(" ")[0];
                    applicationResult = await _applicationService.GetApplicationsByNames(userRoles);
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Data collected successfully";
                response.result = applicationResult;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpGet("GetApplicationsForMM")]
        public async Task<APIResponseClass<List<ApplicationGetDTO>>> GetApplicationsForMM()
        {
            APIResponseClass<List<ApplicationGetDTO>> response = new();
            try
            {
                List<ApplicationGetDTO> applicationResult = new List<ApplicationGetDTO>();
                string[] userRoles = _claimService.GetRoles();
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    applicationResult = await _applicationService.GetAllApplications();
                }
                else
                {
                    applicationResult = await _applicationService.GetApplicationByUserIdForMM();
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Data collected successfully";
                response.result = applicationResult;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }


        [HttpGet("GetApplicationsForUM")]
        public async Task<APIResponseClass<List<ApplicationGetDTO>>> GetApplicationsForUM(short isOwnOffice = 1)
        {
            APIResponseClass<List<ApplicationGetDTO>> response = new();
            try
            {
                List<ApplicationGetDTO> applicationResult = new List<ApplicationGetDTO>();
                string[] userRoles = _claimService.GetRoles();
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    applicationResult = await _applicationService.GetAllApplicationsForSuperAdmin();
                }
                else if (Array.Exists(userRoles, roleName => roleName.ToLower().Contains("module admin")))
                {
                    userRoles[0] = userRoles[0].ToLower().Split(" ")[0];
                    applicationResult = await _applicationService.GetApplicationsByNames(userRoles);
                }
                else if (Array.Exists(userRoles, roleName => roleName.ToLower().Contains("user admin")) && isOwnOffice == 2)
                {
                    userRoles[0] = userRoles[0].ToLower().Split(" ")[0];
                    applicationResult = await _applicationService.GetApplicationsByNames(userRoles);
                }
                else if (isOwnOffice == 1)
                {
                    applicationResult = await _applicationService.GetApplicationsByUserIdForUM(_claimService.GetUserId());
                }
                else
                {
                    applicationResult = await _applicationService.GetApplicationsByUserIdForUM(_claimService.GetUserId());
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Data collected successfully";
                response.result = applicationResult;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }


        [HttpPost("CreateNewApplication")]
        public async Task<APIResponseClass<bool>> CreateNewApplication([FromForm] ApplicationCreateDTO application, IFormFile logo)
        {
            APIResponseClass<bool> response = new();
            try
            {
                string photoUrl = "";

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                //string photoPath = "";
                if (logo != null)
                {
                    if (logo.ContentType != "image/jpeg" && logo.ContentType != "image/jpg" && logo.ContentType != "image/png")
                    {
                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.message = "Invalid File Format";
                        response.result = false;
                        return response;
                    }
                    photoUrl = Path.Combine(uploadsDir, Path.GetFileName(logo.FileName));
                    using (var stream = new FileStream(photoUrl, FileMode.Create))
                    {
                        await logo.CopyToAsync(stream);
                    }
                }

                //if (logo != null)
                //{
                //using (var stream = logo.OpenReadStream())
                //{
                //    var uploadParams = new ImageUploadParams()
                //    {
                //        File = new FileDescription(logo.FileName, stream),
                //        Folder = "wwwroot/uploads" // Specify folder name if needed
                //    };

                //    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                //    if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                //    {
                //        photoUrl = uploadResult.Url.ToString(); // Get the Cloudinary URL
                //    }
                //    else
                //    {
                //        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                //        response.Message = "Image upload failed.";
                //        return response;
                //    }
                //}
                //}

                // Store the URL in the database with the application data
                var res = await _applicationService.CreateApplication(application, photoUrl);
                if (res.Item1)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                }
                response.message = res.Item2;
                response.result = res.Item1;
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + ex.Message;
                return response;
            }
        }
        
        [HttpPost("UpdateApplication")]
        public async Task<APIResponseClass<string>> UpdateApplication([FromForm] ApplicationUpdateDTO application, IFormFile? logo)
        {
            APIResponseClass<string> response = new();
            try
            {
                string photoUrl = "";

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                if (logo != null)
                {
                    if(logo.ContentType != "image/jpeg" && logo.ContentType != "image/jpg" && logo.ContentType != "image/png")
                    {
                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.message = "Invalid File Format";
                        response.result = "Invalid File Format";
                        return response;
                    }
                    photoUrl = Path.Combine(uploadsDir, Path.GetFileName(logo.FileName));
                    using (var stream = new FileStream(photoUrl, FileMode.Create))
                    {
                        await logo.CopyToAsync(stream);
                    }
                }

                var res = await _applicationService.UpdateApplication(application, photoUrl);
                if (res.Item1)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                }
                response.message = res.Item2;
                response.result = res.Item3;
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + ex.Message;
                return response;
            }
        }

        [HttpDelete("DelteApplicationById")]
        public async Task<APIResponseClass<bool>> DeleteApplication(int AppId)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _applicationService.DeleteApplication(AppId);
                if (res.Item2)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = res.Item1;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = res.Item1;
                }
                response.result = res.Item2;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = Ex.Message;
                return response;
            }
        }

        [HttpGet("GetApplicationsByUserId")]
        public async Task<APIResponseClass<List<ApplicationFetchDTO>>> GetApplicationsByUserId()
        {
            APIResponseClass<List<ApplicationFetchDTO>> response = new();
            try
            {

                var applicationResult = await _applicationService.GetApplicationsByUserId(_claimService.GetUserId());
                if (applicationResult.Count != 0)
                {
                    response.message = "Data Found";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = applicationResult;

                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }
        [HttpGet("GetApplicationsForAppAccessByUserId")]
        public async Task<APIResponseClass<List<ApplicationFetchDTO>>> GetApplicationsForAppAccessByUserId()
        {
            APIResponseClass<List<ApplicationFetchDTO>> response = new();
            try
            {
                var applicationResult = await _applicationService.GetApplicationsForAppAccessByUserId(_claimService.GetUserId());
                if (applicationResult.Count != 0)
                {
                    response.message = "Data Found";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = applicationResult;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }
    }
}
