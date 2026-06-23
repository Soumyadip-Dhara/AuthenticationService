using AutoMapper;
using UserManagement.DAL.Entities;
using UserManagement.Models.MQueue;

namespace UserManagement.Helper
{
    public class MapperClass : Profile
    {
        public MapperClass()
        {
            
            //new
            // CreateMap<ApplicationCreateDTO, Application>();

            // CreateMap<ApplicationGetDTO, Application>();
            // CreateMap<Application, ApplicationGetDTO>();

            // CreateMap<Role, RoleGetDTO>();
            // CreateMap<RoleGetDTO, Role>();

            // CreateMap<RoleModel, Role>();
            // CreateMap<Role, RoleModel>();

            // CreateMap<RoleSetDTO, Role>();
            // CreateMap<Role, RoleSetDTO>();


            // CreateMap<UserBasicDetailsSetDTO, UserModel>();
            // CreateMap<UserModel, UserBasicDetailsSetDTO>();

            // CreateMap<Permission, PermissionSetDTO>();
            // CreateMap<PermissionSetDTO, Permission>();
            // CreateMap<PermissionUpdateDTO, Permission>();


            // CreateMap<RoleRelationshipModel, RoleRelationship>();
            // CreateMap<RoleRelationship, RoleRelationshipModel>();

            // CreateMap<RoleHasPermissionModel, RoleHasPermission>();
            // CreateMap<RoleHasPermission, RoleHasPermissionModel>();
            CreateMap<NewConsumeLogModel, ConsumeLog>();
        }
    }
}
