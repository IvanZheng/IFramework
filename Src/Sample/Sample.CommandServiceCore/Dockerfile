#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Sample/Sample.CommandServiceCore/Sample.CommandServiceCoreNet5.csproj", "Sample/Sample.CommandServiceCore/"]
COPY ["iFramework.Plugins/IFramework.MessageQueueCore.ConfluentKafka/IFramework.MessageQueue.ConfluentKafka.csproj", "iFramework.Plugins/IFramework.MessageQueueCore.ConfluentKafka/"]
COPY ["iFramework/IFramework.csproj", "iFramework/"]
COPY ["iFramework.Plugins/IFramework.MessageQueue.Client.Abstracts/IFramework.MessageQueue.Client.Abstracts.csproj", "iFramework.Plugins/IFramework.MessageQueue.Client.Abstracts/"]
COPY ["Sample/Sample.DomainEventHandler/Sample.DomainEventSubscriber.csproj", "Sample/Sample.DomainEventHandler/"]
COPY ["Sample/Sample.DomainEvent/Sample.DomainEvents.csproj", "Sample/Sample.DomainEvent/"]
COPY ["Sample/Sample.Command/Sample.Command.csproj", "Sample/Sample.Command/"]
COPY ["Sample/Sample.DTO/Sample.DTO.csproj", "Sample/Sample.DTO/"]
COPY ["iFramework.Plugins/IFramework.Logging.Log4Net/IFramework.Logging.Log4Net.csproj", "iFramework.Plugins/IFramework.Logging.Log4Net/"]
COPY ["iFramework.Plugins/IFramework.AspNet/IFramework.AspNet.csproj", "iFramework.Plugins/IFramework.AspNet/"]
COPY ["iFramework.Plugins/IFramework.EntityFrameworkCore/IFramework.EntityFrameworkCore.csproj", "iFramework.Plugins/IFramework.EntityFrameworkCore/"]
COPY ["Sample/Sample.CommandHandler/Sample.CommandHandler.csproj", "Sample/Sample.CommandHandler/"]
COPY ["Sample/Sample.Persistence/Sample.Persistence.csproj", "Sample/Sample.Persistence/"]
COPY ["iFramework.Plugins/IFramework.MessageStores/IFramework.MessageStores.Relational.csproj", "iFramework.Plugins/IFramework.MessageStores/"]
COPY ["iFramework.Plugins/IFramework.MessageStores.Abstracts/IFramework.MessageStores.Abstracts.csproj", "iFramework.Plugins/IFramework.MessageStores.Abstracts/"]
COPY ["Sample/Sample.Domain/Sample.Domain.csproj", "Sample/Sample.Domain/"]
COPY ["iFramework.Plugins/IFramework.Infrastructure.EventSourcing/IFramework.Infrastructure.EventSourcing.csproj", "iFramework.Plugins/IFramework.Infrastructure.EventSourcing/"]
COPY ["Sample/Sample.ApplicationEvent/Sample.ApplicationEvent.csproj", "Sample/Sample.ApplicationEvent/"]
COPY ["Sample/ApplicationEventSubscriber/Sample.ApplicationEventSubscriber.csproj", "Sample/ApplicationEventSubscriber/"]
COPY ["iFramework.Plugins/IFramework.MessageQueueCore.InMemory/IFramework.MessageQueue.InMemory.csproj", "iFramework.Plugins/IFramework.MessageQueueCore.InMemory/"]
COPY ["iFramework.Plugins/IFramework.Logging.Serilog/IFramework.Logging.Serilog.csproj", "iFramework.Plugins/IFramework.Logging.Serilog/"]
COPY ["iFramework.Plugins/IFramework.DependencyInjection.Unity/IFramework.DependencyInjection.Unity.csproj", "iFramework.Plugins/IFramework.DependencyInjection.Unity/"]
COPY ["iFramework.Plugins/IFramework.MessageQueue.EQueue/IFramework.MessageQueue.EQueue.csproj", "iFramework.Plugins/IFramework.MessageQueue.EQueue/"]
COPY ["Sample/AsyncDomainEventSubscriber/Sample.AsyncDomainEventSubscriber.csproj", "Sample/AsyncDomainEventSubscriber/"]
COPY ["iFramework.Plugins/IFramework.MessageQueue.RabbitMQ/IFramework.MessageQueue.RabbitMQ.csproj", "iFramework.Plugins/IFramework.MessageQueue.RabbitMQ/"]
COPY ["iFramework.Plugins/IFramework.JsonNetCore/IFramework.JsonNet.csproj", "iFramework.Plugins/IFramework.JsonNetCore/"]
COPY ["Sample/Sample.Applications/Sample.Applications.csproj", "Sample/Sample.Applications/"]
COPY ["iFramework.Plugins/IFramework.EventStore.Redis/IFramework.EventStore.Redis.csproj", "iFramework.Plugins/IFramework.EventStore.Redis/"]
COPY ["iFramework.Plugins/IFramework.DependencyInjection.Autofac/IFramework.DependencyInjection.Autofac.csproj", "iFramework.Plugins/IFramework.DependencyInjection.Autofac/"]
RUN dotnet restore "Sample/Sample.CommandServiceCore/Sample.CommandServiceCoreNet5.csproj"
COPY . .
WORKDIR "/src/Sample/Sample.CommandServiceCore"
RUN dotnet build "Sample.CommandServiceCoreNet5.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sample.CommandServiceCoreNet5.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.CommandServiceCore.dll"]