using Microsoft.AspNetCore.Mvc;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.Filters;
using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers.Master
{
    [Authorize("Super Admin,User Admin,Level Admin,Module Admin,IFMS USER")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ScopeController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IScopeService _scopeService;

        public ScopeController(IScopeService scopeService, IRoleService roleService, ILevelService levelService, IClaimService claimService)
        {
            _scopeService = scopeService;
            _claimService = claimService;
        }

        [HttpPost("InsertScopeData")]
        public async Task<APIResponseClass<bool>> InsertScopeData(List<ScopeDataInsertDTO> scopes)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _scopeService.InsertScopeData(scopes);
                if (res.Item1)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success; ;
                    response.message = res.Item2;
                    return response;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = res.Item2;
                }
                response.result = res.Item1;
                return response;
            }
            catch (Exception Ex)
            {
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Failed, please try again.." + Ex.Message;
                    return response;
                }

            }
        }
        [HttpPost("ImportScopesFromCSV")]
        public async Task<APIResponseClass<bool>> ImportScopesFromCSV(CSVScopeDataDTO scopes)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _scopeService.ImportScopesFromCSV(scopes);
                if (res.Item1)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success; ;
                    response.message = res.Item2;
                    return response;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = res.Item2;
                }
                response.result = res.Item1;
                return response;
            }
            catch (Exception Ex)
            {
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Failed, please try again.." + Ex.Message;
                    return response;
                }

            }
        }

        [HttpPost("CreateNewScope")]
        public async Task<APIResponseClass<bool>> CreateNewScope(ScopeCreateDTO scope)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _scopeService.CreateNewScope(scope);
                if (!res)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success; ;
                    response.message = "Scope Created successfully.";
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Failed, unable to create scope. Please try again..";
                }
                response.result = res;
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + ex.Message;
                return response;
            }
        }

        [HttpGet("GetScopesByLevelId")]
        public async Task<APIResponseClass<List<ScopeStructureDTO>>> GetScopesByLevelId(int levelId)
        {
            APIResponseClass<List<ScopeStructureDTO>> response = new();
            try
            {
                var Result = await _scopeService.GetScopesByLevelId(levelId);

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Scope Found";
                response.result = Result;

                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }

        }
        [HttpGet("GetScopesByLevelIdForViewOnly")]
        public async Task<APIResponseClass<ScopeReturnDTO>> GetScopesByLevelIdForViewOnly(int levelId, int first, int rows, string globalSearch = "")
        {
            APIResponseClass<ScopeReturnDTO> response = new();
            try
            {
                var res = await _scopeService.GetScopesByLevelIdForViewOnly(levelId, first, rows, globalSearch);
                if (res.TotalCount > 0)
                {
                    response.message = "Scope Found";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = res;

                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }

        }

        //[HttpGet("GetScopesOfAuthenticatedUserByLevelIdForSearch")]
        //public async Task<APIResponseClass<List<ScopeFetchDTO>>> GetScopesOfAuthenticatedUserByLevelId(int levelId, string? search)
        //{
        //    APIResponseClass<List<ScopeFetchDTO>> response = new();
        //    try
        //    {
        //        var Result = await _scopeService.GetScopesOfAuthenticatedUserByLevelId(levelId, search);

        //        if (Result.Count > 0)
        //        {
        //            response.message = "Scope Found";
        //        }
        //        else
        //        {
        //            response.message = "Scope Not Found";
        //        }

        //        response.apiResponseStatus = Enum.APIResponseStatus.Success;
        //        response.result = Result;

        //        return response;
        //    }
        //    catch (Exception Ex)
        //    {
        //        response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //        response.message = "Failed, please try again.." + Ex.Message;
        //        return response;
        //    }
        //}

        [HttpGet("GetScopesOfAuthenticatedUserByLevelId")]
        public async Task<APIResponseClass<List<ScopeFetchDTO>>> GetScopesOfAuthenticatedUserByLevelId(int levelId, string? filter)
        {
            APIResponseClass<List<ScopeFetchDTO>> response = new();
            try
            {
                var Result = await _scopeService.GetScopesOfAuthenticatedUserByLevelId(levelId, filter);
                if (Result.Count() > 0)
                {
                    response.message = "Scope Found";
                } else
                {
                    response.message = "Scope Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = Result;

                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }

        }

        [HttpGet("GetAllAndSelectedScope")]
        public async Task<APIResponseClass<GetAllAndSelectedScopeDTO>> GetAllAndSelectedScope(int ScopeId, int ParentLevelId)
        {
            APIResponseClass<GetAllAndSelectedScopeDTO> response = new();
            try
            {
                var result = await _scopeService.GetAllAndSelectedScope(ScopeId, ParentLevelId);
                if (result != null)
                {
                    response.message = "Data Fetched";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = result;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpPost("UpadateScope")]
        public async Task<APIResponseClass<bool>> UpadateScope(ScopeUpdateDTO scopeUpdateDTO)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _scopeService.UpadateScope(scopeUpdateDTO);
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

        [HttpDelete("DeleteScopeById")]
        public async Task<APIResponseClass<bool>> DeleteScope(int ScopeId)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _scopeService.DeleteScope(ScopeId);
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
    }
}
