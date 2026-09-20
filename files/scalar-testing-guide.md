# Using Scalar UI to Test JustGo API Access

## Quick Start

### 1. Access the Scalar UI
Navigate to: `https://localhost:56461/scalar/ui`

### 2. Find an Endpoint
- Browse the left sidebar for endpoints (e.g., `/members/{memberId}`)
- Click on the endpoint to expand it
- Read the description and required parameters

### 3. Make a Request
1. Click "Try it" on the endpoint
2. Fill in required parameters (e.g., `memberId: 1`)
3. Scroll to the bottom and look for **"Authorization"** or headers section
4. Add the API key header manually (see below)
5. Click "Send"

## Adding Authentication Headers

Since the API key is currently stored in AppHost secrets and not auto-injected into Scalar:

### Manual Method (Until Auto-Injection is Implemented)

1. In the Scalar request builder, find the **"Headers"** section
2. Add a custom header:
   - **Name**: `X-Api-Key`
   - **Value**: `<your-api-key-from-apphost-secrets>`

Or, if the JustGo API expects it in a different header, check the sync job code:
- Look in `JustGo.Api/Services/JustGoClientExtensions.cs` for how headers are added

### Getting Your API Key

The API key is stored in AppHost user secrets:

```powershell
# View the current API key
cd ..\JustGo
dotnet user-secrets list

# Or check AppHost logs in Aspire dashboard
aspire logs api --follow
```

## Testing API Key Access Differences

### Scenario: Admin vs Single-Club Key

To compare what each key can access:

1. **Start with admin key**:
   - Set `ASPNETCORE_ENVIRONMENT=Development` (default)
   - Ensure `JustGo:ApiKey` is set to your admin key
   - Restart API: `aspire resource api restart`

2. **Test endpoints**:
   - Try `/clubs/search` - should return all clubs
   - Try `/organisations/search` - should return all organisations
   - Try `/members/{id}` - should return member details

3. **Switch to single-club (Mosgiel) key**:
   - Update AppHost secret: `dotnet user-secrets set JustGo:ApiKey <mosgiel-key>`
   - Restart API: `aspire resource api restart`

4. **Test the same endpoints**:
   - Note which endpoints fail or return limited data
   - This shows the scope difference between keys

## Endpoint Categories

### Authentication
- `POST /auth/login` - Authenticate and get session
- `POST /auth/change-password` - Change user password

### Clubs
- `GET /clubs/{clubId}` - Get club details
- `GET /clubs/members` - Get club members
- `GET /clubs/search` - Search clubs (⚠️ currently returns 500)

### Members
- `GET /members/{memberId}` - Get member details
- `GET /members/attributes` - Search by attributes (used by sync job)

### Organisations
- `GET /organisations/{orgId}` - Get org details
- `GET /organisations/search` - Search organisations (⚠️ currently returns 500)
- `GET /organisations/roles` - List organisation roles

## Known Upstream Issues

⚠️ **Blocking API exploration:**
- `/clubs/search` returns 500 error
- `/organisations/search` returns 500 error

**Workaround**: Use entity ID endpoints directly (e.g., `/clubs/{clubId}`)

## Next Steps

1. Implement auto-injection of AppHost secrets into Scalar headers
2. Create `.http` files with pre-configured requests
3. Add API documentation comments to endpoints
4. Document the expected header format in the API codebase
