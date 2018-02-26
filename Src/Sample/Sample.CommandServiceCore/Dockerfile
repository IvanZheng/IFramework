FROM microsoft/dotnet:2.0-sdk AS build-env 
COPY ./ /src
WORKDIR /src/Sample/Sample.CommandServiceCore

RUN dotnet publish -c Release -o out

FROM microsoft/aspnetcore
WORKDIR /app
COPY --from=build-env /src/Sample/Sample.CommandServiceCore/out ./
EXPOSE 80
ENTRYPOINT ["dotnet", "Sample.CommandServiceCore.dll"]
