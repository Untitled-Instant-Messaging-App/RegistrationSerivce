﻿using ChannelService.Repository;
using RegistrationApi.Contracts;
using RegistrationApi.Errors;
using RegistrationApi.EventBus.RabbitMQ;

namespace RegistrationApi.Services.Register
{
    public class RegistrationService : IRegistrationService
    {
        private readonly UserRepository repository;
        private readonly IRabbitMQPublisher<RegisteredUser> rabbitMQPublisher;

        public RegistrationService(UserRepository repository, IRabbitMQPublisher<RegisteredUser> rabbitMQPublisher)
        {
            this.repository = repository;
            this.rabbitMQPublisher = rabbitMQPublisher;
        }

        public async Task<RegisteredUser> RegisterUser(BasicUser user)
        {
            var (exists, placement) = await repository.CheckDisplayNameAvailability(user.DisplayName);

            if (exists) 
            {
                throw new UserAlreadyExists();
            }

            var registeredUser = new RegisteredUser
            {
                Id = Guid.NewGuid().ToString(),
                Username = $"{user.DisplayName}#{placement:0000}",
                DisplayName = user.DisplayName,
                Image = user.Image,
                JoinedAt = DateTime.UtcNow,
                EphemeralPassword = Guid.NewGuid().ToString(),
            };

            await repository.CreateUser(registeredUser);

            rabbitMQPublisher.Publish(registeredUser, "users.new");

            return registeredUser;
        }

        public async Task UnregisterUser(string userId)
        {
            await repository.DeleteUser(userId);
        }
    }
}