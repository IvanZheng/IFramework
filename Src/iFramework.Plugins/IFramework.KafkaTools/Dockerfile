FROM microsoft/aspnetcore AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore AS build
COPY ./ /src

WORKDIR /src/iFramework.Plugins/IFramework.KafkaTools
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "IFramework.KafkaTools.dll"]
