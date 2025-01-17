﻿using Grpc.Core;
using GrpcServer.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcServer.Services
{
    /// <summary>
    /// Implementation of phone book RPC service
    /// </summary>
    public class FaceProfilerService : FaceProfiler.FaceProfilerBase 
    {
        private readonly ILogger<FaceProfilerService> logger;
        private readonly FaceProfilerRepository repository;
        
        public FaceProfilerService(ILogger<FaceProfilerService> logger, FaceProfilerRepository repository)
        {
            this.logger = logger;
            this.repository = repository; 
        }

        public override Task<ContactModel> CreateNewContact(ContactModel request, ServerCallContext context)
        {
            ContactModel response = repository.AddContact(request);

            return Task.FromResult(response);
        }

        public override Task<ContactsResponse> GetAllContacts(GetAllRequest request, ServerCallContext context)
        {
            ContactsResponse response = new ContactsResponse();
            foreach (var contact in repository.Contacts)
            {   
                response.Contact.Add(contact);
            }
            return Task.FromResult(response);
        }

        public override async Task SearchContacts(SearchRequest request, IServerStreamWriter<ContactModel> responseStream, ServerCallContext context)
        {
            foreach (var contact in repository.Contacts)
            {
                bool match = false;
                if (request.TenantName.Length > 0)
                {
                    if (contact.TenantName.ToUpper().Contains(request.TenantName.ToUpper()))
                    {
                        match = true;
                    }
                }
                if (request.UserName.Length > 0)
                {
                    if (contact.UserName.ToUpper().Contains(request.UserName.ToUpper()))
                    {
                        match = true;
                    }
                }
                if (match)
                {
                    await Task.Delay(1000);
                    await responseStream.WriteAsync(contact);
                }
            }
        }

        public override Task<ContactModel> AddPhoneNumber(AddPhoneNumberRequest request, ServerCallContext context)
        {
            ContactModel updateContact = repository.FindContact(request.ContactID);
            if (updateContact == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Contact with ID={request.ContactID} is not found."));
            }
            updateContact.PhoneNumbers.Add(new PhoneNumberModel
            {
                NumberID = repository.NextNumberID(),
                Number = request.Number,
                PhoneType = request.PhoneType
            });

            return Task.FromResult(updateContact);
        }

        public override Task<ContactModel> UpdateContact(ContactModel request, ServerCallContext context)
        {
            ContactModel updateContact = repository.FindContact(request.ContactID);
            if (updateContact == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Contact with ID={request.ContactID} is not found."));
            }
            updateContact.TenantName = request.TenantName;
            updateContact.UserName = request.UserName;
            updateContact.Address = request.Address;
            updateContact.City = request.City;
            updateContact.Country = request.Country;
            updateContact.Zipcode = request.Zipcode;
            updateContact.Email = request.Email;
            updateContact.FaceData = request.FaceData;

            return Task.FromResult(updateContact);
        }

        public override Task<ContactModel> UpdatePhoneNumber(PhoneNumberModel request, ServerCallContext context)
        {
            PhoneNumberModel updatePhone = null;
            ContactModel updateContact = null;

            repository.FindContactAndPhoneNumber(request.NumberID, out updateContact, out updatePhone);

            if (updatePhone == null || updateContact == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Phone number with ID={request.NumberID} is not found."));
            }

            updatePhone.Number = request.Number;
            updatePhone.PhoneType = request.PhoneType;

            return Task.FromResult(updateContact);
        }

        public override Task<GenericResponseMessage> DeleteContact(DeleteContactRequest request, ServerCallContext context)
        {
            ContactModel deleteContact = repository.FindContact(request.ContactID);

            if (deleteContact == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Contact with ID={request.ContactID} is not found."));
            }

            repository.Contacts.Remove(deleteContact);

            return Task.FromResult(new GenericResponseMessage { Message = "Contact is successfuly deleted" });
        }

        public override Task<ContactModel> DeletePhoneNumber(DeletePhoneNumberRequest request, ServerCallContext context)
        {
            ContactModel contact;
            PhoneNumberModel deletePhone;
            
            repository.FindContactAndPhoneNumber(request.NumberID, out contact, out deletePhone);

            if (contact == null || deletePhone == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Phone number with ID={request.NumberID} is not found."));
            }

            contact.PhoneNumbers.Remove(deletePhone);

            return Task.FromResult(contact);
        }

        public override Task<ContactModel> GetContact(GetContactRequest request, ServerCallContext context)
        {
            ContactModel contactResponse = repository.FindContact(request.ContactID);
            
            if (contactResponse == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Contact with ID={request.ContactID} is not found."));
            }

            return Task.FromResult(contactResponse);
        }

        public override Task<PhoneNumberModel> GetPhoneNumber(GetPhoneNumberRequest request, ServerCallContext context)
        {
            PhoneNumberModel phoneNumberResponse = repository.FindPhoneNumber(request.NumberID);

            if (phoneNumberResponse == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Phone number with ID={request.NumberID} is not found."));
            }

            return Task.FromResult(phoneNumberResponse);
        }
    }
}
