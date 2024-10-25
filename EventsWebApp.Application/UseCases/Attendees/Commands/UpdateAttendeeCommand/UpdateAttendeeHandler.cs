﻿using AutoMapper;
using EventsWebApp.Application.Interfaces.UseCases;
using EventsWebApp.Domain.Interfaces.Repositories;
using EventsWebApp.Domain.Models;

namespace EventsWebApp.Application.UseCases.Attendees.Commands
{
    public class UpdateAttendeeHandler : ICommandHandler<UpdateAttendeeCommand, Guid>
    {
        private readonly IAppUnitOfWork _appUnitOfWork;
        private readonly IMapper _mapper;
        public UpdateAttendeeHandler(IAppUnitOfWork appUnitOfWork, IMapper mapper)
        {
            _appUnitOfWork = appUnitOfWork;
            _mapper = mapper;
        }
        public async Task<Guid> Handle(UpdateAttendeeCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Attendee candidate = await _appUnitOfWork.AttendeeRepository.GetByIdWithInclude(request.Id, cancellationToken);
            if(candidate == null)
            {
                throw new Exception("Attendee not found");
            }
            
            Attendee attendee = _mapper.Map<Attendee>(request);
            attendee.DateOfRegistration = candidate.DateOfRegistration;
            attendee.SocialEvent = candidate.SocialEvent;
            attendee.SocialEventId = candidate.SocialEventId;
            attendee.User = candidate.User;
            attendee.UserId = candidate.UserId;
            var id = await _appUnitOfWork.AttendeeRepository.Update(attendee, cancellationToken);

            _appUnitOfWork.Save();
            return id;
        }
    }
}