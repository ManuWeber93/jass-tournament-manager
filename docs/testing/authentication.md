# Manual Authentication Tests

These smoke tests verify user-visible authentication behavior. Business rules are maintained in [Authentication](../features/authentication.md); technical details and local API addresses are maintained in [Architecture](../architecture.md#app-authentication-and-session-handling).

## Prerequisites

- Start the API and the app using the [README setup instructions](../../README.md#first-time-setup).
- Configure API connectivity and platform HTTP/HTTPS settings for the target device as described in the architecture documentation.
- Prepare an existing account with known credentials for login tests.
- Use an email address not already registered for the successful registration test, and another unused address for the connection-failure test.

## Android Device over USB

Android Debug builds use `http://localhost:8080`. ADB reverse port forwarding connects that device-local address to the API published on the development PC by Docker Compose. The same setup is required for an Android emulator using this Debug configuration.

1. Start the Compose API and database using the README setup instructions. The API must be reachable on the PC at `http://localhost:8080/swagger/index.html` before testing the device connection.
2. Enable developer options and USB debugging on the Android device, connect it by USB, and accept the debugging authorization prompt.
3. Run the following commands on the PC. Use the Android SDK platform-tools directory or the full path to `adb.exe` if ADB is not on `PATH`:

   ```powershell
   adb devices -l
   adb reverse tcp:8080 tcp:8080
   adb reverse --list
   ```

   The device must appear with status `device`, not `unauthorized`. If multiple devices or emulators are connected, add `-s DEVICE_SERIAL` immediately after `adb` in each reverse command, using the serial from the device list.
4. Open `http://localhost:8080/swagger/index.html` in the device browser to verify the forwarded connection independently of the app.
5. Deploy and start the app in the Debug configuration, then execute the authentication scenarios below. Android Debug builds allow cleartext HTTP for local testing; this exception is not enabled in other configurations.

Keep the USB connection active. After reconnecting the device or restarting ADB, check the mapping and reapply it if needed. To remove it when finished:

```powershell
adb reverse --remove tcp:8080
```

No LAN IP, router port forwarding or public backend exposure is required for this USB connection. Use test accounts only: this development setup uses HTTP, not HTTPS. Do not use these development endpoints or the cleartext exception as production configuration.

## Login and Logout

1. Submit empty credentials and verify that validation is shown.
2. Submit incorrect credentials and verify that an authentication error is shown.
3. Log in with valid credentials and verify the displayed user name and disabled inputs while loading. Watch the transition: the entered values must remain unchanged until the home screen replaces login; neither empty fields nor reset-induced validation errors may flash before the switch.
4. Log out and verify that the login screen returns without back navigation into the authenticated area. Email and password must be empty, and previous authentication and field-validation errors must not be visible. Open registration and verify that its inputs, participant selection, search filter and errors are also cleared and that it starts at the first step.
5. Log in again, stop the API, and log out: verify local logout and the remote-revocation warning.

## Registration

1. Open registration, choose no previous participation, and verify required fields, password confirmation and the 200-character password limit.
2. Register with a new email address, valid names and a password containing at least eight characters, a digit and a special character. Verify disabled inputs while loading and automatic navigation to the home screen with the correct name. The registration fields must not visibly clear, flash validation errors or switch back to login before the home screen replaces the authentication page.
3. Log out and verify that the empty login form is shown, not the registration form, without previous authentication or field-validation errors. Reopen registration and verify that all input fields and errors are cleared and the first step is shown. Then log in with the newly created account.
4. Log out and attempt registration with the same email address: verify the duplicate-email message and that the app stays unauthenticated.
5. Stop the API and attempt registration with a different address: verify the connection message and restored controls. If the response was lost after submission, try login before registering again.

## Field Validation Timing

1. Open login, focus the email field and type an incomplete address. Verify that no field error appears during this first edit. Leave the field and verify that the invalid-email message appears.
2. Return to the email field and correct the address. Verify that the message disappears while correcting it, without requiring another focus change.
3. Focus an empty password field and leave it. Verify that the required-field message appears only after leaving it. Untouched fields must not show field errors.
4. Repeat these checks in registration for names, email, password and confirmation. Enter differing passwords and verify that the mismatch message appears only after leaving confirmation for the first time; correcting either password should update it immediately afterward.
5. Submit a form with invalid or untouched required fields and verify that submission is still rejected.
6. Complete authentication successfully, log out and repeat the checks. All fields must wait for their first focus loss again, including password confirmation in registration.

## Password Visibility

1. Open login and verify that the password is masked initially. Enter a password, reveal it using the field's show/hide action, and verify that the text is unchanged. Toggle again and verify that it is masked.
2. Open registration and repeat the check independently for password and confirmation. Toggling one field must not reveal the other or affect validation.
3. While login or registration is running, verify that the show/hide actions are disabled together with their input fields.
4. On the target device, verify that the actions are discoverable and announced with a meaningful description by the screen reader.

## Coverage Limits

These manual scenarios do not deterministically reproduce secure-storage failures or a response lost after successful account creation. Those cases require controlled fault injection; platform-neutral automated tests use fake HTTP and secure-storage implementations. See [Running app unit tests](../../README.md#running-app-unit-tests) for the command and the limits of that test suite.

This document is a test procedure, not evidence that a platform build, device smoke test or live-API test has been performed.