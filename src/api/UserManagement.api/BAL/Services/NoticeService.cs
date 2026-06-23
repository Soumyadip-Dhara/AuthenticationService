using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Repositories;
using UserManagement.Middlewares;
using UserManagement.Models;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Services
{
    public class NoticeService : INoticeService
    {
        private readonly INoticeRepository _noticeRepository;
        private readonly IUserMasterRepository _userMasterRepository;
        private readonly ILogger<NoticeService> _logger;
        private readonly UserManagementDBContext _context;

        public NoticeService(UserManagementDBContext context,INoticeRepository noticeRepository, ILogger<NoticeService> logger, IUserMasterRepository userMasterRepository)
        {
            _noticeRepository = noticeRepository;
            _userMasterRepository = userMasterRepository;
            _logger = logger;
            _context = context;

        }



        public async Task<List<NoticeDTO>> GetAllNoticesAsync(NoticeFilterDTO filter)
        {
            int pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
            int pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

            // Business rule: normalize ToDate
            DateTime? toDate = filter.ToDate?.Date.AddDays(1).AddTicks(-1);

            var notices = await _noticeRepository.GetNoticesAsync(
                pageNumber,
                pageSize,
                filter.FromDate,
                toDate,
                filter.IsActive);

            return notices.Select(n => new NoticeDTO
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                ExpiresAt = n.ExpiresAt,
                IsActive = n.IsActive
            }).ToList();
        }

        public async Task<long> CreateNoticeAsync(CreateNoticeDTO dto, long createdBy)
        {
            // Business validations
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new Exception("Title is required");

            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new Exception("Message is required");

            var notice = new Notice
            {
                Title = dto.Title.Trim(),
                Message = dto.Message.Trim(),
                ExpiresAt = dto.ExpiresAt,
                CreatedBy = createdBy,
                IsActive = true
                // CreatedAt handled by DB default
            };

            return await _noticeRepository.InsertNoticeAsync(notice);
        }

        public async Task UpdateNoticeAsync(UpdateNoticeDTO dto, long updatedBy)
        {
            if (dto.Id <= 0)
                throw new Exception("Invalid notice id");

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new Exception("Title cannot be empty");

            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new Exception("Message cannot be empty");

            var notice = await _noticeRepository.GetNoticeByIdAsync(dto.Id);

            if (notice == null)
                throw new Exception("Notice not found");

            // Update allowed fields only
            notice.Title = dto.Title.Trim();
            notice.Message = dto.Message.Trim();
            notice.ExpiresAt = dto.ExpiresAt;
            notice.IsActive = dto.IsActive;

            notice.UpdatedBy = updatedBy;
            notice.UpdatedAt = DateTime.Now;

            await _noticeRepository.UpdateNoticeAsync(notice);
        }





    }
}

