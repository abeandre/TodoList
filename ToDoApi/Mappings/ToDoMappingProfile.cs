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
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FinishedAt, opt => opt.Ignore());

            // FinishedAt is intentionally excluded from UpdateToDoRequest — completion status
            // is managed via the dedicated PATCH /status endpoint, not the PUT /update endpoint.
            CreateMap<UpdateToDoRequest, ToDo.DataAccess.ToDo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FinishedAt, opt => opt.Ignore());
        }
    }
}
