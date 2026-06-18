using AutoMapper;
using UserManagement.DAL.Entities;
using UserManagement.Models.MQueue;

public class ConsumeLogProfile : Profile
{
    public ConsumeLogProfile()
    {
        CreateMap<NewConsumeLogModel, ConsumeFailedLog>()

            // REQUIRED fields (DB NOT NULL)
            .ForMember(dest => dest.ActionStatus,
                opt => opt.MapFrom(src => src.ActionStatus ?? "PENDING"))

            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom(_ => DateTime.Now))

            // OPTIONAL fields (ignore)
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ResolvedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Remarks, opt => opt.Ignore());
    }
}