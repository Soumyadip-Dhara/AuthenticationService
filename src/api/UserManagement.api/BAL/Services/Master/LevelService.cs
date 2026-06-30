using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;

namespace UserManagement.BAL.Services.Master
{
    public class LevelService : ILevelService
    {
        private readonly IApplicationLevelRepository _applicationLevelRepository;
        private readonly IUserRoleHasUserLevelRepository _userRoleHasUserLevelRepository;
        private readonly ILevelRelationshipRepository _levelRelationshipRepository;
        private readonly IClaimService _claimService;

        public LevelService(
            IApplicationLevelRepository applicationLevelRepository,
            IUserRoleHasUserLevelRepository userRoleHasUserLevelRepository,
            ILevelRelationshipRepository levelRelationshipRepository,
            IClaimService claimService)
        {
            _applicationLevelRepository = applicationLevelRepository;
            _userRoleHasUserLevelRepository = userRoleHasUserLevelRepository;
            _levelRelationshipRepository = levelRelationshipRepository;
            _claimService = claimService;
        }

        public async Task<FetchLevelResponse> GetLevelListAsync(QueryParameters payload)
        {
            try
            {
                int applicationId = 0;
                short officeCode = 0;

                if (payload?.Filters != null)
                {
                    var appFilter = payload.Filters.FirstOrDefault(f => f.Field.Equals("ApplicationId", StringComparison.OrdinalIgnoreCase));
                    if (appFilter != null && int.TryParse(appFilter.Value, out int appId))
                    {
                        applicationId = appId;
                    }

                    var officeFilter = payload.Filters.FirstOrDefault(f => f.Field.Equals("OfficeCode", StringComparison.OrdinalIgnoreCase));
                    if (officeFilter != null && short.TryParse(officeFilter.Value, out short oCode))
                    {
                        officeCode = oCode;
                    }
                }

                var userId = _claimService.GetUserId();
                var userRoles = _claimService.GetRoles();

                IQueryable<ApplicationLevel> query;

                bool isSuperAdmin = Array.Exists(userRoles, role => role == "Super Admin");
                bool isUserAdmin = Array.Exists(userRoles, role => role.ToLower().Contains("user admin"));

                if (isSuperAdmin || isUserAdmin)
                {
                    query = _applicationLevelRepository.GetAllByCondition(e => e.AppId == applicationId);
                }
                else if (officeCode == 1)
                {
                    var ownedLevelIds = _userRoleHasUserLevelRepository.GetAllByCondition(e => 
                        e.UserHasApp != null && e.UserHasApp.UserId == userId && e.UserHasApp.AppId == applicationId
                    ).Select(e => e.RoleHasLevelId);

                    query = _applicationLevelRepository.GetAllByCondition(e => ownedLevelIds.Contains(e.AppLevelId));
                }
                else if (officeCode == 2)
                {
                    var ownedLevelIdsQuery = _userRoleHasUserLevelRepository.GetAllByCondition(e => 
                        e.UserHasApp != null && e.UserHasApp.UserId == userId && e.UserHasApp.AppId == applicationId
                    ).Select(e => e.RoleHasLevelId);

                    var ownedLevelIds = await ownedLevelIdsQuery.ToListAsync();

                    var childLevelIds = await _levelRelationshipRepository.GetAllByCondition(e => 
                        ownedLevelIds.Contains(e.LevelId) && e.LevelId != e.AccessLevelId
                    ).Select(e => e.AccessLevelId).ToListAsync();

                    var ownLevelIdsAllowed = await _applicationLevelRepository.GetAllByCondition(e => 
                        ownedLevelIds.Contains(e.AppLevelId) && e.SameLevelOtherOfficeAdminAllowed == true
                    ).Select(e => e.AppLevelId).ToListAsync();

                    var distinctLevelIds = childLevelIds.Concat(ownLevelIdsAllowed).Distinct().ToList();

                    query = _applicationLevelRepository.GetAllByCondition(e => distinctLevelIds.Contains(e.AppLevelId));
                }
                else
                {
                    return new FetchLevelResponse
                    {
                        apiResponseStatus = 3,
                        message = "Level List Not Found",
                        validationResults = "Invalid OfficeCode or permissions.",
                        result = new LevelPaginatedResult
                        {
                            totalCount = null,
                            pageNumber = null,
                            pageSize = null,
                            data = null
                        }
                    };
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

                        if (fieldName == "applicationid" || fieldName == "officecode")
                            continue;

                        if (fieldName == "title")
                        {
                            if (op == "equals")
                            {
                                query = query.Where(e => e.Level != null && e.Level.LevelName.ToLower() == value.ToLower());
                            }
                            else if (op == "startswith")
                            {
                                query = query.Where(e => e.Level != null && e.Level.LevelName.ToLower().StartsWith(value.ToLower()));
                            }
                            else if (op == "endswith")
                            {
                                query = query.Where(e => e.Level != null && e.Level.LevelName.ToLower().EndsWith(value.ToLower()));
                            }
                            else // contains
                            {
                                query = query.Where(e => e.Level != null && e.Level.LevelName.ToLower().Contains(value.ToLower()));
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
                                ? (isOrdered ? ((IOrderedQueryable<ApplicationLevel>)query).ThenByDescending(e => e.Level != null ? e.Level.LevelName : string.Empty) : query.OrderByDescending(e => e.Level != null ? e.Level.LevelName : string.Empty))
                                : (isOrdered ? ((IOrderedQueryable<ApplicationLevel>)query).ThenBy(e => e.Level != null ? e.Level.LevelName : string.Empty) : query.OrderBy(e => e.Level != null ? e.Level.LevelName : string.Empty));
                            isOrdered = true;
                        }
                        else if (fieldName == "id")
                        {
                            query = isDesc
                                ? (isOrdered ? ((IOrderedQueryable<ApplicationLevel>)query).ThenByDescending(e => e.AppLevelId) : query.OrderByDescending(e => e.AppLevelId))
                                : (isOrdered ? ((IOrderedQueryable<ApplicationLevel>)query).ThenBy(e => e.AppLevelId) : query.OrderBy(e => e.AppLevelId));
                            isOrdered = true;
                        }
                    }
                }

                if (!isOrdered)
                {
                    query = query.OrderBy(e => e.Level != null ? e.Level.LevelName : string.Empty);
                }

                // Apply Pagination
                int totalCount = await query.CountAsync();
                int pageNumber = payload?.PageNumber > 0 ? payload.PageNumber : 1;
                int pageSize = payload?.PageSize > 0 ? payload.PageSize : 10;

                var paginatedData = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new LevelGetDTO
                    {
                        Id = e.AppLevelId,
                        Title = e.Level != null ? e.Level.LevelName : string.Empty,
                        Rank = e.Rank,
                        SameLeveladminAllowed = e.SameLevelOtherOfficeAdminAllowed
                    })
                    .ToListAsync();

                if (paginatedData.Count == 0)
                {
                    return new FetchLevelResponse
                    {
                        apiResponseStatus = 3, // Error
                        message = "Level List Not Found",
                        validationResults = "No levels matched the criteria.",
                        result = new LevelPaginatedResult
                        {
                            totalCount = null,
                            pageNumber = null,
                            pageSize = null,
                            data = null
                        }
                    };
                }

                return new FetchLevelResponse
                {
                    apiResponseStatus = 1, // Success
                    message = "Level List fetched successfully",
                    validationResults = null,
                    result = new LevelPaginatedResult
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
                return new FetchLevelResponse
                {
                    apiResponseStatus = 3, // Error
                    message = "Level List Not Found",
                    validationResults = ex.Message,
                    result = new LevelPaginatedResult
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
