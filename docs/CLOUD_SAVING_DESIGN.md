# Cloud Saving Design Document

## Overview

This document outlines the architecture and implementation plan for cloud synchronization features in AnimeQuoteWall. The cloud saving feature will allow users to sync their quotes, backgrounds, wallpapers, and settings across multiple devices.

## Architecture Overview

### High-Level Design

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────────┐
│   Client App    │◄───────►│   Cloud Service  │◄───────►│   Database      │
│  (WPF Desktop)  │         │   (REST API)      │         │   (PostgreSQL)  │
└─────────────────┘         └──────────────────┘         └─────────────────┘
       │                              │
       │                              │
       ▼                              ▼
┌─────────────────┐         ┌──────────────────┐
│  Local Storage  │         │  Authentication  │
│  (JSON Files)   │         │  (OAuth 2.0)     │
└─────────────────┘         └──────────────────┘
```

## Data Synchronization Strategy

### Sync Entities

1. **Quotes** (`quotes.json`)
   - Full sync: Upload/download entire quote collection
   - Conflict resolution: Last-write-wins with timestamp comparison
   - Metadata: LastModified timestamp, DeviceId

2. **Background Images**
   - Upload: Individual image files with metadata
   - Download: List available, download on-demand
   - Storage: Cloud storage (S3/Azure Blob) with CDN

3. **Wallpaper History**
   - Selective sync: User chooses which wallpapers to sync
   - Thumbnail generation: Store thumbnails, full images optional
   - Metadata: Timestamp, quote reference, settings

4. **User Settings** (`settings.json`)
   - Full sync: Upload/download settings
   - Device-specific: Some settings (paths) remain local

### Sync Modes

1. **Manual Sync**: User-triggered sync on demand
2. **Automatic Sync**: Background sync when changes detected
3. **Selective Sync**: User chooses what to sync

## API Design (REST Endpoints)

### Base URL
```
https://api.animequotewall.com/v1
```

### Authentication
- OAuth 2.0 with PKCE flow
- JWT tokens for API requests
- Refresh token rotation

### Endpoints

#### Quotes
```
GET    /quotes                    - Get all quotes
POST   /quotes                    - Create/update quotes (batch)
DELETE /quotes/{id}               - Delete quote
GET    /quotes/sync               - Get sync status
POST   /quotes/sync               - Initiate sync
```

#### Backgrounds
```
GET    /backgrounds               - List backgrounds
POST   /backgrounds               - Upload background
GET    /backgrounds/{id}/download - Download background
DELETE /backgrounds/{id}          - Delete background
```

#### Wallpapers
```
GET    /wallpapers                - List wallpaper history
POST   /wallpapers                - Upload wallpaper
GET    /wallpapers/{id}/download  - Download wallpaper
DELETE /wallpapers/{id}           - Delete wallpaper
```

#### Settings
```
GET    /settings                  - Get user settings
PUT    /settings                  - Update settings
```

#### Sync
```
GET    /sync/status               - Get sync status
POST   /sync/start                - Start sync operation
GET    /sync/history              - Get sync history
```

## Data Structures

### Quote Sync Payload
```json
{
  "quotes": [
    {
      "id": "uuid",
      "text": "quote text",
      "character": "character name",
      "anime": "anime name",
      "categories": ["category1", "category2"],
      "tags": ["tag1", "tag2"],
      "isFavorite": false,
      "rating": 0,
      "lastModified": "2024-01-01T00:00:00Z",
      "deviceId": "device-uuid"
    }
  ],
  "lastSyncTimestamp": "2024-01-01T00:00:00Z"
}
```

### Sync Status Response
```json
{
  "lastSyncTime": "2024-01-01T00:00:00Z",
  "quotesCount": 100,
  "backgroundsCount": 50,
  "wallpapersCount": 20,
  "conflicts": [
    {
      "entity": "quote",
      "entityId": "uuid",
      "localModified": "2024-01-01T00:00:00Z",
      "remoteModified": "2024-01-02T00:00:00Z"
    }
  ]
}
```

## Conflict Resolution

### Strategy: Last-Write-Wins with Conflict Detection

1. **Detection**: Compare `lastModified` timestamps
2. **Resolution Options**:
   - **Last-Write-Wins**: Use most recent version (default)
   - **Manual Resolution**: Present conflicts to user
   - **Merge**: Attempt automatic merge (for quotes)

### Conflict UI Flow

```
Conflict Detected
    ↓
Show Conflict Dialog
    ↓
User Chooses:
  - Keep Local
  - Use Remote
  - Merge (if applicable)
    ↓
Resolve and Sync
```

## Authentication/Authorization

### OAuth 2.0 Flow

1. **Authorization Request**: User clicks "Sign In"
2. **Redirect**: Open browser to authorization server
3. **User Consent**: User grants permissions
4. **Authorization Code**: Redirected back to app
5. **Token Exchange**: Exchange code for access/refresh tokens
6. **Storage**: Securely store tokens (Windows Credential Manager)

### Token Management

- **Access Token**: Short-lived (1 hour), used for API requests
- **Refresh Token**: Long-lived (30 days), used to get new access tokens
- **Storage**: Windows Credential Manager (encrypted)
- **Rotation**: Refresh tokens rotated on each use

## Privacy and Security

### Data Encryption

- **In Transit**: TLS 1.3 for all API communications
- **At Rest**: Server-side encryption (AES-256)
- **Local Storage**: Windows DPAPI for sensitive data

### Privacy Considerations

- **User Data**: User owns all data, can delete at any time
- **Analytics**: Opt-in only, anonymized
- **Third-Party**: No data sharing with third parties
- **GDPR Compliance**: Right to access, delete, export data

### Security Measures

- **Rate Limiting**: Prevent abuse
- **Input Validation**: Sanitize all inputs
- **SQL Injection**: Parameterized queries
- **XSS Prevention**: Content Security Policy
- **CORS**: Restrict cross-origin requests

## Implementation Roadmap

### Phase 1: Foundation (Months 1-2)
- [ ] Authentication system (OAuth 2.0)
- [ ] Basic API client library
- [ ] Local sync state tracking
- [ ] Manual sync for quotes

### Phase 2: Core Features (Months 3-4)
- [ ] Background image sync
- [ ] Wallpaper history sync
- [ ] Settings sync
- [ ] Conflict resolution UI

### Phase 3: Advanced Features (Months 5-6)
- [ ] Automatic background sync
- [ ] Selective sync
- [ ] Sync history and status
- [ ] Offline mode support

### Phase 4: Polish (Months 7-8)
- [ ] Performance optimization
- [ ] Error handling improvements
- [ ] User documentation
- [ ] Beta testing

## Technical Stack

### Client (WPF)
- **HTTP Client**: `HttpClient` with retry policies
- **JSON**: `System.Text.Json`
- **Storage**: Windows Credential Manager for tokens
- **Sync Engine**: Custom implementation with conflict detection

### Server (Future)
- **API**: ASP.NET Core Web API
- **Database**: PostgreSQL
- **Storage**: Azure Blob Storage / AWS S3
- **Authentication**: IdentityServer / Auth0
- **CDN**: CloudFront / Azure CDN

## Error Handling

### Sync Errors

1. **Network Errors**: Retry with exponential backoff
2. **Authentication Errors**: Refresh token, re-authenticate if needed
3. **Conflict Errors**: Present to user for resolution
4. **Server Errors**: Log and notify user

### Error Recovery

- **Partial Sync**: Resume from last successful item
- **Offline Mode**: Queue changes, sync when online
- **Data Validation**: Validate before sync, reject invalid data

## Performance Considerations

### Optimization Strategies

1. **Incremental Sync**: Only sync changed items
2. **Compression**: Compress JSON payloads (gzip)
3. **Batching**: Batch multiple operations
4. **Caching**: Cache frequently accessed data
5. **Background Processing**: Sync in background thread

### Bandwidth Management

- **Thumbnails**: Use thumbnails for wallpaper previews
- **Progressive Download**: Download images on-demand
- **Compression**: Compress images before upload
- **Delta Sync**: Only sync differences

## Testing Strategy

### Unit Tests
- Sync logic
- Conflict resolution
- Data transformation

### Integration Tests
- API client
- Authentication flow
- Sync operations

### End-to-End Tests
- Full sync workflow
- Conflict resolution flow
- Error recovery

## Future Enhancements

1. **Multi-Device Sync**: Real-time sync across devices
2. **Sharing**: Share quotes/wallpapers with other users
3. **Backup/Restore**: Full backup and restore functionality
4. **Version History**: Track changes over time
5. **Collaboration**: Multiple users editing same collection

## Notes

- This is a design document for future implementation
- Current implementation does not include cloud features
- All cloud features are optional and opt-in
- Local-first approach: app works fully offline

