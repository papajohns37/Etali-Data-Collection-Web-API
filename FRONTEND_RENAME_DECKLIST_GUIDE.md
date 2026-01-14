# Frontend Implementation Guide: Rename Decklist Feature

## Overview

This guide explains how to implement the decklist rename feature in your frontend application. The backend provides a REST API endpoint that allows authenticated users to rename decklists with proper authorization controls.

## Table of Contents

1. [API Endpoint Details](#api-endpoint-details)
2. [Authentication Requirements](#authentication-requirements)
3. [Authorization Rules](#authorization-rules)
4. [Implementation Examples](#implementation-examples)
5. [Error Handling](#error-handling)
6. [UI/UX Recommendations](#uiux-recommendations)
7. [Testing Checklist](#testing-checklist)

---

## API Endpoint Details

### Endpoint
```
PATCH /decklist/{id}/rename
```

### Parameters

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `id` | integer | Path | Yes | The ID of the decklist to rename |
| `newName` | string | Query | Yes | The new name for the decklist (1-500 characters) |

### Request Headers
```http
Authorization: Bearer {jwt-token}
Content-Type: application/json
```

### Full Request Example
```http
PATCH /decklist/123/rename?newName=My%20Updated%20Deck HTTP/1.1
Host: api.example.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Authentication Requirements

### JWT Token
All requests must include a valid JWT bearer token in the Authorization header.

**How to get a token:**
```javascript
// Login request
const response = await fetch('/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    username: 'your-username',
    password: 'your-password'
  })
});

const { token, expiration } = await response.json();
// Store token securely (e.g., in memory, sessionStorage, or httpOnly cookie)
```

**Token Storage Options:**
- ? **Recommended**: In-memory storage for SPA, or httpOnly cookie
- ?? **Not Recommended**: localStorage (vulnerable to XSS)

---

## Authorization Rules

### Who Can Rename Decklists?

```
???????????????????????????????????????????????????????????
?                  Authorization Matrix                    ?
???????????????????????????????????????????????????????????
? User Role   ? Can Rename?                               ?
???????????????????????????????????????????????????????????
? Admin       ? ? ANY decklist                           ?
?             ?                                            ?
? Contributor ? ? Only decklists they created            ?
?             ? ? Decklists created by others            ?
?             ? ? Legacy decklists (no creator)          ?
?             ?                                            ?
? Anonymous   ? ? Cannot rename any decklist             ?
???????????????????????????????????????????????????????????
```

### Determining Rename Permissions in UI

When displaying a decklist, check if the current user can rename it:

```javascript
function canRenameDecklist(decklist, currentUser) {
  // Not logged in
  if (!currentUser) {
    return false;
  }
  
  // Admin can rename anything
  if (currentUser.roles.includes('Admin')) {
    return true;
  }
  
  // Contributor can only rename their own decklists
  if (currentUser.roles.includes('Contributor')) {
    return decklist.createdByUserId === currentUser.id;
  }
  
  return false;
}
```

**Example Usage:**
```javascript
// In your component
const showRenameButton = canRenameDecklist(decklist, currentUser);

// In React
{showRenameButton && (
  <button onClick={handleRename}>Rename</button>
)}
```

---

## Implementation Examples

### React/TypeScript Example

```typescript
interface Decklist {
  id: number;
  name: string;
  cards: Card[];
  moxfieldUrl?: string;
  moxfieldId?: string;
  createdAt: string;
  createdByUserId?: string;
}

interface RenameResponse {
  message: string;
  id: number;
  oldName: string;
  newName: string;
}

// API service function
async function renameDeck(
  decklistId: number, 
  newName: string, 
  token: string
): Promise<RenameResponse> {
  const response = await fetch(
    `/decklist/${decklistId}/rename?newName=${encodeURIComponent(newName)}`,
    {
      method: 'PATCH',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    }
  );

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.detail || 'Failed to rename decklist');
  }

  return response.json();
}

// React component
function DecklistCard({ decklist, currentUser, onRename }) {
  const [isRenaming, setIsRenaming] = useState(false);
  const [newName, setNewName] = useState(decklist.name);
  const [error, setError] = useState<string | null>(null);

  const canRename = useMemo(
    () => canRenameDecklist(decklist, currentUser),
    [decklist, currentUser]
  );

  const handleSubmitRename = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    // Client-side validation
    if (!newName.trim()) {
      setError('Name cannot be empty');
      return;
    }

    if (newName.length > 500) {
      setError('Name cannot exceed 500 characters');
      return;
    }

    try {
      const result = await renameDeck(
        decklist.id,
        newName,
        currentUser.token
      );
      
      // Update local state or trigger parent refresh
      onRename(result);
      setIsRenaming(false);
      
      // Optional: Show success toast
      toast.success(`Renamed to "${result.newName}"`);
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div className="decklist-card">
      {isRenaming ? (
        <form onSubmit={handleSubmitRename}>
          <input
            type="text"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            maxLength={500}
            autoFocus
          />
          {error && <span className="error">{error}</span>}
          <button type="submit">Save</button>
          <button type="button" onClick={() => setIsRenaming(false)}>
            Cancel
          </button>
        </form>
      ) : (
        <>
          <h3>{decklist.name}</h3>
          {canRename && (
            <button onClick={() => setIsRenaming(true)}>Rename</button>
          )}
        </>
      )}
    </div>
  );
}
```

### Vue.js Example

```vue
<template>
  <div class="decklist-card">
    <div v-if="isRenaming" class="rename-form">
      <input
        v-model="newName"
        type="text"
        maxlength="500"
        @keyup.enter="handleRename"
        @keyup.esc="cancelRename"
        ref="nameInput"
      />
      <span v-if="error" class="error">{{ error }}</span>
      <button @click="handleRename">Save</button>
      <button @click="cancelRename">Cancel</button>
    </div>
    
    <div v-else>
      <h3>{{ decklist.name }}</h3>
      <button 
        v-if="canRename" 
        @click="startRename"
        title="Rename this decklist"
      >
        Rename
      </button>
    </div>
  </div>
</template>

<script>
export default {
  props: {
    decklist: Object,
    currentUser: Object,
  },
  
  data() {
    return {
      isRenaming: false,
      newName: this.decklist.name,
      error: null,
    };
  },
  
  computed: {
    canRename() {
      if (!this.currentUser) return false;
      
      if (this.currentUser.roles.includes('Admin')) {
        return true;
      }
      
      if (this.currentUser.roles.includes('Contributor')) {
        return this.decklist.createdByUserId === this.currentUser.id;
      }
      
      return false;
    },
  },
  
  methods: {
    startRename() {
      this.isRenaming = true;
      this.newName = this.decklist.name;
      this.$nextTick(() => {
        this.$refs.nameInput?.focus();
      });
    },
    
    cancelRename() {
      this.isRenaming = false;
      this.newName = this.decklist.name;
      this.error = null;
    },
    
    async handleRename() {
      this.error = null;
      
      // Validation
      if (!this.newName.trim()) {
        this.error = 'Name cannot be empty';
        return;
      }
      
      if (this.newName.length > 500) {
        this.error = 'Name cannot exceed 500 characters';
        return;
      }
      
      try {
        const response = await fetch(
          `/decklist/${this.decklist.id}/rename?newName=${encodeURIComponent(this.newName)}`,
          {
            method: 'PATCH',
            headers: {
              'Authorization': `Bearer ${this.currentUser.token}`,
            },
          }
        );
        
        if (!response.ok) {
          const error = await response.json();
          throw new Error(error.detail || 'Failed to rename');
        }
        
        const result = await response.json();
        
        // Emit event to parent
        this.$emit('renamed', result);
        this.isRenaming = false;
        
        // Optional: Show success notification
        this.$toast.success(`Renamed to "${result.newName}"`);
      } catch (err) {
        this.error = err.message;
      }
    },
  },
};
</script>
```

### Plain JavaScript/Fetch Example

```javascript
/**
 * Rename a decklist
 * @param {number} decklistId - The ID of the decklist
 * @param {string} newName - The new name (1-500 characters)
 * @param {string} authToken - JWT bearer token
 * @returns {Promise<Object>} Response with message, id, oldName, newName
 */
async function renameDecklist(decklistId, newName, authToken) {
  // Input validation
  if (!newName || !newName.trim()) {
    throw new Error('Decklist name cannot be empty');
  }
  
  if (newName.length > 500) {
    throw new Error('Decklist name cannot exceed 500 characters');
  }
  
  // Make API request
  const response = await fetch(
    `/decklist/${decklistId}/rename?newName=${encodeURIComponent(newName)}`,
    {
      method: 'PATCH',
      headers: {
        'Authorization': `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      },
    }
  );
  
  // Handle response
  if (!response.ok) {
    const errorData = await response.json();
    throw new Error(errorData.detail || `HTTP ${response.status}: ${response.statusText}`);
  }
  
  return await response.json();
}

// Usage example
async function handleRenameDeck() {
  const decklistId = 123;
  const newName = document.getElementById('deckName').value;
  const token = getUserToken(); // Your token retrieval function
  
  try {
    const result = await renameDecklist(decklistId, newName, token);
    console.log('Success:', result);
    // Update UI with result.newName
    document.getElementById('deckTitle').textContent = result.newName;
    showSuccessMessage(`Renamed to "${result.newName}"`);
  } catch (error) {
    console.error('Error:', error);
    showErrorMessage(error.message);
  }
}
```

---

## Error Handling

### HTTP Status Codes and Responses

| Status | Meaning | Response Structure | Action |
|--------|---------|-------------------|--------|
| 200 | Success | `{ message, id, oldName, newName }` | Update UI, show success |
| 400 | Bad Request | `{ status, title, detail }` | Show validation error |
| 401 | Unauthorized | (empty or minimal) | Redirect to login or show "Permission denied" |
| 404 | Not Found | `{ status, title, detail }` | Show "Decklist not found" |
| 500 | Server Error | `{ status, title, detail }` | Show "Server error, try again" |

### Detailed Error Handling

```typescript
interface ApiError {
  status: number;
  title: string;
  detail: string;
}

async function handleRenameWithErrors(
  decklistId: number,
  newName: string,
  token: string
) {
  try {
    const response = await fetch(
      `/decklist/${decklistId}/rename?newName=${encodeURIComponent(newName)}`,
      {
        method: 'PATCH',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      }
    );

    // Success
    if (response.ok) {
      return await response.json();
    }

    // Handle specific error cases
    switch (response.status) {
      case 400: {
        const error: ApiError = await response.json();
        // Validation errors - show to user
        throw new Error(error.detail || 'Invalid decklist name');
      }

      case 401: {
        // Not authorized - either not logged in or not the owner
        // Check if token is expired
        if (isTokenExpired(token)) {
          throw new Error('SESSION_EXPIRED');
        }
        throw new Error('You do not have permission to rename this decklist');
      }

      case 404: {
        const error: ApiError = await response.json();
        throw new Error('Decklist not found');
      }

      case 500: {
        throw new Error('Server error. Please try again later.');
      }

      default: {
        throw new Error(`Unexpected error: ${response.status}`);
      }
    }
  } catch (error) {
    // Network errors
    if (error instanceof TypeError) {
      throw new Error('Network error. Check your connection.');
    }
    
    // Re-throw other errors
    throw error;
  }
}

// Usage with user-friendly error handling
try {
  await handleRenameWithErrors(deckId, newName, token);
  toast.success('Decklist renamed successfully!');
} catch (error) {
  if (error.message === 'SESSION_EXPIRED') {
    // Redirect to login
    router.push('/login');
    toast.info('Please log in again');
  } else {
    // Show error to user
    toast.error(error.message);
  }
}
```

---

## UI/UX Recommendations

### 1. Show/Hide Rename Button Based on Permissions

```javascript
// Only show rename button if user has permission
if (canRenameDecklist(decklist, currentUser)) {
  // Show rename button or inline edit icon
}
```

### 2. Inline Editing Pattern

**Good UX:**
```
???????????????????????????????????????
?  My Awesome Deck  ??                ?  ? Click pencil to edit
???????????????????????????????????????

          ? (Click edit)

???????????????????????????????????????
?  [My Awesome Deck        ] ? ?     ?  ? Input with save/cancel
???????????????????????????????????????
```

### 3. Validation Feedback

```javascript
// Real-time character counter
<input 
  value={name}
  onChange={e => setName(e.target.value)}
  maxLength={500}
/>
<span className="char-count">
  {name.length}/500 characters
</span>
```

### 4. Loading States

```javascript
const [isRenaming, setIsRenaming] = useState(false);

<button 
  onClick={handleRename}
  disabled={isRenaming}
>
  {isRenaming ? 'Saving...' : 'Save'}
</button>
```

### 5. Optimistic Updates

```javascript
// Update UI immediately, rollback on error
const originalName = decklist.name;
setDecklistName(newName); // Optimistic update

try {
  await renameDeck(decklistId, newName, token);
  // Success - UI already updated
} catch (error) {
  setDecklistName(originalName); // Rollback
  showError(error.message);
}
```

### 6. Keyboard Shortcuts

```javascript
// Enter to save, Escape to cancel
<input
  onKeyDown={(e) => {
    if (e.key === 'Enter') handleSave();
    if (e.key === 'Escape') handleCancel();
  }}
/>
```

### 7. Visual Indicators for Ownership

```html
<!-- Show indicator for owned decklists -->
<div class="decklist-card">
  <h3>
    {decklist.name}
    {isOwner && <span class="owner-badge">Your deck</span>}
  </h3>
</div>
```

---

## Testing Checklist

### Functional Tests

- [ ] **Rename as Admin**
  - [ ] Can rename own decklist
  - [ ] Can rename other users' decklists
  - [ ] Can rename legacy decklists (no owner)

- [ ] **Rename as Contributor**
  - [ ] Can rename own decklist
  - [ ] Cannot rename other users' decklists (401 error shown)
  - [ ] Cannot rename legacy decklists (401 error shown)

- [ ] **Rename without Authentication**
  - [ ] Button not visible when not logged in
  - [ ] API returns 401 if called directly

- [ ] **Input Validation**
  - [ ] Empty name rejected (client-side)
  - [ ] Whitespace-only name rejected
  - [ ] 500+ character name rejected
  - [ ] Valid name accepted (1-500 chars)

- [ ] **Error Handling**
  - [ ] Network error handled gracefully
  - [ ] 404 error (decklist not found) handled
  - [ ] 401 error (unauthorized) handled
  - [ ] 400 error (validation) handled
  - [ ] Server error handled

### UI/UX Tests

- [ ] **Visual States**
  - [ ] Rename button shows only when permitted
  - [ ] Edit mode UI clear and intuitive
  - [ ] Loading state visible during save
  - [ ] Success feedback shown
  - [ ] Error feedback shown

- [ ] **Interactions**
  - [ ] Click edit enters edit mode
  - [ ] Enter key saves
  - [ ] Escape key cancels
  - [ ] Click outside cancels (optional)
  - [ ] Cannot edit if already editing another deck

### Edge Cases

- [ ] **Concurrent Edits**
  - [ ] Handle if deck deleted while editing
  - [ ] Handle if deck renamed by someone else

- [ ] **Session Management**
  - [ ] Handle expired token gracefully
  - [ ] Redirect to login if needed

- [ ] **Special Characters**
  - [ ] Names with quotes work
  - [ ] Names with unicode characters work
  - [ ] Names with emojis work (if allowed)

---

## Complete Working Example (React + TypeScript)

Here's a complete, production-ready component:

```typescript
import React, { useState, useCallback, useEffect, useRef } from 'react';
import { useAuth } from './hooks/useAuth'; // Your auth hook
import './DecklistCard.css';

interface Decklist {
  id: number;
  name: string;
  createdByUserId?: string;
  // ... other fields
}

interface DecklistCardProps {
  decklist: Decklist;
  onUpdate: (decklist: Decklist) => void;
}

export function DecklistCard({ decklist, onUpdate }: DecklistCardProps) {
  const { user, token } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [name, setName] = useState(decklist.name);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Check permissions
  const canRename = useCallback(() => {
    if (!user) return false;
    if (user.roles.includes('Admin')) return true;
    if (user.roles.includes('Contributor')) {
      return decklist.createdByUserId === user.id;
    }
    return false;
  }, [user, decklist.createdByUserId]);

  // Focus input when entering edit mode
  useEffect(() => {
    if (isEditing) {
      inputRef.current?.focus();
      inputRef.current?.select();
    }
  }, [isEditing]);

  const handleStartEdit = () => {
    setIsEditing(true);
    setName(decklist.name);
    setError(null);
  };

  const handleCancel = () => {
    setIsEditing(false);
    setName(decklist.name);
    setError(null);
  };

  const handleSave = async () => {
    // Client-side validation
    const trimmedName = name.trim();
    
    if (!trimmedName) {
      setError('Name cannot be empty');
      return;
    }

    if (trimmedName.length > 500) {
      setError('Name cannot exceed 500 characters');
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const response = await fetch(
        `/decklist/${decklist.id}/rename?newName=${encodeURIComponent(trimmedName)}`,
        {
          method: 'PATCH',
          headers: {
            'Authorization': `Bearer ${token}`,
          },
        }
      );

      if (!response.ok) {
        if (response.status === 401) {
          throw new Error('You do not have permission to rename this decklist');
        }
        if (response.status === 404) {
          throw new Error('Decklist not found');
        }
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Failed to rename decklist');
      }

      const result = await response.json();
      
      // Update parent component
      onUpdate({
        ...decklist,
        name: result.newName,
      });

      setIsEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setIsSaving(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !isSaving) {
      handleSave();
    } else if (e.key === 'Escape') {
      handleCancel();
    }
  };

  return (
    <div className="decklist-card">
      <div className="decklist-header">
        {isEditing ? (
          <div className="rename-form">
            <input
              ref={inputRef}
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              onKeyDown={handleKeyDown}
              maxLength={500}
              disabled={isSaving}
              className="deck-name-input"
              aria-label="Decklist name"
            />
            <div className="char-counter" aria-live="polite">
              {name.length}/500
            </div>
            {error && (
              <div className="error-message" role="alert">
                {error}
              </div>
            )}
            <div className="button-group">
              <button
                onClick={handleSave}
                disabled={isSaving}
                className="btn-primary"
                aria-label="Save changes"
              >
                {isSaving ? 'Saving...' : 'Save'}
              </button>
              <button
                onClick={handleCancel}
                disabled={isSaving}
                className="btn-secondary"
                aria-label="Cancel"
              >
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <div className="deck-title-row">
            <h3 className="deck-name">{decklist.name}</h3>
            {canRename() && (
              <button
                onClick={handleStartEdit}
                className="btn-icon"
                aria-label="Rename decklist"
                title="Rename this decklist"
              >
                ??
              </button>
            )}
          </div>
        )}
      </div>
      {/* Rest of your card content */}
    </div>
  );
}
```

---

## Additional Resources

### API Documentation
- Full API documentation available at `/swagger` endpoint
- Interactive testing available in Swagger UI

### Related Endpoints
- `POST /auth/login` - Get authentication token
- `GET /decklists` - Get all decklists (see `createdByUserId`)
- `POST /decklist` - Create new decklist

### Support
For questions or issues:
1. Check the Swagger documentation
2. Review error messages in the API response
3. Check browser console for client-side errors
4. Verify JWT token is valid and not expired

---

## Quick Reference

```typescript
// Minimal working example
async function renameDeck(id: number, newName: string, token: string) {
  const response = await fetch(
    `/decklist/${id}/rename?newName=${encodeURIComponent(newName)}`,
    {
      method: 'PATCH',
      headers: { 'Authorization': `Bearer ${token}` },
    }
  );
  
  if (!response.ok) throw new Error('Rename failed');
  return response.json();
}
```

**Remember:**
- ? Always URL-encode the `newName` parameter
- ? Include `Bearer` prefix in Authorization header
- ? Validate input length (1-500 characters)
- ? Handle 401 errors gracefully (permission denied)
- ? Show appropriate UI based on user permissions

