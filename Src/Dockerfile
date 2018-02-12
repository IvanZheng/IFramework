FROM microsoft/dotnet:2.0-runtime
COPY . /app
WORKDIR /app/src/Sample/Sample.CommandServiceCore
RUN dotnet publish -c Release -o out
WORKDIR /app
COPY /app/src/Sample/Sample.CommandServiceCore/out ./
EXPOSE 80
ENTRYPOINT ["dotnet", "Sample.CommandServiceCore.dll"]