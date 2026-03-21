using AutoMapper;
using ToDoApi.Models;

namespace ToDoApi.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<ToDo.DataAccess.User, UserResponse>();

            CreateMap<CreateUserRequest, ToDo.DataAccess.User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.HashedPassword, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ToDos, opt => opt.Ignore());

            CreateMap<UpdateUserRequest, ToDo.DataAccess.User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.HashedPassword, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ToDos, opt => opt.Ignore());
        }
    }
}
