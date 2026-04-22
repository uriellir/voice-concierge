FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/api/VoiceConcierge/VoiceConcierge.csproj src/api/VoiceConcierge/
RUN dotnet restore src/api/VoiceConcierge/VoiceConcierge.csproj

COPY src/api/VoiceConcierge/ src/api/VoiceConcierge/
RUN dotnet publish src/api/VoiceConcierge/VoiceConcierge.csproj -c Release -o /app/publish /p:UseAppHost=false
RUN mkdir -p /app/publish/Data/Seed && cp src/api/VoiceConcierge/Data/Seed/meridian-property-data.csv /app/publish/Data/Seed/meridian-property-data.csv

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VoiceConcierge.dll"]
