# Authentication

## Scope and Status

This document is the source of truth for authentication business rules and user-visible app behavior. See [Requirements](../requirements.md) for the wider product context, [Architecture](../architecture.md#app-authentication-and-session-handling) for technical implementation details, and [Manual authentication tests](../testing/authentication.md) for verification scenarios.

| Capability | Current app status |
| --- | --- |
| Register a new account and sign in automatically | Implemented |
| Log in with an existing account | Implemented |
| Log out, including a warning when server confirmation fails | Implemented |
| Claim an imported participant | Not available in the app |
| Restore a session automatically when the app starts | Deferred |

## Registration

### Input Rules

- First name and last name are required, with a maximum of 50 characters each after trimming.
- A valid email address is required, with a maximum of 320 characters after trimming.
- The password must contain at least eight characters, a digit and a special character, and must not exceed 200 characters.
- Password confirmation must match the password.
- The email address must not already be used by another account.

The 200-character password maximum mirrors the existing registration contract. This document does not establish a new limit or provide a rationale for the original choice.

### Successful Flow

1. The user opens registration and chooses no previous participation.
2. The user enters the required details and submits the form.
3. While the request is running, inputs are disabled and duplicate submissions are prevented.
4. After account creation and successful session storage, the user is signed in automatically and sees the home screen with their name. No separate login is needed.
5. Password and confirmation are cleared.

### Validation and Failure Cases

- Invalid or missing input prevents submission until the form is valid.
- A duplicate email address is reported in the form; the user remains unauthenticated.
- Backend failures are displayed in the form.
- If the account was created but session storage fails, the app reports that sign-in could not be completed and recommends normal login instead of registering again. Password and confirmation are cleared.
- A network failure or unreadable response may leave account creation uncertain. The app recommends trying login first rather than assuming that registration failed.
- After a failed attempt finishes, the form controls become available again.

## Login

- Users can sign in with an existing backend account.
- Missing credentials are subject to form validation; incorrect credentials produce an authentication error.
- Inputs are disabled while login is running.
- Successful login opens the home screen showing the user's name after session storage succeeds.

## Logout

- Logout attempts to end the server session and clears local authentication data.
- After successful local logout, the login screen is shown without back navigation into the authenticated area.
- If the server cannot confirm logout, including when the access token has expired, local logout still completes and the app shows a warning.
- If local secure-storage cleanup fails, the app reports the failure instead of presenting logout as completed.

## Deferred Behavior

### Imported Participant Claiming

Allowing users to take over an existing imported participant remains a product requirement, but the app flow is not connected yet. It explicitly shows that this option is unavailable and does not display sample participants. The existing backend claiming capability must not be interpreted as an available app feature.

### Session Restoration

Sessions are not automatically restored when the app restarts. Secure session storage does not yet imply automatic sign-in at startup.