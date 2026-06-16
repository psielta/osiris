# Osiris Mobile (Kotlin Multiplatform)

Authentication base for the Osiris mobile app. Cross-platform logic lives in `:shared` (Ktor, Koin,
session, ViewModels); the Android UI (Jetpack Compose, Material 3) lives in `:android`. iOS is a later
milestone — the `iosMain` source set is reserved and `commonMain` is kept platform-agnostic.

## Modules

- `shared/` — KMP. `commonMain`: DTOs, Ktor client with single-flight token refresh, `SessionManager`,
  `TokenStore`, `AuthRepository`, validators, and the screen ViewModels. `androidMain`: OkHttp engine
  and the DataStore-backed `TokenStore`.
- `android/` — Compose app: Eye-of-Horus theme, navigation (Splash → Login ↔ Register → Home), the four
  screens, and Koin startup.

## Requirements

- JDK 17+
- Android SDK (`compileSdk 35`, `minSdk 26`)
- Android Studio, or the bundled Gradle wrapper

## Run against the local API

1. Start PostgreSQL and the API, bound to all interfaces so the emulator can reach the host:
   ```powershell
   docker compose up -d                                   # from the repo root
   dotnet run --project ../src/Osiris.Api --urls http://0.0.0.0:13455
   ```
2. Open `mobile/` in Android Studio, or build from the CLI:
   ```powershell
   ./gradlew :android:assembleDebug
   ```
3. Run on an emulator (API 26+). Debug builds target `http://10.0.2.2:13455/` (the emulator's view of the
   host loopback); cleartext to `10.0.2.2` is permitted only in debug.
4. Register or log in → Home shows your name and workspace. Relaunch the app → it skips login (the token
   is persisted). Let the access token expire, then act → the client refreshes silently. "Sair" returns
   to login.

For a physical device, set `BASE_URL` to the host LAN IP (release builds are HTTPS-only).

## Tests

```powershell
./gradlew :shared:testDebugUnitTest
```

Covers the client-side validators and the single-flight token-refresh logic (Ktor `MockEngine`).

## Configuration

- Base URL per build type via `buildConfigField` in `android/build.gradle.kts`.
- Dependency versions are pinned in `gradle/libs.versions.toml`.
- The token store currently keeps tokens in app-private DataStore; at-rest encryption (Android Keystore)
  is a planned follow-up behind the `TokenStore` interface.
