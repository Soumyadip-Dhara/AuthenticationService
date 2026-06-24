using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UserManagement.Enum;

namespace UserManagement.Models.DTO.Pagination
{
    public class PaginatedResult<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<T> Data { get; set; } = new();
    }

    public class FilterCriteria
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class SortCriteria
    {
        public string Field { get; set; } = string.Empty;
        public string Order { get; set; } = "asc"; // "asc" or "desc"
    }

    public class QueryParameters
    {
        public List<FilterCriteria> Filters { get; set; } = new List<FilterCriteria>();
        public List<SortCriteria> Sorts { get; set; } = new List<SortCriteria>();
        public List<string> SelectColumns { get; set; } = new List<string>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<string> IncludeProperties { get; set; } = new List<string>();
    }

    public class ServiceResponse<T>
    {
        public T? result { get; set; }
        public APIResponseStatus apiResponseStatus { get; set; }
        public string Message { get; set; } = string.Empty;
        public ICollection<ValidationResult>? validationResults { get; set; }
    }
}
