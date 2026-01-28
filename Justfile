# IVK Skill SDK for C# - Task Automation

# Default recipe
default: build

# Generate SDK from OpenAPI spec
generate-sdk: _generate-sdk-raw _fix-emit-default-values

# Internal: Run OpenAPI generator
_generate-sdk-raw:
    rm -rf src/Invokation.Skill.Sdk/Api src/Invokation.Skill.Sdk/Client src/Invokation.Skill.Sdk/Model
    docker run --rm \
        -u "$(id -u):$(id -g)" \
        -v "{{justfile_directory()}}:/local" \
        openapitools/openapi-generator-cli:v7.12.0 generate \
        -i /local/ivk-skill-openapi.json \
        -c /local/openapi-generator-config.yaml \
        -o /local/generated
    cp -r generated/src/Invokation.Skill.Sdk/Api src/Invokation.Skill.Sdk/
    cp -r generated/src/Invokation.Skill.Sdk/Client src/Invokation.Skill.Sdk/
    cp -r generated/src/Invokation.Skill.Sdk/Model src/Invokation.Skill.Sdk/
    rm -rf generated

# Fix EmitDefaultValue attributes in generated models
# The OpenAPI generator's optionalEmitDefaultValues option doesn't work correctly,
# so we post-process the files to set EmitDefaultValue = false for optional fields
_fix-emit-default-values:
    #!/usr/bin/env bash
    set -euo pipefail
    echo "Fixing EmitDefaultValue attributes in Model files..."
    find src/Invokation.Skill.Sdk/Model -name "*.cs" -exec sed -i '' \
        's/EmitDefaultValue = true/EmitDefaultValue = false/g' {} \;
    echo "Done. Changed $(grep -r 'EmitDefaultValue = false' src/Invokation.Skill.Sdk/Model | wc -l | tr -d ' ') attributes to EmitDefaultValue = false"

# Build the SDK (all target frameworks require .NET 6 and 8 SDKs installed)
build:
    dotnet build src/Invokation.Skill.Sdk/Invokation.Skill.Sdk.csproj --configuration Release

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
    dotnet build tests/Invokation.Skill.Sdk.Tests.Net6 --configuration Release
    mise x dotnet@6 -- dotnet test tests/Invokation.Skill.Sdk.Tests.Net6 --configuration Release --no-build

test-net8:
    dotnet build tests/Invokation.Skill.Sdk.Tests.Net8 --configuration Release
    mise x dotnet@8 -- dotnet test tests/Invokation.Skill.Sdk.Tests.Net8 --configuration Release --no-build

test-net10:
    dotnet test tests/Invokation.Skill.Sdk.Tests.Net10 --configuration Release
    dotnet test tests/Invokation.Skill.Sdk.Tests --configuration Release

# Pack for NuGet (requires all target SDKs for multi-targeting)
pack:
    dotnet pack src/Invokation.Skill.Sdk/Invokation.Skill.Sdk.csproj --configuration Release -o ./nupkg

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
    dotnet run --project src/Invokation.Skill.Sdk.Example

# Full CI pipeline (restore, build, test) - requires all SDKs
ci: restore build-all test

# Publish to NuGet (requires NUGET_API_KEY env var)
publish: pack
    dotnet nuget push ./nupkg/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
