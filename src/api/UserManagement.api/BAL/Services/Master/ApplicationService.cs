using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;

namespace UserManagement.BAL.Services.Master
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserHasUserManagementRepository _userHasUserManagementRepository;
        private readonly IClaimService _claimService;

        public ApplicationService(
            IApplicationRepository applicationRepository,
            IUserHasUserManagementRepository userHasUserManagementRepository,
            IClaimService claimService)
        {
            _applicationRepository = applicationRepository;
            _userHasUserManagementRepository = userHasUserManagementRepository;
            _claimService = claimService;
        }

        public async Task<FetchApplicationResponse> GetApplicationListAsync(QueryParameters payload)
        {
            try
            {
                var userId = _claimService.GetUserId();
                var userRoles = _claimService.GetRoles();
                bool isSuperAdmin = Array.Exists(userRoles, roleName => roleName == "Super Admin");

                var query = _applicationRepository.GetAll();

                if (isSuperAdmin)
                {
                    // Filter for Super Admin (excluding specific systems)
                    query = query.Where(e => e.Title.ToLower() != "user management" && e.Title.ToLower() != "module management");
                }
                else
                {
                    // Filter for regular User Management users
                    var assignedAppIds = await _userHasUserManagementRepository.GetSelectedColumnByConditionAsync(
                        e => e.UserId == userId && e.AssignedAppId != 1,
                        e => e.AssignedAppId
                    );
                    query = query.Where(e => assignedAppIds.Contains(e.Id));
                }

                // Apply Filters
                if (payload?.Filters != null)
                {
                    foreach (var filter in payload.Filters)
                    {
                        if (string.IsNullOrWhiteSpace(filter.Field) || string.IsNullOrWhiteSpace(filter.Value))
                            continue;

                        var fieldName = filter.Field.Trim().ToLower();
                        var value = filter.Value.Trim();
                        var op = filter.Operator?.Trim().ToLower();

                        if (fieldName == "title")
                        {
                            if (op == "equals")
                            {
                                query = query.Where(e => e.Title.ToLower() == value.ToLower());
                            }
                            else if (op == "startswith")
                            {
                                query = query.Where(e => e.Title.ToLower().StartsWith(value.ToLower()));
                            }
                            else if (op == "endswith")
                            {
                                query = query.Where(e => e.Title.ToLower().EndsWith(value.ToLower()));
                            }
                            else // contains
                            {
                                query = query.Where(e => e.Title.ToLower().Contains(value.ToLower()));
                            }
                        }
                        else if (fieldName == "isactive")
                        {
                            if (bool.TryParse(value, out bool boolVal))
                            {
                                query = query.Where(e => e.IsActive == boolVal);
                            }
                        }
                        else if (fieldName == "ismaintenance" || fieldName == "isundermaintenance")
                        {
                            if (bool.TryParse(value, out bool boolVal))
                            {
                                query = query.Where(e => e.IsUnderMaintenance == boolVal);
                            }
                        }
                        else if (fieldName == "isuseusermanagement")
                        {
                            if (bool.TryParse(value, out bool boolVal))
                            {
                                query = query.Where(e => e.IsUseUserManagement == boolVal);
                            }
                        }
                    }
                }

                // Apply Sorting
                bool isOrdered = false;
                if (payload?.Sorts != null)
                {
                    foreach (var sort in payload.Sorts)
                    {
                        if (string.IsNullOrWhiteSpace(sort.Field))
                            continue;

                        var fieldName = sort.Field.Trim().ToLower();
                        var isDesc = sort.Order?.Trim().ToLower() == "desc";

                        if (fieldName == "title")
                        {
                            query = isDesc
                                ? (isOrdered ? ((IOrderedQueryable<DAL.Entities.Application>)query).ThenByDescending(e => e.Title) : query.OrderByDescending(e => e.Title))
                                : (isOrdered ? ((IOrderedQueryable<DAL.Entities.Application>)query).ThenBy(e => e.Title) : query.OrderBy(e => e.Title));
                            isOrdered = true;
                        }
                        else if (fieldName == "id")
                        {
                            query = isDesc
                                ? (isOrdered ? ((IOrderedQueryable<DAL.Entities.Application>)query).ThenByDescending(e => e.Id) : query.OrderByDescending(e => e.Id))
                                : (isOrdered ? ((IOrderedQueryable<DAL.Entities.Application>)query).ThenBy(e => e.Id) : query.OrderBy(e => e.Id));
                            isOrdered = true;
                        }
                        else if (fieldName == "isactive")
                        {
                            query = isDesc
                                ? (isOrdered ? ((IOrderedQueryable<DAL.Entities.Application>)query).ThenByDescending(e => e.IsActive) : query.OrderByDescending(e => e.IsActive))
                                : (isOrdered ? ((IOrderedQueryable<DAL.Entities.Application>)query).ThenBy(e => e.IsActive) : query.OrderBy(e => e.IsActive));
                            isOrdered = true;
                        }
                    }
                }

                if (!isOrdered)
                {
                    query = query.OrderBy(e => e.Title);
                }

                // Apply Pagination
                int totalCount = await query.CountAsync();
                int pageNumber = payload?.PageNumber > 0 ? payload.PageNumber : 1;
                int pageSize = payload?.PageSize > 0 ? payload.PageSize : 10;

                var paginatedData = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new ApplicationGetDTO
                    {
                        Id = e.Id,
                        Title = e.Title,
                        IsMaintenance = e.IsUnderMaintenance,
                        IsActive = e.IsActive,
                        IsMultiAdminDisallowed = e.IsMultiAdminDisallowed,
                        IsUseUserManagement = e.IsUseUserManagement,
                        LogoUrl = e.LogoUrl,
                        url = e.Url,
                        BaseUrl = e.BaseUrl,
                        IsConsumingData = e.IsConsumingData,
                        CreatedAt = e.CreatedAt,
                        Email = e.ApplicationAdminEmail,
                        Mobile = e.ApplicationAdminMobileNumber
                    })
                    .ToListAsync();

                if (paginatedData.Count == 0)
                {
                    return new FetchApplicationResponse
                    {
                        apiResponseStatus = 3, // Error
                        message = "Application List Not Found",
                        validationResults = "No applications matched the criteria.",
                        result = new ApplicationPaginatedResult
                        {
                            totalCount = null,
                            pageNumber = null,
                            pageSize = null,
                            data = null
                        }
                    };
                }

                return new FetchApplicationResponse
                {
                    apiResponseStatus = 1, // Success
                    message = "Application List fetched successfully",
                    validationResults = null,
                    result = new ApplicationPaginatedResult
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
                return new FetchApplicationResponse
                {
                    apiResponseStatus = 3, // Error
                    message = "Application List Not Found",
                    validationResults = ex.Message,
                    result = new ApplicationPaginatedResult
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
