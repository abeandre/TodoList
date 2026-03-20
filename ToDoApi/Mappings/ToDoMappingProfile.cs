using AutoMapper;
using ToDoApi.Models;

namespace ToDoApi.Mappings
{
    public class ToDoMappingProfile : Profile
    {
        public ToDoMappingProfile()
        {
            CreateMap<ToDo.DataAccess.ToDo, ToDoResponse>();

            CreateMap<CreateToDoRequest, ToDo.DataAccess.ToDo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FinishedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty));

            CreateMap<UpdateToDoRequest, ToDo.DataAccess.ToDo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FinishedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty));
        }
    }
}
