FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR BellevueAllianceBot

# Copy everything else and build
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR BellevueAllianceBot
COPY BellevueAllianceBot/*.json ./
COPY --from=build-env /BellevueAllianceBot/token .
COPY --from=build-env /BellevueAllianceBot/out .

# Run the app on container startup
EXPOSE 8080/tcp
ENTRYPOINT [ "dotnet", "BellevueAllianceBot.dll" ]