﻿using EventsWebApp.Application.Validators;
using FluentValidation;

namespace EventsWebApp.Application.UseCases.Attendees.Commands
{
    public class UpdateAttendeeValidator : AddUpdateAttendeeValidator<UpdateAttendeeCommand>
    {
        public UpdateAttendeeValidator() : base(){
            RuleFor(attendee => attendee.Id)
                .NotEmpty();
        }
    }
}
