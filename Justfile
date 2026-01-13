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

# Build the SDK
build:
    dotnet build --configuration Release

# Build in debug mode
build-debug:
    dotnet build --configuration Debug

# Run tests
test:
    dotnet test --configuration Release

# Run tests with coverage
test-coverage:
    dotnet test --configuration Release --collect:"XPlat Code Coverage"

# Pack for NuGet
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

# Full CI pipeline (restore, build, test)
ci: restore build test

# Publish to NuGet (requires NUGET_API_KEY env var)
publish: pack
    dotnet nuget push ./nupkg/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
