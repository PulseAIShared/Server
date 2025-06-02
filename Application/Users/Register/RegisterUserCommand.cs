using Application.Abstractions.Messaging;
using DataTransferObjects.Users.Responses;
using SharedKernel.Enums;

namespace Application.Users.Register;

public sealed record RegisterUserCommand(
   string Email,
    string FirstName,
    string LastName,
    string Password,
    string? CompanyName = null,    
    string? CompanyDomain = null,        
    string? CompanyCountry = null,      
    CompanySize? CompanySize = null,   
    string? CompanyIndustry = null,    
    string? InvitationToken = null       
) : ICommand<RegisterUserResponse>;
