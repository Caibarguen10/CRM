using AutoMapper;
using CrmService.Domain;
using CrmService.DTOs;

namespace CrmService.Mappings;

public class MappingProfile : Profile
{
	public MappingProfile()
	{
		// Client mappings
		CreateMap<Client, ClientDto>();
		CreateMap<CreateClientDto, Client>();

		// Contact mappings
		CreateMap<Contact, ContactDto>();
		CreateMap<CreateContactDto, Contact>();

		// Note mappings
		CreateMap<ClientNote, NoteDto>();
		CreateMap<CreateNoteDto, ClientNote>();

		// Opportunity mappings
		CreateMap<Opportunity, OpportunityDto>();
		CreateMap<CreateOpportunityDto, Opportunity>();
	}
}
