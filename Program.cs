using RegistrationService.Repository;
using RegistrationService.Repository.Connection;
using System.Text.Json.Serialization;
using RegistrationService.Exceptions;
using RegistrationService.Contracts;
using RegistrationService.EventBus.RabbitMQ.Connection;
using RegistrationService.EventBus.RabbitMQ;
using RegistrationService.Services;
using Bugsnag;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bugsnag
builder.Services.AddSingleton<IClient>(_ => new Client(builder.Configuration["Bugsnag:ApiKey"]));

// Serialization
builder.Services.AddControllers().AddJsonOptions(configs =>
{
    configs.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    configs.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Controllers
builder.Services.AddControllers();

// Repositories
builder.Services.AddSingleton(_ => new UserRepository(new ConnectionFactory(builder.Configuration["ConnectionStrings:Users"])));
builder.Services.AddSingleton(_ => new UserRepository(new ConnectionFactory(builder.Configuration["ConnectionStrings:Users"])));

// RabbitMQ
var rabbitMQConnection = new RabbitMQConnection(builder.Configuration["RabbitMQ:Uri"]);
builder.Services.AddSingleton<IRabbitMQPublisher<RegisteredUser>>(_ => new RabbitMQPublisher<RegisteredUser>(rabbitMQConnection, builder.Configuration["RabbitMQ:Exchanges"]));

// Services
builder.Services.AddSingleton(s => new UserService(s.GetRequiredService<UserRepository>(), s.GetRequiredService<IRabbitMQPublisher<RegisteredUser>>()));

var app = builder.Build();

// Singleton instantiation
app.Services.GetService<UserService>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<HttpExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
