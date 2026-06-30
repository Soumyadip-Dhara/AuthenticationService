using AngleSharp.Io;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UserManagement.DAL.Entities;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;

namespace UserManagement.BAL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IClaimService _claimService;
        private readonly UserManagementDBContext _dbContext;
        private readonly IUserMasterRepository _userMasterRepository;
        private readonly IUserHasUserManagementRepository _userHasUserManagementRepository;
        private readonly IUserHasModuleManagementRepository _userHasModuleManagementRepository;

        public UserService(
            IUserRepository userRepository, 
            IClaimService claimService, 
            UserManagementDBContext dbContext,
            IUserMasterRepository userMasterRepository,
            IUserHasUserManagementRepository userHasUserManagementRepository,
            IUserHasModuleManagementRepository userHasModuleManagementRepository)
        {
            _userRepository = userRepository;
            _claimService = claimService;
            _dbContext = dbContext;
            _userMasterRepository = userMasterRepository;
            _userHasUserManagementRepository = userHasUserManagementRepository;
            _userHasModuleManagementRepository = userHasModuleManagementRepository;
        }

        public async Task<ServiceResponse<PaginatedResult<UserDetailsDTO>>> FetchUserList(QueryParameters payload)
        {
            var userId = _claimService.GetUserId();
            var userRoles = _claimService.GetRoles();
            var role = userRoles.Length > 0 ? userRoles[0] : "User";

            var result = await _userRepository.FetchUserList(payload, role, userId);
            return new ServiceResponse<PaginatedResult<UserDetailsDTO>>
            {
                result = result,
                apiResponseStatus = Enum.APIResponseStatus.Success,
                Message = result.Data.Count > 0 ? "User List Found" : "Data Not Found"
            };
        }

        public async Task<UpsertBasicUserDetailsResponse> UpsertBasicUserDetails(UpsertBasicUserDetailsRequest request)
        {
            try
            {
                // Determine if it is an update or insert
                bool isUpdate = request.userId.HasValue && request.userId.Value > 0;

                // Validate request based on Insert vs Update rules
                if (isUpdate)
                {
                    // Update: userId is mandatory (already checked by isUpdate)
                    // Validations on other fields if provided
                    if (request.mobile != null)
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(request.mobile, @"^[6-9]\d{9}$"))
                        {
                            return new UpsertBasicUserDetailsResponse
                            {
                                apiResponseStatus = 3,
                                message = "User details Not Updated",
                                validationResults = "Mobile number must be 10 digits and start with 6-9."
                            };
                        }
                    }

                    if (request.email != null)
                    {
                        if (!IsValidEmail(request.email))
                        {
                            return new UpsertBasicUserDetailsResponse
                            {
                                apiResponseStatus = 3,
                                message = "User details Not Updated",
                                validationResults = "Email must be a valid email format."
                            };
                        }
                    }
                }
                else
                {
                    // Insert: All fields are mandatory except userId
                    if (string.IsNullOrWhiteSpace(request.userName) ||
                        string.IsNullOrWhiteSpace(request.hrmsId) ||
                        string.IsNullOrWhiteSpace(request.name) ||
                        string.IsNullOrWhiteSpace(request.designation) ||
                        string.IsNullOrWhiteSpace(request.mobile) ||
                        string.IsNullOrWhiteSpace(request.email) ||
                        !request.active.HasValue ||
                        !request.blocked.HasValue)
                    {
                        return new UpsertBasicUserDetailsResponse
                        {
                            apiResponseStatus = 3,
                            message = "User details Not Inserted",
                            validationResults = "All fields (userName, hrmsId, name, designation, mobile, email, active, blocked) are mandatory for user insertion."
                        };
                    }

                    // Validate Mobile
                    if (!System.Text.RegularExpressions.Regex.IsMatch(request.mobile, @"^[6-9]\d{9}$"))
                    {
                        return new UpsertBasicUserDetailsResponse
                        {
                            apiResponseStatus = 3,
                            message = "User details Not Inserted",
                            validationResults = "Mobile number must be 10 digits and start with 6-9."
                        };
                    }

                    // Validate Email
                    if (!IsValidEmail(request.email))
                    {
                        return new UpsertBasicUserDetailsResponse
                        {
                            apiResponseStatus = 3,
                            message = "User details Not Inserted",
                            validationResults = "Email must be a valid email format."
                        };
                    }
                }

                // Get Current User ID from claim service (audit trail)
                long currentUserId = _claimService.GetUserId();
                if (currentUserId == 0)
                {
                    return new UpsertBasicUserDetailsResponse
                    {
                        apiResponseStatus = 3,
                        message = "User details Not Inserted",
                        validationResults = "Active login User Not Found"
                    };
                }

                if (isUpdate)
                {
                    // Execute Update
                    long targetUserId = request.userId!.Value;
                    var existingUser = await _dbContext.UserMasters.FirstOrDefaultAsync(u => u.Id == targetUserId);

                    if (existingUser == null)
                    {
                        return new UpsertBasicUserDetailsResponse
                        {
                            apiResponseStatus = 3,
                            message = "User details Not Updated",
                            validationResults = $"User with ID {targetUserId} not found."
                        };
                    }

                    // Check username uniqueness if userName is changing
                    if (!string.IsNullOrWhiteSpace(request.userName) && !request.userName.Equals(existingUser.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        var userNameExists = await _dbContext.UserMasters.AnyAsync(u => u.UserName.ToLower() == request.userName.ToLower());
                        if (userNameExists)
                        {
                            return new UpsertBasicUserDetailsResponse
                            {
                                apiResponseStatus = 3,
                                message = "User details Not Updated",
                                validationResults = $"Username '{request.userName}' is already taken."
                            };
                        }
                        existingUser.UserName = request.userName;
                    }

                    // Detect toggle flags changes for toggling specific success/error messages
                    bool oldIsActive = existingUser.IsActive;
                    bool oldIsBlocked = existingUser.IsBlocked;
                    bool isToggleRequest = (request.active.HasValue && request.active.Value != oldIsActive) ||
                                           (request.blocked.HasValue && request.blocked.Value != oldIsBlocked);

                    // Update optional fields if provided
                    if (request.hrmsId != null) existingUser.HrmsId = request.hrmsId;
                    if (request.name != null) existingUser.Name = request.name;
                    if (request.designation != null) existingUser.Designation = request.designation;
                    if (request.mobile != null) existingUser.MobileNumber = request.mobile;
                    if (request.email != null) existingUser.Email = request.email;
                    if (request.active.HasValue) existingUser.IsActive = request.active.Value;
                    if (request.blocked.HasValue) existingUser.IsBlocked = request.blocked.Value;

                    existingUser.UpdatedAt = DateTime.Now;
                    existingUser.UpdatedBy = currentUserId;

                    _dbContext.UserMasters.Update(existingUser);
                    await _dbContext.SaveChangesAsync();

                    // Formulate response message
                    string successMessage = "User Details Updated Successfully";
                    if (request.active.HasValue && request.active.Value != oldIsActive)
                    {
                        successMessage = request.active.Value
                            ? $"User '{existingUser.UserName}' has been activated successfully."
                            : $"User '{existingUser.UserName}' has been deactivated successfully.";
                    }
                    else if (request.blocked.HasValue && request.blocked.Value != oldIsBlocked)
                    {
                        successMessage = request.blocked.Value
                            ? $"User '{existingUser.UserName}' has been blocked successfully."
                            : $"User '{existingUser.UserName}' has been unblocked successfully.";
                    }

                    return new UpsertBasicUserDetailsResponse
                    {
                        apiResponseStatus = 1,
                        message = successMessage,
                        validationResults = null
                    };
                }
                else
                {
                    // Execute Insert
                    // Check username uniqueness
                    var userNameExists = await _dbContext.UserMasters.AnyAsync(u => u.UserName.ToLower() == request.userName!.ToLower());
                    if (userNameExists)
                    {
                        return new UpsertBasicUserDetailsResponse
                        {
                            apiResponseStatus = 3,
                            message = "User details Not Inserted",
                            validationResults = $"Username '{request.userName}' is already taken."
                        };
                    }

                    var newUser = new UserMaster
                    {
                        UserName = request.userName!,
                        HrmsId = request.hrmsId,
                        Name = request.name!,
                        Designation = request.designation!,
                        MobileNumber = request.mobile!,
                        Email = request.email,
                        IsActive = request.active!.Value,
                        IsBlocked = request.blocked!.Value,
                        CreatedAt = DateTime.Now,
                        EffectiveFrom = DateOnly.FromDateTime(DateTime.Now),
                        ExpiresOn = DateOnly.FromDateTime(DateTime.Now.AddYears(1)),
                        DueFirstLogin = true,
                        IsAnAdmin = false,
                        OldId = 0,
                        SignerId = string.Empty,
                        UnsuccessfulLoginAttempt = 0,
                        CreatedBy = currentUserId
                    };

                    // Compute password hash and salt for default password
                    using (var hmac = new System.Security.Cryptography.HMACSHA512())
                    {
                        newUser.PasswordSalt = hmac.Key;
                        newUser.PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes("Welcome@123"));
                    }

                    _dbContext.UserMasters.Add(newUser);
                    await _dbContext.SaveChangesAsync();

                    return new UpsertBasicUserDetailsResponse
                    {
                        apiResponseStatus = 1,
                        message = "User Details Inserted Successfully",
                        validationResults = null
                    };
                }
            }
            catch (Exception ex)
            {
                // Capture toggle status to output correct message prefix
                bool isUpdate = request.userId.HasValue && request.userId.Value > 0;
                string baseMessage = isUpdate ? "User details Not Updated" : "User details Not Inserted";
                
                // If it is toggling, the requirement says "User '{userName}' Could not be Toggled"
                if (isUpdate && !string.IsNullOrEmpty(request.userName))
                {
                    // Try to see if it is a toggle request
                    try
                    {
                        var existing = _dbContext.UserMasters.FirstOrDefault(u => u.Id == request.userId.Value);
                        if (existing != null)
                        {
                            bool toggled = (request.active.HasValue && request.active.Value != existing.IsActive) ||
                                           (request.blocked.HasValue && request.blocked.Value != existing.IsBlocked);
                            if (toggled)
                            {
                                baseMessage = $"User '{existing.UserName}' Could not be Toggled";
                            }
                        }
                    }
                    catch { }
                }

                return new UpsertBasicUserDetailsResponse
                {
                    apiResponseStatus = 3,
                    message = baseMessage,
                    validationResults = ex.Message
                };
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public async Task<FetchBasicUserDetailsResponse> FetchBasicUserDetails(long userId)
        {
            try
            {
              
                UserDetailsDTO? userDetails = null;
                var userMaster = await _dbContext.UserMasters.FirstOrDefaultAsync(u => u.Id == userId);
                if (userMaster == null)
                {
                     return new FetchBasicUserDetailsResponse
                     {
                         apiResponseStatus = 3,
                         message = "User Details Not Found",
                         validationResults = "User details not found for the provided User ID."
                     };
                }

                userDetails = new UserDetailsDTO
                { 
                    userId = userMaster.Id,
                    userName = userMaster.UserName,
                    hrmsId = userMaster.HrmsId,
                    name = userMaster.Name,
                    designation = userMaster.Designation,
                    mobile = userMaster.MobileNumber,
                    email = userMaster.Email,
                    active = userMaster.IsActive,
                    blocked = userMaster.IsBlocked,
                    createdAt = userMaster.CreatedAt.ToString("dd-MM-yyyy")
                };
                

                return new FetchBasicUserDetailsResponse
                {
                    result = userDetails,
                    apiResponseStatus = 1,
                    message = "User Details Found",
                    validationResults = null
                };
            }
            catch (Exception ex)
            {
                return new FetchBasicUserDetailsResponse
                {
                    apiResponseStatus = 3,
                    message = "User Details Not Found",
                    validationResults = ex.Message
                };
            }
        }

        public async Task<FetchUserPrivilegeResponse> GetUserPrivilegeListAsync(QueryParameters payload)
        {
            try
            {
                long userId = 0;
                if (payload?.Filters != null)
                {
                    var userFilter = payload.Filters.FirstOrDefault(f => f.Field.Equals("UserId", StringComparison.OrdinalIgnoreCase));
                    if (userFilter != null && long.TryParse(userFilter.Value, out long uId))
                    {
                        userId = uId;
                    }
                }

                if (userId == 0)
                {
                    return new FetchUserPrivilegeResponse
                    {
                        apiResponseStatus = 3, // Error
                        message = "User ID is required",
                        validationResults = "UserId filter is missing or invalid.",
                        result = new UserPrivilegePaginatedResult
                        {
                            totalCount = null,
                            pageNumber = null,
                            pageSize = null,
                            data = null
                        }
                    };
                }

                var resultList = new List<UserAccessDTO>();
                var isAnAdmin = (bool)await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
                    e => e.Id == userId,
                    e => e.IsActive);

                if (isAnAdmin)
                {
                    var tempResult = await _userMasterRepository.GetUserPrivilegesAsync(userId);
                    foreach (var item in tempResult)
                    {
                        if (item.data != null && item.data.application != null && item.data.application.title.ToLower().Contains("user management"))
                        {
                            var umApps = await _userHasUserManagementRepository.GetSelectedColumnByConditionAsync(
                                e => e.UserId == userId,
                                e => new ChildUserPrivilegesDTO
                                {
                                    data = new UserPrivilegesDTO
                                    {
                                        application = new ApplicationsDto
                                        {
                                            id = e.AssignedAppId,
                                            title = e.AssignedApp.Title
                                        },
                                        role = item.data.role,
                                        permissions = item.data.permissions,
                                        level = item.data.level,
                                        scopes = item.data.scopes
                                    }
                                });

                            item.children = umApps.ToList();
                        }
                        if (item.data != null && item.data.application != null && item.data.application.title.ToLower().Contains("module management"))
                        {
                            var umApps = await _userHasModuleManagementRepository.GetSelectedColumnByConditionAsync(
                                e => e.UserId == userId,
                                e => new ChildUserPrivilegesDTO
                                {
                                    data = new UserPrivilegesDTO
                                    {
                                        application = new ApplicationsDto
                                        {
                                            id = e.AssignedAppId,
                                            title = e.AssignedApp.Title
                                        },
                                        role = item.data.role,
                                        permissions = item.data.permissions,
                                        level = item.data.level,
                                        scopes = item.data.scopes
                                    }
                                });
                            item.children = umApps.ToList();
                        }
                    }
                    resultList = tempResult.ToList();
                }
                else
                {
                    resultList = (await _userMasterRepository.GetUserPrivilegesAsync(userId)).ToList();
                }

                // Flatten the UserAccessDTO result to FlatUserAccessDTO to match requested POST response format
                var flatList = resultList.Select(item => new FlatUserAccessDTO
                {
                    id = item.Id,
                    application = item.data?.application,
                    role = item.data?.role,
                    level = item.data?.level,
                    permissions = item.data?.permissions,
                    scopes = item.data?.scopes,
                    userManagementEnabled = item.data?.UserManagementEnabled,
                    children = item.children
                }).ToList();

                // Apply in-memory paging
                int totalCount = flatList.Count;
                int pageSize = payload?.PageSize > 0 ? payload.PageSize : 10;
                int pageNumber = payload?.PageNumber > 0 ? payload.PageNumber : 1;

                var paginatedData = flatList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                if (totalCount == 0)
                {
                    return new FetchUserPrivilegeResponse
                    {
                        apiResponseStatus = 3, // Error
                        message = "User Privilege Not Found",
                        validationResults = "No privileges found for the user.",
                        result = new UserPrivilegePaginatedResult
                        {
                            totalCount = null,
                            pageNumber = null,
                            pageSize = null,
                            data = null
                        }
                    };
                }

                return new FetchUserPrivilegeResponse
                {
                    apiResponseStatus = 1, // Success
                    message = "User Privilege fetched successfully",
                    validationResults = null,
                    result = new UserPrivilegePaginatedResult
                    {
                        totalCount = totalCount,
                        pageNumber = pageNumber,
                        pageSize = pageSize,
                        data = paginatedData
                    }
                };
            }
            catch (Exception ex)
            {
                return new FetchUserPrivilegeResponse
                {
                    apiResponseStatus = 3, // Error
                    message = "User Privilege Not Found",
                    validationResults = ex.Message,
                    result = new UserPrivilegePaginatedResult
                    {
                        totalCount = null,
                        pageNumber = null,
                        pageSize = null,
                        data = null
                    }
                };
            }
        }
    }
}
