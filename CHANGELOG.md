# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2024-01-13

### Added

- Initial release of the IVK Skill SDK for C#
- `SkillSdk` wrapper class with builder pattern
- Async and sync API methods:
  - `PostMatchResultAsync` / `PostMatchResult`
  - `PostPreMatchAsync` / `PostPreMatch`
  - `GetConfigurationAsync` / `GetConfiguration`
- Automatic retry with exponential backoff via Polly
- API key authentication
- Support for .NET 6, .NET 8, .NET 10, and .NET Standard 2.1
- OpenAPI-generated models for all API types
- Unit tests with xUnit
- GitHub Actions workflows for CI/CD
- Example application demonstrating SDK usage
