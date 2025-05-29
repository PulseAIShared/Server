# Use the .NET Runtime image as the base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

# Build the application in a separate stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the solution and project files
COPY PulseAi.sln ./
COPY Web.Api/*.csproj Web.Api/
COPY Application/*.csproj Application/
COPY Domain/*.csproj Domain/
COPY Infrastructure/*.csproj Infrastructure/
COPY SharedKernel/*.csproj SharedKernel/
COPY DataTransferObjects/*.csproj DataTransferObjects/
# Restore dependencies
RUN dotnet restore PulseAi.sln

# Copy the rest of the files
COPY . ./

# Build and publish the application
WORKDIR /src/Web.Api
RUN dotnet publish -c Release -o /app/publish

# Final stage using the Runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Copy entrypoint and wait-for-it scripts
COPY Web.Api/entrypoint.sh .
COPY Web.Api/wait-for-it.sh .
RUN chmod +x ./entrypoint.sh ./wait-for-it.sh

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["./entrypoint.sh"]
