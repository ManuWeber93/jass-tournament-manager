# Manual Authentication Tests

These smoke tests verify user-visible authentication behavior. Business rules are maintained in [Authentication](../features/authentication.md); technical details and local API addresses are maintained in [Architecture](../architecture.md#app-authentication-and-session-handling).

## Prerequisites

- Start the API and the app using the [README setup instructions](../../README.md#first-time-setup).
- Configure API connectivity and platform HTTP/HTTPS settings for the target device as described in the architecture documentation.
- Prepare an existing account with known credentials for login tests.
- Use an email address not already registered for the successful registration test, and another unused address for the connection-failure test.

## Login and Logout

1. Submit empty credentials and verify that validation is shown.
2. Submit incorrect credentials and verify that an authentication error is shown.
3. Log in with valid credentials and verify the displayed user name and disabled inputs while loading.
4. Log out and verify that the login screen returns without back navigation into the authenticated area.
5. Log in again, stop the API, and log out: verify local logout and the remote-revocation warning.

## Registration

1. Open registration, choose no previous participation, and verify required fields, password confirmation and the 200-character password limit.
2. Register with a new email address, valid names and a password containing at least eight characters, a digit and a special character. Verify disabled inputs while loading and automatic navigation to the home screen with the correct name.
3. Log out, then log in with the newly created account.
4. Log out and attempt registration with the same email address: verify the duplicate-email message and that the app stays unauthenticated.
5. Stop the API and attempt registration with a different address: verify the connection message and restored controls. If the response was lost after submission, try login before registering again.

## Coverage Limits

These manual scenarios do not deterministically reproduce secure-storage failures or a response lost after successful account creation. Those cases require controlled fault injection; platform-neutral automated tests use fake HTTP and secure-storage implementations. See [Running app unit tests](../../README.md#running-app-unit-tests) for the command and the limits of that test suite.

This document is a test procedure, not evidence that a platform build, device smoke test or live-API test has been performed.