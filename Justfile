# IVK Skill SDK for C# - Task Automation

# Default recipe
default: build

# Generate SDK from OpenAPI spec
generate-sdk:
    rm -rf src/Ivk.Skill.Sdk/Api src/Ivk.Skill.Sdk/Client src/Ivk.Skill.Sdk/Model
    docker run --rm \
        -v "{{justfile_directory()}}:/local" \
        openapitools/openapi-generator-cli:v7.12.0 generate \
        -i /local/ivk-skill-openapi.json \
        -c /local/openapi-generator-config.yaml \
        -o /local/generated
    cp -r generated/src/Ivk.Skill.Sdk/Api src/Ivk.Skill.Sdk/
    cp -r generated/src/Ivk.Skill.Sdk/Client src/Ivk.Skill.Sdk/
    cp -r generated/src/Ivk.Skill.Sdk/Model src/Ivk.Skill.Sdk/
    rm -rf generated

# Build the SDK (all target frameworks require .NET 6 and 8 SDKs installed)
build:
    dotnet build src/Ivk.Skill.Sdk/Ivk.Skill.Sdk.csproj --configuration Release

# Build all projects (requires all target SDKs)
build-all:
    dotnet build --configuration Release

# Run tests (requires .NET 6 and 8 runtimes for full test coverage)
# Use test-local if you only have one .NET version installed
test:
    dotnet test --configuration Release

# Run tests for a specific framework
# Build with .NET 10 first (required for multi-targeting), then run tests with target runtime
test-net6:
    dotnet build tests/Ivk.Skill.Sdk.Tests.Net6 --configuration Release
    mise x dotnet@6 -- dotnet test tests/Ivk.Skill.Sdk.Tests.Net6 --configuration Release --no-build

test-net8:
    dotnet build tests/Ivk.Skill.Sdk.Tests.Net8 --configuration Release
    mise x dotnet@8 -- dotnet test tests/Ivk.Skill.Sdk.Tests.Net8 --configuration Release --no-build

test-net10:
    dotnet test tests/Ivk.Skill.Sdk.Tests.Net10 --configuration Release
    dotnet test tests/Ivk.Skill.Sdk.Tests --configuration Release

# Pack for NuGet (requires all target SDKs for multi-targeting)
pack:
    dotnet pack src/Ivk.Skill.Sdk/Ivk.Skill.Sdk.csproj --configuration Release -o ./nupkg

# Format code
format:
    dotnet format

# Clean build artifacts
clean:
    dotnet clean
    rm -rf ./nupkg
    rm -rf ./generated

# Restore dependencies
restore:
    dotnet restore

# Run the example application
run-example:
    dotnet run --project src/Ivk.Skill.Sdk.Example

# Full CI pipeline (restore, build, test) - requires all SDKs
ci: restore build-all test

# Publish to NuGet (requires NUGET_API_KEY env var)
publish: pack
    dotnet nuget push ./nupkg/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
