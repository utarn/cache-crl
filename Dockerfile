FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY CrlCaching/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY CrlCaching/. ./
RUN dotnet publish -c Release -r linux-musl-x64 -o out

# Build runtime image using Alpine
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine
WORKDIR /app

# Install packages needed for AOT compilation
RUN apk add --no-cache icu-libs

# Copy the published app
COPY --from=build-env /app/out .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080

# Run the app (note: with AOT, we run the executable directly)
ENTRYPOINT ["./CrlCaching"]