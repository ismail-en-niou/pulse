# Local API

The Pulse app serves a small HTTP API so any script or tool can show activities in the island. The `pulse` CLI is a thin wrapper around it: use the CLI from shell scripts, and the API when you'd rather not start a process per update.

## Connecting

- The API listens on **127.0.0.1 only**, on a port the OS picks each time Pulse starts.
- The port and the token are in `connection.json` in the Pulse folder:
  - Windows: `%LOCALAPPDATA%\Pulse\connection.json`
  - macOS: `~/Library/Application Support/Pulse/connection.json`
  - The `PULSE_HOME` environment variable overrides the folder, e.g. to run a second development copy.

  ```json
  { "port": 52731, "token": "…", "pid": 4242 }
  ```

  `port` is `0` while Pulse isn't running.
- Only the current user can read the file: its Windows access list grants only you, and on macOS its mode is 0600. The token stays the same across restarts; delete the file while Pulse is stopped to get a new one.
- Every request needs `Authorization: Bearer <token>`, or it gets `401`.

## Activities

| Field | Type | Notes |
|---|---|---|
| `id` | string | 10 characters, set by Pulse |
| `source` | string | Who reported it; `api` unless given (the CLI sends `cli`). Up to 40 of `A-Z a-z 0-9 - _ .` |
| `title` | string | Required, up to 120 characters |
| `subtitle` | string? | Up to 240 characters |
| `state` | string | `running`, `needsInput`, `success`, `failure` or `info` |
| `progress` | int? | 0–100, or absent when there is nothing to measure |
| `priority` | int | −100 to 100, default 0; higher shows first |
| `createdAt`, `updatedAt`, `finishedAt` | timestamp | ISO 8601, UTC |
| `isFinished` | bool | `success`, `failure` and `info` are finished and can't change |

The island shows the first activity in display order: highest **priority**, then the most urgent **state** (needs input, failure, running, success, notice), then the **newest**. Finished activities fade out after `finishedActivitySeconds` from `settings.json` (default 10).

## Endpoints

| Method and path | Body | Result |
|---|---|---|
| `GET /v1/activities` | none | `200 { "activities": [ … ] }` in display order |
| `POST /v1/activities` | `{ "title", "subtitle"?, "state"?, "progress"?, "priority"?, "source"? }` | `201` with the activity and a `Location` header |
| `PATCH /v1/activities/{id}` | `{ "title"?, "subtitle"?, "state"?, "progress"?, "priority"? }` | `200` with the activity |
| `POST /v1/activities/{id}/finish` | `{ "state": "success" \| "failure", "subtitle"? }` | `200` with the activity |

- `POST` with `"state": "info"` (or `success`/`failure`) creates an activity that is already finished, such as a notification.
- `PATCH` changes only the fields it carries. `state` can switch between `running` and `needsInput`; use `/finish` to end an activity. An empty `subtitle` clears it.
- Finishing with `success` sets measured progress to 100; a failure keeps the progress it reached.
- `{id}` can be `latest` with `?source=<name>`: the newest unfinished activity from that source.
- Bodies are limited to 16 KB.

## Errors

Errors come back as `{ "error": "<code>", "message": "<text>" }`:

| Status | `error` | When |
|---|---|---|
| 400 | `invalid` | Bad JSON, a missing title, or a value out of range |
| 401 | `unauthorized` | Missing or wrong token |
| 404 | `notFound` | No such activity, no unfinished `latest` one, or no such endpoint |
| 405 | `methodNotAllowed` | Wrong method; `Allow` lists the right one |
| 409 | `finished` | The activity has already finished |
| 413 | `badRequest` | The body is over 16 KB |

## Examples

bash, with `jq`:

```bash
conn=~/Library/Application\ Support/Pulse/connection.json   # %LOCALAPPDATA%/Pulse/connection.json on Windows
port=$(jq .port "$conn"); token=$(jq -r .token "$conn")
api() { curl -s -H "Authorization: Bearer $token" -H "Content-Type: application/json" "http://127.0.0.1:$port$@"; }

id=$(api /v1/activities -d '{"title":"Training model","source":"train"}' | jq -r .id)
api /v1/activities/$id -X PATCH -d '{"progress":40,"subtitle":"epoch 4 of 10"}'
api /v1/activities/$id/finish -d '{"state":"success"}'
```

PowerShell:

```powershell
$conn = Get-Content "$env:LOCALAPPDATA\Pulse\connection.json" | ConvertFrom-Json
$headers = @{ Authorization = "Bearer $($conn.token)" }
$base = "http://127.0.0.1:$($conn.port)/v1/activities"

$activity = Invoke-RestMethod $base -Method Post -Headers $headers -ContentType application/json -Body '{"title":"Backup"}'
Invoke-RestMethod "$base/$($activity.id)/finish" -Method Post -Headers $headers -ContentType application/json -Body '{"state":"success"}'
```

Pulse logs event types and timings only. It never logs tokens, titles or subtitles.
