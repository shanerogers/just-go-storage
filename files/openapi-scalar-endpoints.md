# OpenAPI & Scalar API Testing UI

## Summary

OpenAPI documentation generation and Scalar API reference UI have been successfully added to the JustGo.Api project. This enables interactive API exploration and testing without manual token plumbing.

## Endpoints

### OpenAPI Specification
- **URL**: `https://localhost:56461/openapi/v1.json`
- **Format**: JSON OpenAPI 3.1.0 specification
- **Contains**: All endpoint definitions, request/response schemas, parameters

### Scalar UI (Interactive Explorer)
- **URL**: `https://localhost:56461/scalar/ui`
- **Purpose**: Browse endpoints, view schemas, try requests interactively
- **Features**: 
  - Syntax-highlighted request/response bodies
  - Schema documentation
  - Request builder with parameter validation

## Implementation Details

### Changes Made

1. **NuGet Packages** (`JustGo.Api.csproj`):
   - `Microsoft.AspNetCore.OpenApi` (v10.0.0) - OpenAPI generation
   - `Scalar.AspNetCore` (v1.2.41) - Scalar UI hosting

2. **Package Versions** (`Directory.Packages.props`):
   - Added version pins for both packages (Central Package Management)

3. **Program.cs Middleware**:
   - Added `builder.Services.AddOpenApi()` during configuration
   - Added `application.MapOpenApi()` to expose `/openapi/v1.json`
   - Added `application.MapScalarApiReference()` to host Scalar UI
   - Added using directive: `using Scalar.AspNetCore;`

### Running Locally

```bash
# Start Aspire (if not already running)
aspire start --isolated

# Wait for API to be ready
aspire wait api

# Access endpoints
# - OpenAPI spec: https://localhost:56461/openapi/v1.json
# - Scalar UI: https://localhost:56461/scalar/ui
```

## Known Endpoints Exposed

Sample of endpoints visible in the OpenAPI spec:
- `/admin/cache/clear` - Cache management
- `/auth/login` - Authentication
- `/clubs/{clubId}` - Club details
- `/members/{memberId}` - Member details
- `/organisations/{orgId}` - Organisation details

Full list available in the generated OpenAPI specification.

## Next Steps

**Potential enhancements:**
1. Auto-inject API key from AppHost secrets into Scalar requests
2. Create `.http` files for common club/member/organisation queries
3. Add API documentation comments to endpoint handlers
4. Configure Scalar UI theming/branding

## Testing Status

✅ OpenAPI document generation working
✅ Scalar UI accessible and responsive
✅ All endpoint definitions properly exposed
✅ Development environment ready for API exploration
