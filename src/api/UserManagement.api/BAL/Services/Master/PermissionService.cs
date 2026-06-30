using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;

namespace UserManagement.BAL.Services.Master
{
    public class PermissionService : IPermissionService
    {
        private readonly IRoleHasPermissionRepository _roleHasPermissionRepository;

        public PermissionService(IRoleHasPermissionRepository roleHasPermissionRepository)
        {
            _roleHasPermissionRepository = roleHasPermissionRepository;
        }

        public async Task<FetchPermissionResponse> GetPermissionListAsync(QueryParameters payload)
        {
            try
            {
                int roleId = 0;

                if (payload?.Filters != null)
                {
                    var roleFilter = payload.Filters.FirstOrDefault(f => f.Field.Equals("RoleId", StringComparison.OrdinalIgnoreCase));
                    if (roleFilter != null && int.TryParse(roleFilter.Value, out int rId))
                    {
                        roleId = rId;
                    }
                }

                if (roleId == 0)
                {
                    return new FetchPermissionResponse
                    {
                        apiResponseStatus = 3, // Error
                        message = "Permisssion List Not Found",
                        validationResults = "RoleId filter is required.",
                        result = new PermissionPaginatedResult
                        {
                            totalCount = null,
                            pageNumber = null,
                            pageSize = null,
                            data = null
                        }
                    };
                }

                // Query RoleHasPermission filtering by RoleId
                var query = _roleHasPermissionRepository.GetAllByCondition(e => e.RoleId == roleId);

                // Apply additional filters (e.g. on Permission Name)
                if (payload?.Filters != null)
                {
                    foreach (var filter in payload.Filters)
                    {
                        if (string.IsNullOrWhiteSpace(filter.Field) || string.IsNullOrWhiteSpace(filter.Value))
                            continue;

                        var fieldName = filter.Field.Trim().ToLower();
                        var value = filter.Value.Trim();
                        var op = filter.Operator?.Trim().ToLower();

                        if (fieldName == "roleid")
                            continue;

                        if (fieldName == "name")
                        {
                            if (op == "eq" || op == "equals")
                            {
                                query = query.Where(e => e.Permission != null && e.Permission.Name.ToLower() == value.ToLower());
                            }
                            else if (op == "startswith")
                            {
                                query = query.Where(e => e.Permission != null && e.Permission.Name.ToLower().StartsWith(value.ToLower()));
                            }
                            else if (op == "endswith")
                            {
                                query = query.Where(e => e.Permission != null && e.Permission.Name.ToLower().EndsWith(value.ToLower()));
                            }
                            else // contains
                            {
                                query = query.Where(e => e.Permission != null && e.Permission.Name.ToLower().Contains(value.ToLower()));
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

                        if (fieldName == "name")
                        {
                            query = isDesc
                                ? (isOrdered ? ((IOrderedQueryable<RoleHasPermission>)query).ThenByDescending(e => e.Permission != null ? e.Permission.Name : string.Empty) : query.OrderByDescending(e => e.Permission != null ? e.Permission.Name : string.Empty))
                                : (isOrdered ? ((IOrderedQueryable<RoleHasPermission>)query).ThenBy(e => e.Permission != null ? e.Permission.Name : string.Empty) : query.OrderBy(e => e.Permission != null ? e.Permission.Name : string.Empty));
                            isOrdered = true;
                        }
                        else if (fieldName == "id")
                        {
                            query = isDesc
                                ? (isOrdered ? ((IOrderedQueryable<RoleHasPermission>)query).ThenByDescending(e => e.PermissionId) : query.OrderByDescending(e => e.PermissionId))
                                : (isOrdered ? ((IOrderedQueryable<RoleHasPermission>)query).ThenBy(e => e.PermissionId) : query.OrderBy(e => e.PermissionId));
                            isOrdered = true;
                        }
                    }
                }

                if (!isOrdered)
                {
                    query = query.OrderBy(e => e.Permission != null ? e.Permission.Name : string.Empty);
                }

                // Apply Pagination
                int totalCount = await query.CountAsync();
                int pageNumber = payload?.PageNumber > 0 ? payload.PageNumber : 1;
                int pageSize = payload?.PageSize > 0 ? payload.PageSize : 10;

                var paginatedData = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new PermissionResultDTO
                    {
                        id = e.PermissionId,
                        name = e.Permission != null ? e.Permission.Name : string.Empty
                    })
                    .ToListAsync();

                if (paginatedData.Count == 0)
                {
                    return new FetchPermissionResponse
                    {
                        apiResponseStatus = 3, // Error
                        message = "Permisssion List Not Found",
                        validationResults = "No permissions matched the criteria.",
                        result = new PermissionPaginatedResult
                        {
                            totalCount = null,
                            pageNumber = null,
                            pageSize = null,
                            data = null
                        }
                    };
                }

                return new FetchPermissionResponse
                {
                    apiResponseStatus = 1, // Success
                    message = "Permission List fetched successfully",
                    validationResults = null,
                    result = new PermissionPaginatedResult
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
                return new FetchPermissionResponse
                {
                    apiResponseStatus = 3, // Error
                    message = "Permisssion List Not Found",
                    validationResults = ex.Message,
                    result = new PermissionPaginatedResult
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
