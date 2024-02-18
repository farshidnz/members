using AutoMapper;
using SettingsAPI.Common;
using SettingsAPI.EF;
using SettingsAPI.Model;
using SettingsAPI.Model.Rest;
using SettingsAPI.Model.Rest.UpdateEmail;
using SettingsAPI.Model.Rest.UpdateMobile;

namespace SettingsAPI.Mappers
{
    public class MemberProfile : Profile
    {
        public MemberProfile()
        {
            CreateMap<CommsPreferencesRequest, UpdateCommsPreferencesModel>()
                .ForMember(dest => dest.PersonId, opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.PersonId]))
                .ForMember(dest => dest.MemberId, opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.MemberId]));

            CreateMap<InstallNotifierRequest, InstallNotifierModel>()
               .ForMember(dest => dest.PersonId, opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.PersonId]))
               .ForMember(dest => dest.MemberId, opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.MemberId]));


            CreateMap<Member, Member>();

            CreateMap<UpdateMobileNumberRequest, MemberModel>()
               .ForMember(dest => dest.MemberId,
               opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.MemberId]))
               .ForMember(dest => dest.PersonId,
               opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.PersonId]));

            CreateMap<EmailUpdateRequest, MemberModel>()
               .ForMember(dest => dest.MemberId,
               opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.MemberId]))
               .ForMember(dest => dest.PersonId,
               opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.PersonId]));

            CreateMap<CommsPromptShownRequest, CommsPromptShownModel>()
               .ForMember(dest => dest.PersonId, opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.PersonId]))
               .ForMember(dest => dest.MemberId, opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.MemberId]))
               .ForMember(dest => dest.Action, opts => opts.MapFrom((_, __, ___, context) => context.Items[Constant.Mapper.CommsPromptDismissalAction]));

        }


    }
}